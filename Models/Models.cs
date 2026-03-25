using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Our11.Models
{
    // ─── Identity ─────────────────────────────────────────────────────────────
    public class ApplicationUser : IdentityUser
    {
        [Required, MaxLength(100)]
        public string FullName { get; set; } = "";
        public decimal WalletBalance { get; set; } = 0;
        public string? ProfilePicture { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
        public ICollection<UserContest> UserContests { get; set; } = new List<UserContest>();
        public ICollection<UserTeam> UserTeams { get; set; } = new List<UserTeam>();
        public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    }

    // ─── Match ─────────────────────────────────────────────────────────────────
    public class Match
    {
        public int Id { get; set; }
        [Required] public string ExternalMatchId { get; set; } = "";
        [Required] public string Team1 { get; set; } = "";
        [Required] public string Team2 { get; set; } = "";
        public string? Team1Logo { get; set; }
        public string? Team2Logo { get; set; }
        [Required] public string Sport { get; set; } = "Cricket";
        public string MatchType { get; set; } = "T20";
        public string Venue { get; set; } = "";
        public DateTime StartTime { get; set; }
        public string Status { get; set; } = "Upcoming"; // Upcoming, Live, Completed
        public string? Score { get; set; }
        public string? LiveData { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public ICollection<Contest> Contests { get; set; } = new List<Contest>();
        public ICollection<Player> Players { get; set; } = new List<Player>();
    }

    // ─── Player ────────────────────────────────────────────────────────────────
    public class Player
    {
        public int Id { get; set; }
        public int MatchId { get; set; }
        [Required] public string Name { get; set; } = "";
        [Required] public string Team { get; set; } = "";
        public string Role { get; set; } = ""; // WK, BAT, ALL, BOWL
        public string? Country { get; set; }
        public decimal Credits { get; set; } = 8.0m;
        public int SelectionPercentage { get; set; } = 0;
        public int Points { get; set; } = 0;
        public bool IsPlaying { get; set; } = true;
        public string? ImageUrl { get; set; }
        public Match Match { get; set; } = null!;
        public ICollection<TeamPlayer> TeamPlayers { get; set; } = new List<TeamPlayer>();
    }

    // ─── Contest ───────────────────────────────────────────────────────────────
    public class Contest
    {
        public int Id { get; set; }
        public int MatchId { get; set; }
        [Required] public string Name { get; set; } = "";
        public string ContestType { get; set; } = "Public"; // Public, Private
        public string? InviteCode { get; set; }
        public int MaxTeams { get; set; } = 100;
        public int FilledTeams { get; set; } = 0;
        public decimal EntryFee { get; set; } = 0;
        public decimal TotalPrize { get; set; } = 0;
        public decimal CommissionPct { get; set; } = 25m;
        public string CreatedBy { get; set; } = ""; // UserId or "admin"
        public bool IsAdminContest { get; set; } = true;
        public string Status { get; set; } = "Open"; // Open, Full, Completed, Cancelled
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Match Match { get; set; } = null!;
        public ICollection<UserContest> UserContests { get; set; } = new List<UserContest>();
        public ICollection<Prize> Prizes { get; set; } = new List<Prize>();
    }

    // ─── Prize ─────────────────────────────────────────────────────────────────
    public class Prize
    {
        public int Id { get; set; }
        public int ContestId { get; set; }
        public int RankFrom { get; set; }
        public int RankTo { get; set; }
        public decimal Amount { get; set; }
        public Contest Contest { get; set; } = null!;
    }

    // ─── UserTeam ──────────────────────────────────────────────────────────────
    public class UserTeam
    {
        public int Id { get; set; }
        public string UserId { get; set; } = "";
        public int MatchId { get; set; }
        [Required] public string TeamName { get; set; } = "";
        public string? CaptainId { get; set; }
        public string? ViceCaptainId { get; set; }
        public int TotalPoints { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public ApplicationUser User { get; set; } = null!;
        public Match Match { get; set; } = null!;
        public ICollection<TeamPlayer> TeamPlayers { get; set; } = new List<TeamPlayer>();
        public ICollection<UserContest> UserContests { get; set; } = new List<UserContest>();
    }

    // ─── TeamPlayer ────────────────────────────────────────────────────────────
    public class TeamPlayer
    {
        public int Id { get; set; }
        public int UserTeamId { get; set; }
        public int PlayerId { get; set; }
        public UserTeam UserTeam { get; set; } = null!;
        public Player Player { get; set; } = null!;
    }

    // ─── UserContest ───────────────────────────────────────────────────────────
    public class UserContest
    {
        public int Id { get; set; }
        public string UserId { get; set; } = "";
        public int ContestId { get; set; }
        public int UserTeamId { get; set; }
        public int? Rank { get; set; }
        public decimal? WinningsAmount { get; set; }
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
        public ApplicationUser User { get; set; } = null!;
        public Contest Contest { get; set; } = null!;
        public UserTeam UserTeam { get; set; } = null!;
    }

    // ─── Transaction ───────────────────────────────────────────────────────────
    public class Transaction
    {
        public int Id { get; set; }
        public string UserId { get; set; } = "";
        public string Type { get; set; } = ""; // Deposit, Withdrawal, ContestEntry, WinningsCredit, Commission
        public decimal Amount { get; set; }
        public string Status { get; set; } = "Success";
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public ApplicationUser User { get; set; } = null!;
    }

    // ─── AppSettings ───────────────────────────────────────────────────────────
    public class AppSetting
    {
        public int Id { get; set; }
        [Required] public string Key { get; set; } = "";
        [Required] public string Value { get; set; } = "";
        public string? Description { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    // ─── View Models ───────────────────────────────────────────────────────────
    public class LoginViewModel
    {
        [Required, EmailAddress] public string Email { get; set; } = "";
        [Required, DataType(DataType.Password)] public string Password { get; set; } = "";
        public bool RememberMe { get; set; }
    }

    public class RegisterViewModel
    {
        [Required, MaxLength(100)] public string FullName { get; set; } = "";
        [Required, EmailAddress] public string Email { get; set; } = "";
        [Required, MinLength(6)] public string Password { get; set; } = "";
        [Compare("Password")] public string ConfirmPassword { get; set; } = "";
        [Required] public string PhoneNumber { get; set; } = "";
    }

    public class CreateContestViewModel
    {
        public int MatchId { get; set; }
        [Required] public string Name { get; set; } = "";
        [Required, Range(2, 10000)] public int MaxTeams { get; set; } = 10;
        [Required, Range(0, 100000)] public decimal EntryFee { get; set; } = 10;
        public string ContestType { get; set; } = "Public";
        public List<PrizeBreakdown> PrizeBreakdowns { get; set; } = new();
    }

    public class PrizeBreakdown
    {
        public int RankFrom { get; set; }
        public int RankTo { get; set; }
        public decimal Amount { get; set; }
    }

    public class CreateTeamViewModel
    {
        public int MatchId { get; set; }
        public string TeamName { get; set; } = "";
        public List<int> PlayerIds { get; set; } = new();
        public int CaptainId { get; set; }
        public int ViceCaptainId { get; set; }
    }

    public class MatchDetailViewModel
    {
        public Match Match { get; set; } = null!;
        public List<Player> Players { get; set; } = new();
        public List<Contest> Contests { get; set; } = new();
        public List<UserTeam> UserTeams { get; set; } = new();
    }

    public class ContestDetailViewModel
    {
        public Contest Contest { get; set; } = null!;
        public List<UserContest> Participants { get; set; } = new();
        public List<UserTeam> UserTeams { get; set; } = new();
        public decimal NetPrizePool { get; set; }
    }

    public class DashboardViewModel
    {
        public List<Match> UpcomingMatches { get; set; } = new();
        public List<Match> LiveMatches { get; set; } = new();
        public List<UserContest> MyContests { get; set; } = new();
        public decimal WalletBalance { get; set; }
        public int TotalContestsJoined { get; set; }
        public decimal TotalWinnings { get; set; }
    }

    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalMatches { get; set; }
        public int TotalContests { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal CommissionPct { get; set; }
        public List<Transaction> RecentTransactions { get; set; } = new();
        public List<Match> ActiveMatches { get; set; } = new();
    }
}
