using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Our11.Data;
using Our11.Models;
using Our11.Services;

namespace Our11.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IContestService _contestService;
        private readonly ICricketApiService _cricketApi;

        public AdminController(ApplicationDbContext db, UserManager<ApplicationUser> um, IContestService cs, ICricketApiService ca)
        {
            _db = db; _userManager = um; _contestService = cs; _cricketApi = ca;
        }

        public async Task<IActionResult> Index()
        {
            var commissionPct = await _contestService.GetCommissionPctAsync();
            var vm = new AdminDashboardViewModel
            {
                TotalUsers = await _db.Users.CountAsync(),
                TotalMatches = await _db.Matches.CountAsync(),
                TotalContests = await _db.Contests.CountAsync(),
                TotalRevenue = await _db.Transactions.Where(t => t.Type == "ContestEntry").SumAsync(t => Math.Abs(t.Amount)) * commissionPct / 100m,
                CommissionPct = commissionPct,
                RecentTransactions = await _db.Transactions.Include(t => t.User).OrderByDescending(t => t.CreatedAt).Take(20).ToListAsync(),
                ActiveMatches = await _db.Matches.Where(m => m.Status != "Completed").OrderBy(m => m.StartTime).ToListAsync()
            };
            return View(vm);
        }

        // ─── Commission ────────────────────────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateCommission(decimal commissionPct)
        {
            if (commissionPct < 0 || commissionPct > 50) { TempData["Error"] = "Commission must be 0-50%."; return RedirectToAction("Index"); }
            await _contestService.SetCommissionPctAsync(commissionPct);
            TempData["Success"] = $"Commission updated to {commissionPct}%";
            return RedirectToAction("Index");
        }

        // ─── Matches ───────────────────────────────────────────────────────────
        public async Task<IActionResult> Matches() =>
            View(await _db.Matches.OrderByDescending(m => m.StartTime).ToListAsync());

        public IActionResult CreateMatch() => View();

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateMatch(Match m)
        {
            m.ExternalMatchId = Guid.NewGuid().ToString("N")[..8];
            m.CreatedAt = DateTime.UtcNow;
            _db.Matches.Add(m);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Match created.";
            return RedirectToAction("ManagePlayers", new { matchId = m.Id });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMatch(int id)
        {
            var m = await _db.Matches.FindAsync(id);
            if (m != null) { m.IsActive = false; await _db.SaveChangesAsync(); }
            return RedirectToAction("Matches");
        }

        // ─── Players ───────────────────────────────────────────────────────────
        public async Task<IActionResult> ManagePlayers(int matchId)
        {
            var match = await _db.Matches.Include(m => m.Players).FirstOrDefaultAsync(m => m.Id == matchId);
            if (match == null) return NotFound();
            return View(match);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPlayer(Player p)
        {
            _db.Players.Add(p);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Player added.";
            return RedirectToAction("ManagePlayers", new { matchId = p.MatchId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePlayerPoints(int playerId, int points)
        {
            var p = await _db.Players.FindAsync(playerId);
            if (p != null) { p.Points = points; await _db.SaveChangesAsync(); }
            return Ok();
        }

        // ─── Contests ──────────────────────────────────────────────────────────
        public async Task<IActionResult> Contests() =>
            View(await _db.Contests.Include(c => c.Match).OrderByDescending(c => c.CreatedAt).ToListAsync());

        public async Task<IActionResult> CreateContest(int? matchId)
        {
            ViewBag.Matches = await _db.Matches.Where(m => m.Status != "Completed" && m.IsActive).ToListAsync();
            ViewBag.CommissionPct = await _contestService.GetCommissionPctAsync();
            var vm = new CreateContestViewModel { MatchId = matchId ?? 0 };
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateContest(CreateContestViewModel vm)
        {
            var commissionPct = await _contestService.GetCommissionPctAsync();
            var gross = vm.EntryFee * vm.MaxTeams;
            var net = gross - (gross * commissionPct / 100m);

            var contest = new Contest
            {
                MatchId = vm.MatchId,
                Name = vm.Name,
                ContestType = vm.ContestType,
                MaxTeams = vm.MaxTeams,
                EntryFee = vm.EntryFee,
                TotalPrize = net,
                CommissionPct = commissionPct,
                CreatedBy = "admin",
                IsAdminContest = true,
                Status = "Open",
                InviteCode = vm.ContestType == "Private" ? await _contestService.GenerateInviteCodeAsync() : null
            };
            _db.Contests.Add(contest);
            await _db.SaveChangesAsync();

            if (vm.PrizeBreakdowns.Any())
            {
                foreach (var pb in vm.PrizeBreakdowns)
                    _db.Prizes.Add(new Prize { ContestId = contest.Id, RankFrom = pb.RankFrom, RankTo = pb.RankTo, Amount = pb.Amount });
                await _db.SaveChangesAsync();
            }
            TempData["Success"] = "Contest created!";
            return RedirectToAction("Contests");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> FinalizeContest(int id)
        {
            await _contestService.FinalizeContestAsync(id);
            TempData["Success"] = "Contest finalized and prizes distributed!";
            return RedirectToAction("Contests");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelContest(int id)
        {
            using var tx = await _db.Database.BeginTransactionAsync();
            var contest = await _db.Contests.FindAsync(id);
            if (contest == null) { await tx.RollbackAsync(); return NotFound(); }

            // Refund all entries
            var entries = await _db.UserContests.Where(uc => uc.ContestId == id).ToListAsync();
            foreach (var uc in entries)
            {
                var user = await _db.Users.FindAsync(uc.UserId);
                if (user != null)
                {
                    user.WalletBalance += contest.EntryFee;
                    _db.Transactions.Add(new Transaction { UserId = user.Id, Type = "Deposit", Amount = contest.EntryFee, Description = $"Refund: {contest.Name} cancelled", Status = "Success" });
                }
            }
            contest.Status = "Cancelled";
            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            TempData["Success"] = "Contest cancelled and all entry fees refunded.";
            return RedirectToAction("Contests");
        }

        // ─── Users ─────────────────────────────────────────────────────────────
        public async Task<IActionResult> Users()
        {
            var users = _userManager.Users.ToList();
            return View(users);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null) { user.IsActive = !user.IsActive; await _userManager.UpdateAsync(user); }
            return RedirectToAction("Users");
        }

        // ─── Settings ──────────────────────────────────────────────────────────
        public async Task<IActionResult> Settings() =>
            View(await _db.AppSettings.ToListAsync());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSetting(int id, string value)
        {
            var s = await _db.AppSettings.FindAsync(id);
            if (s != null) { s.Value = value; s.UpdatedAt = DateTime.UtcNow; await _db.SaveChangesAsync(); }
            TempData["Success"] = "Setting updated.";
            return RedirectToAction("Settings");
        }

        // ─── Sync Matches from API ─────────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> SyncMatches()
        {
            var matches = await _cricketApi.FetchUpcomingMatchesAsync();
            int added = 0;
            foreach (var m in matches)
            {
                if (!await _db.Matches.AnyAsync(x => x.ExternalMatchId == m.ExternalMatchId))
                {
                    _db.Matches.Add(m);
                    added++;
                }
            }
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Synced {added} new matches.";
            return RedirectToAction("Matches");
        }
    }
}
