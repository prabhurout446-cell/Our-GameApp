using Microsoft.EntityFrameworkCore;
using Our11.Data;
using Our11.Models;

namespace Our11.Services
{
    public interface IContestService
    {
        Task<(bool Success, string Message)> JoinContestAsync(string userId, int contestId, int teamId);
        Task<decimal> GetNetPrizePoolAsync(int contestId);
        Task<decimal> GetCommissionPctAsync();
        Task SetCommissionPctAsync(decimal pct);
        Task<List<Prize>> CalculatePrizesAsync(int contestId);
        Task FinalizeContestAsync(int contestId);
        Task<(bool Success, string Message)> CreateUserContestAsync(string userId, CreateContestViewModel vm, decimal commissionPct);
        Task<string> GenerateInviteCodeAsync();
    }

    public class ContestService : IContestService
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<ContestService> _logger;

        public ContestService(ApplicationDbContext db, ILogger<ContestService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<decimal> GetCommissionPctAsync()
        {
            var setting = await _db.AppSettings.FirstOrDefaultAsync(s => s.Key == "CommissionPct");
            return setting != null ? decimal.Parse(setting.Value) : 25m;
        }

        public async Task SetCommissionPctAsync(decimal pct)
        {
            var setting = await _db.AppSettings.FirstOrDefaultAsync(s => s.Key == "CommissionPct");
            if (setting != null)
            {
                setting.Value = pct.ToString("F2");
                setting.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }
        }

        public async Task<decimal> GetNetPrizePoolAsync(int contestId)
        {
            var contest = await _db.Contests.FindAsync(contestId);
            if (contest == null) return 0;
            var gross = contest.EntryFee * contest.FilledTeams;
            var commission = gross * contest.CommissionPct / 100m;
            return gross - commission;
        }

        public async Task<(bool Success, string Message)> JoinContestAsync(string userId, int contestId, int teamId)
        {
            using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                var contest = await _db.Contests.Include(c => c.Match).FirstOrDefaultAsync(c => c.Id == contestId);
                if (contest == null) return (false, "Contest not found.");
                if (contest.Status != "Open") return (false, "Contest is not open.");
                if (contest.FilledTeams >= contest.MaxTeams) return (false, "Contest is full.");
                if (contest.Match.StartTime <= DateTime.UtcNow) return (false, "Match has already started.");

                // Already joined with this team?
                var exists = await _db.UserContests.AnyAsync(uc => uc.UserId == userId && uc.ContestId == contestId && uc.UserTeamId == teamId);
                if (exists) return (false, "You have already joined this contest with this team.");

                // Check user wallet
                var user = await _db.Users.FindAsync(userId);
                if (user == null) return (false, "User not found.");
                if (user.WalletBalance < contest.EntryFee) return (false, "Insufficient wallet balance. Please add funds.");

                // Deduct entry fee
                user.WalletBalance -= contest.EntryFee;
                contest.FilledTeams++;
                if (contest.FilledTeams >= contest.MaxTeams) contest.Status = "Full";

                _db.UserContests.Add(new UserContest
                {
                    UserId = userId,
                    ContestId = contestId,
                    UserTeamId = teamId,
                    JoinedAt = DateTime.UtcNow
                });

                _db.Transactions.Add(new Transaction
                {
                    UserId = userId,
                    Type = "ContestEntry",
                    Amount = -contest.EntryFee,
                    Description = $"Joined contest: {contest.Name}",
                    Status = "Success"
                });

                await _db.SaveChangesAsync();
                await tx.CommitAsync();
                return (true, "Successfully joined the contest!");
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "Error joining contest");
                return (false, "An error occurred. Please try again.");
            }
        }

        public async Task<List<Prize>> CalculatePrizesAsync(int contestId)
        {
            var contest = await _db.Contests.FindAsync(contestId);
            if (contest == null) return new();

            var gross = contest.EntryFee * contest.FilledTeams;
            var commission = gross * contest.CommissionPct / 100m;
            var net = gross - commission;

            // Auto-generate prize distribution if not set
            var existing = await _db.Prizes.Where(p => p.ContestId == contestId).ToListAsync();
            if (existing.Any()) return existing;

            var prizes = new List<Prize>();
            int winners = Math.Max(1, (int)(contest.FilledTeams * 0.4)); // top 40% win

            if (winners == 1)
            {
                prizes.Add(new Prize { ContestId = contestId, RankFrom = 1, RankTo = 1, Amount = net });
            }
            else if (winners == 2)
            {
                prizes.Add(new Prize { ContestId = contestId, RankFrom = 1, RankTo = 1, Amount = Math.Round(net * 0.6m, 2) });
                prizes.Add(new Prize { ContestId = contestId, RankFrom = 2, RankTo = 2, Amount = Math.Round(net * 0.4m, 2) });
            }
            else
            {
                prizes.Add(new Prize { ContestId = contestId, RankFrom = 1, RankTo = 1, Amount = Math.Round(net * 0.40m, 2) });
                prizes.Add(new Prize { ContestId = contestId, RankFrom = 2, RankTo = 2, Amount = Math.Round(net * 0.25m, 2) });
                prizes.Add(new Prize { ContestId = contestId, RankFrom = 3, RankTo = 3, Amount = Math.Round(net * 0.15m, 2) });
                int remaining = winners - 3;
                if (remaining > 0)
                {
                    var remainingPrize = Math.Round(net * 0.20m / remaining, 2);
                    prizes.Add(new Prize { ContestId = contestId, RankFrom = 4, RankTo = winners, Amount = remainingPrize });
                }
            }
            return prizes;
        }

        public async Task FinalizeContestAsync(int contestId)
        {
            using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                var contest = await _db.Contests.Include(c => c.Prizes).FirstOrDefaultAsync(c => c.Id == contestId);
                if (contest == null) return;

                var userContests = await _db.UserContests
                    .Include(uc => uc.UserTeam)
                    .Where(uc => uc.ContestId == contestId)
                    .OrderByDescending(uc => uc.UserTeam.TotalPoints)
                    .ToListAsync();

                // Assign ranks
                for (int i = 0; i < userContests.Count; i++)
                    userContests[i].Rank = i + 1;

                // Calculate prizes
                var prizes = contest.Prizes.Any() ? contest.Prizes.ToList() : await CalculatePrizesAsync(contestId);
                var gross = contest.EntryFee * contest.FilledTeams;
                var commission = gross * contest.CommissionPct / 100m;
                var net = gross - commission;

                foreach (var uc in userContests)
                {
                    var prize = prizes.FirstOrDefault(p => uc.Rank >= p.RankFrom && uc.Rank <= p.RankTo);
                    if (prize != null)
                    {
                        uc.WinningsAmount = prize.Amount;
                        var user = await _db.Users.FindAsync(uc.UserId);
                        if (user != null)
                        {
                            user.WalletBalance += prize.Amount;
                            _db.Transactions.Add(new Transaction
                            {
                                UserId = uc.UserId,
                                Type = "WinningsCredit",
                                Amount = prize.Amount,
                                Description = $"Won ₹{prize.Amount} in {contest.Name} (Rank #{uc.Rank})",
                                Status = "Success"
                            });
                        }
                    }
                }

                contest.Status = "Completed";
                await _db.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "Error finalizing contest {ContestId}", contestId);
            }
        }

        public async Task<(bool Success, string Message)> CreateUserContestAsync(string userId, CreateContestViewModel vm, decimal commissionPct)
        {
            var match = await _db.Matches.FindAsync(vm.MatchId);
            if (match == null) return (false, "Match not found.");
            if (match.StartTime <= DateTime.UtcNow.AddMinutes(5)) return (false, "Cannot create contest less than 5 minutes before match start.");

            var inviteCode = vm.ContestType == "Private" ? await GenerateInviteCodeAsync() : null;
            var totalPrize = vm.EntryFee * vm.MaxTeams;
            var netPrize = totalPrize - (totalPrize * commissionPct / 100m);

            var contest = new Contest
            {
                MatchId = vm.MatchId,
                Name = vm.Name,
                ContestType = vm.ContestType,
                InviteCode = inviteCode,
                MaxTeams = vm.MaxTeams,
                EntryFee = vm.EntryFee,
                TotalPrize = netPrize,
                CommissionPct = commissionPct,
                CreatedBy = userId,
                IsAdminContest = false,
                Status = "Open"
            };

            _db.Contests.Add(contest);
            await _db.SaveChangesAsync();

            // Save prize breakdowns if provided
            if (vm.PrizeBreakdowns.Any())
            {
                foreach (var pb in vm.PrizeBreakdowns)
                    _db.Prizes.Add(new Prize { ContestId = contest.Id, RankFrom = pb.RankFrom, RankTo = pb.RankTo, Amount = pb.Amount });
                await _db.SaveChangesAsync();
            }

            return (true, inviteCode != null ? $"Contest created! Invite Code: {inviteCode}" : "Contest created successfully!");
        }

        public async Task<string> GenerateInviteCodeAsync()
        {
            string code;
            do
            {
                code = Guid.NewGuid().ToString("N")[..8].ToUpper();
            } while (await _db.Contests.AnyAsync(c => c.InviteCode == code));
            return code;
        }
    }
}
