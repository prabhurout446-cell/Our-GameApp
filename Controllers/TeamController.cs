using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Our11.Data;
using Our11.Models;

namespace Our11.Controllers
{
    [Authorize]
    public class TeamController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public TeamController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<IActionResult> Create(int matchId)
        {
            var match = await _db.Matches.Include(m => m.Players).FirstOrDefaultAsync(m => m.Id == matchId);
            if (match == null) return NotFound();
            if (match.StartTime <= DateTime.UtcNow) { TempData["Error"] = "Match has already started."; return RedirectToAction("Detail", "Match", new { id = matchId }); }
            ViewBag.Match = match;
            return View(match.Players.OrderBy(p => p.Role).ToList());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateTeamViewModel vm)
        {
            var user = await _userManager.GetUserAsync(User);
            var match = await _db.Matches.FindAsync(vm.MatchId);
            if (match == null || match.StartTime <= DateTime.UtcNow) return BadRequest("Invalid match.");

            // Validate team
            if (vm.PlayerIds.Count != 11) return BadRequest("Select exactly 11 players.");
            if (vm.CaptainId == 0 || vm.ViceCaptainId == 0) return BadRequest("Select captain and vice-captain.");

            var players = await _db.Players.Where(p => vm.PlayerIds.Contains(p.Id)).ToListAsync();

            // Credit check (max 100 credits)
            var totalCredits = players.Sum(p => p.Credits);
            if (totalCredits > 100) return BadRequest("Total credits cannot exceed 100.");

            // Role checks
            var wk = players.Count(p => p.Role == "WK");
            var bat = players.Count(p => p.Role == "BAT");
            var bowl = players.Count(p => p.Role == "BOWL");
            var all = players.Count(p => p.Role == "ALL");
            if (wk < 1 || wk > 4) return BadRequest("Need 1-4 WK.");
            if (bat < 3 || bat > 6) return BadRequest("Need 3-6 BAT.");
            if (bowl < 3 || bowl > 6) return BadRequest("Need 3-6 BOWL.");
            if (all < 1 || all > 4) return BadRequest("Need 1-4 ALL.");

            // Max 7 from one team
            var teams = players.GroupBy(p => p.Team);
            if (teams.Any(g => g.Count() > 7)) return BadRequest("Max 7 players from one team.");

            var teamName = string.IsNullOrEmpty(vm.TeamName) ? $"{user!.FullName}'s Team" : vm.TeamName;

            var team = new UserTeam
            {
                UserId = user!.Id,
                MatchId = vm.MatchId,
                TeamName = teamName,
                CaptainId = vm.CaptainId.ToString(),
                ViceCaptainId = vm.ViceCaptainId.ToString()
            };
            _db.UserTeams.Add(team);
            await _db.SaveChangesAsync();

            foreach (var pid in vm.PlayerIds)
                _db.TeamPlayers.Add(new TeamPlayer { UserTeamId = team.Id, PlayerId = pid });
            await _db.SaveChangesAsync();

            TempData["Success"] = "Team created successfully!";
            return RedirectToAction("Detail", "Match", new { id = vm.MatchId });
        }

        public async Task<IActionResult> View(int id)
        {
            var team = await _db.UserTeams
                .Include(t => t.User)
                .Include(t => t.Match)
                .Include(t => t.TeamPlayers).ThenInclude(tp => tp.Player)
                .FirstOrDefaultAsync(t => t.Id == id);
            if (team == null) return NotFound();
            return View(team);
        }

        public async Task<IActionResult> MyTeams(int matchId)
        {
            var user = await _userManager.GetUserAsync(User);
            var teams = await _db.UserTeams
                .Include(t => t.Match)
                .Include(t => t.TeamPlayers).ThenInclude(tp => tp.Player)
                .Where(t => t.UserId == user!.Id && t.MatchId == matchId)
                .ToListAsync();
            ViewBag.MatchId = matchId;
            return View(teams);
        }
    }
}
