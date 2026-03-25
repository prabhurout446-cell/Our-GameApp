using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Our11.Data;
using Our11.Models;

namespace Our11.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var vm = new DashboardViewModel
            {
                UpcomingMatches = await _db.Matches.Where(m => m.Status == "Upcoming" && m.IsActive).OrderBy(m => m.StartTime).Take(10).ToListAsync(),
                LiveMatches = await _db.Matches.Where(m => m.Status == "Live" && m.IsActive).ToListAsync()
            };

            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    vm.WalletBalance = user.WalletBalance;
                    vm.MyContests = await _db.UserContests
                        .Include(uc => uc.Contest).ThenInclude(c => c.Match)
                        .Include(uc => uc.UserTeam)
                        .Where(uc => uc.UserId == user.Id)
                        .OrderByDescending(uc => uc.JoinedAt)
                        .Take(5)
                        .ToListAsync();
                    vm.TotalContestsJoined = await _db.UserContests.CountAsync(uc => uc.UserId == user.Id);
                    vm.TotalWinnings = await _db.Transactions.Where(t => t.UserId == user.Id && t.Type == "WinningsCredit").SumAsync(t => t.Amount);
                }
            }
            return View(vm);
        }

        [Authorize]
        public async Task<IActionResult> MyContests()
        {
            var user = await _userManager.GetUserAsync(User);
            var contests = await _db.UserContests
                .Include(uc => uc.Contest).ThenInclude(c => c.Match)
                .Include(uc => uc.UserTeam)
                .Where(uc => uc.UserId == user!.Id)
                .OrderByDescending(uc => uc.JoinedAt)
                .ToListAsync();
            return View(contests);
        }

        [Authorize]
        public async Task<IActionResult> Wallet()
        {
            var user = await _userManager.GetUserAsync(User);
            var txns = await _db.Transactions.Where(t => t.UserId == user!.Id).OrderByDescending(t => t.CreatedAt).Take(50).ToListAsync();
            ViewBag.Balance = user!.WalletBalance;
            return View(txns);
        }

        [Authorize, HttpPost]
        public async Task<IActionResult> AddFunds(decimal amount)
        {
            if (amount <= 0 || amount > 100000) { TempData["Error"] = "Invalid amount."; return RedirectToAction("Wallet"); }
            var user = await _userManager.GetUserAsync(User);
            user!.WalletBalance += amount;
            _db.Transactions.Add(new Transaction { UserId = user.Id, Type = "Deposit", Amount = amount, Description = "Wallet top-up", Status = "Success" });
            await _db.SaveChangesAsync();
            TempData["Success"] = $"₹{amount} added to your wallet!";
            return RedirectToAction("Wallet");
        }

        [Authorize, HttpPost]
        public async Task<IActionResult> Withdraw(decimal amount)
        {
            var user = await _userManager.GetUserAsync(User);
            var minWithdrawal = 100m;
            var setting = await _db.AppSettings.FirstOrDefaultAsync(s => s.Key == "MinWithdrawal");
            if (setting != null) minWithdrawal = decimal.Parse(setting.Value);

            if (amount < minWithdrawal) { TempData["Error"] = $"Minimum withdrawal is ₹{minWithdrawal}."; return RedirectToAction("Wallet"); }
            if (user!.WalletBalance < amount) { TempData["Error"] = "Insufficient balance."; return RedirectToAction("Wallet"); }

            user.WalletBalance -= amount;
            _db.Transactions.Add(new Transaction { UserId = user.Id, Type = "Withdrawal", Amount = -amount, Description = "Withdrawal request", Status = "Success" });
            await _db.SaveChangesAsync();
            TempData["Success"] = $"₹{amount} withdrawal initiated!";
            return RedirectToAction("Wallet");
        }

        public IActionResult Privacy() => View();
    }
}
