using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Our11.Data;
using Our11.Models;

namespace Our11.Controllers
{
    public class MatchController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public MatchController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(string? sport, string? status)
        {
            var q = _db.Matches.Where(m => m.IsActive).AsQueryable();
            if (!string.IsNullOrEmpty(sport)) q = q.Where(m => m.Sport == sport);
            if (!string.IsNullOrEmpty(status)) q = q.Where(m => m.Status == status);
            else q = q.Where(m => m.Status != "Completed");

            var matches = await q.OrderBy(m => m.Status == "Live" ? 0 : 1).ThenBy(m => m.StartTime).ToListAsync();
            ViewBag.Sport = sport;
            ViewBag.Status = status;
            return View(matches);
        }

        public async Task<IActionResult> Detail(int id)
        {
            var match = await _db.Matches.Include(m => m.Players).FirstOrDefaultAsync(m => m.Id == id);
            if (match == null) return NotFound();

            var contests = await _db.Contests.Include(c => c.Prizes).Where(c => c.MatchId == id && c.Status != "Cancelled").OrderBy(c => c.EntryFee).ToListAsync();
            var userTeams = new List<UserTeam>();
            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(User);
                userTeams = await _db.UserTeams.Where(ut => ut.UserId == user!.Id && ut.MatchId == id).Include(ut => ut.TeamPlayers).ThenInclude(tp => tp.Player).ToListAsync();
            }

            return View(new MatchDetailViewModel { Match = match, Players = match.Players.ToList(), Contests = contests, UserTeams = userTeams });
        }

        [Authorize]
        public async Task<IActionResult> LiveScore(int id)
        {
            var match = await _db.Matches.FindAsync(id);
            if (match == null) return NotFound();
            return Json(new { score = match.Score, status = match.Status });
        }
    }
}
