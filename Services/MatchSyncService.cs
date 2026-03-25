using Microsoft.EntityFrameworkCore;
using Our11.Data;
using Our11.Models;
using Our11.Services;

namespace Our11.Services
{
    public class MatchSyncService : BackgroundService
    {
        private readonly IServiceScopeFactory _factory;
        private readonly ILogger<MatchSyncService> _logger;

        public MatchSyncService(IServiceScopeFactory factory, ILogger<MatchSyncService> logger)
        {
            _factory = factory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    using var scope = _factory.CreateScope();
                    var api = scope.ServiceProvider.GetRequiredService<ICricketApiService>();
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var contestSvc = scope.ServiceProvider.GetRequiredService<IContestService>();

                    // Fetch matches from API
                    var apiMatches = await api.FetchUpcomingMatchesAsync();
                    foreach (var am in apiMatches)
                    {
                        var existing = await db.Matches.FirstOrDefaultAsync(m => m.ExternalMatchId == am.ExternalMatchId, ct);
                        if (existing == null)
                        {
                            db.Matches.Add(am);
                            await db.SaveChangesAsync(ct);

                            // Auto-fetch players
                            var players = await api.FetchPlayersForMatchAsync(am.ExternalMatchId);
                            var saved = await db.Matches.FirstOrDefaultAsync(m => m.ExternalMatchId == am.ExternalMatchId, ct);
                            if (saved != null)
                            {
                                foreach (var p in players) { p.MatchId = saved.Id; db.Players.Add(p); }
                                await db.SaveChangesAsync(ct);
                            }
                        }
                        else
                        {
                            existing.Status = am.Status;
                            existing.Score = am.Score;
                            await db.SaveChangesAsync(ct);
                        }
                    }

                    // Finalize completed contests
                    var completedMatches = await db.Matches.Where(m => m.Status == "Completed").Select(m => m.Id).ToListAsync(ct);
                    var openContests = await db.Contests.Where(c => completedMatches.Contains(c.MatchId) && c.Status != "Completed" && c.Status != "Cancelled").ToListAsync(ct);
                    foreach (var c in openContests)
                        await contestSvc.FinalizeContestAsync(c.Id);

                    _logger.LogInformation("Match sync completed at {Time}", DateTime.UtcNow);
                }
                catch (Exception ex) { _logger.LogError(ex, "Match sync error"); }

                await Task.Delay(TimeSpan.FromMinutes(5), ct);
            }
        }
    }
}
