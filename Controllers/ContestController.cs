using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Our11.Data;
using Our11.Models;
using Our11.Services;

namespace Our11.Controllers
{
    [Authorize]
    public class ContestController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IContestService _contestService;

        public ContestController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, IContestService contestService)
        {
            _db = db;
            _userManager = userManager;
            _contestService = contestService;
        }

        public async Task<IActionResult> Detail(int id)
        {
            var contest = await _db.Contests
                .Include(c => c.Match)
                .Include(c => c.Prizes)
                .Include(c => c.UserContests).ThenInclude(uc => uc.User)
                .Include(c => c.UserContests).ThenInclude(uc => uc.UserTeam)
                .FirstOrDefaultAsync(c => c.Id == id);
            if (contest == null) return NotFound();

            var vm = new ContestDetailViewModel
            {
                Contest = contest,
                Participants = contest.UserContests.OrderBy(uc => uc.Rank ?? 9999).ToList(),
                NetPrizePool = await _contestService.GetNetPrizePoolAsync(id)
            };
            var user = await _userManager.GetUserAsync(User);
            vm.UserTeams = await _db.UserTeams.Where(ut => ut.UserId == user!.Id && ut.MatchId == contest.MatchId).ToListAsync();
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Join(int contestId, int teamId)
        {
            var user = await _userManager.GetUserAsync(User);
            var (success, message) = await _contestService.JoinContestAsync(user!.Id, contestId, teamId);
            if (success) TempData["Success"] = message;
            else TempData["Error"] = message;
            return RedirectToAction("Detail", new { id = contestId });
        }

        public async Task<IActionResult> Create(int matchId)
        {
            var match = await _db.Matches.FindAsync(matchId);
            if (match == null) return NotFound();
            ViewBag.Match = match;
            ViewBag.CommissionPct = await _contestService.GetCommissionPctAsync();
            return View(new CreateContestViewModel { MatchId = matchId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateContestViewModel vm)
        {
            var user = await _userManager.GetUserAsync(User);
            var commissionPct = await _contestService.GetCommissionPctAsync();
            var (success, message) = await _contestService.CreateUserContestAsync(user!.Id, vm, commissionPct);
            if (success) { TempData["Success"] = message; return RedirectToAction("Detail", "Match", new { id = vm.MatchId }); }
            TempData["Error"] = message;
            ViewBag.Match = await _db.Matches.FindAsync(vm.MatchId);
            ViewBag.CommissionPct = commissionPct;
            return View(vm);
        }

        public async Task<IActionResult> JoinByCode(string code)
        {
            var contest = await _db.Contests.Include(c => c.Match).FirstOrDefaultAsync(c => c.InviteCode == code.ToUpper());
            if (contest == null) { TempData["Error"] = "Invalid invite code."; return RedirectToAction("Index", "Home"); }
            return RedirectToAction("Detail", new { id = contest.Id });
        }

        [HttpGet]
        public IActionResult InviteCode() => View();
    }
}
