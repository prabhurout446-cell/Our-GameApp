using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Our11.Models;

namespace Our11.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Match> Matches { get; set; }
        public DbSet<Player> Players { get; set; }
        public DbSet<Contest> Contests { get; set; }
        public DbSet<Prize> Prizes { get; set; }
        public DbSet<UserTeam> UserTeams { get; set; }
        public DbSet<TeamPlayer> TeamPlayers { get; set; }
        public DbSet<UserContest> UserContests { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<AppSetting> AppSettings { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Fix: explicit decimal precision for ApplicationUser.WalletBalance
            builder.Entity<ApplicationUser>(e =>
            {
                e.Property(u => u.WalletBalance).HasColumnType("decimal(18,2)");
            });

            builder.Entity<Match>(e =>
            {
                e.Property(m => m.Sport).HasDefaultValue("Cricket");
                e.HasIndex(m => m.ExternalMatchId);
            });

            builder.Entity<Player>(e =>
            {
                e.Property(p => p.Credits).HasColumnType("decimal(5,1)");
                e.HasOne(p => p.Match).WithMany(m => m.Players).HasForeignKey(p => p.MatchId).OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<Contest>(e =>
            {
                e.Property(c => c.EntryFee).HasColumnType("decimal(18,2)");
                e.Property(c => c.TotalPrize).HasColumnType("decimal(18,2)");
                e.Property(c => c.CommissionPct).HasColumnType("decimal(5,2)");
                e.HasOne(c => c.Match).WithMany(m => m.Contests).HasForeignKey(c => c.MatchId).OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<Prize>(e =>
            {
                e.Property(p => p.Amount).HasColumnType("decimal(18,2)");
                e.HasOne(p => p.Contest).WithMany(c => c.Prizes).HasForeignKey(p => p.ContestId).OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<UserTeam>(e =>
            {
                e.HasOne(ut => ut.User).WithMany(u => u.UserTeams).HasForeignKey(ut => ut.UserId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(ut => ut.Match).WithMany().HasForeignKey(ut => ut.MatchId).OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<TeamPlayer>(e =>
            {
                e.HasOne(tp => tp.UserTeam).WithMany(ut => ut.TeamPlayers).HasForeignKey(tp => tp.UserTeamId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(tp => tp.Player).WithMany(p => p.TeamPlayers).HasForeignKey(tp => tp.PlayerId).OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<UserContest>(e =>
            {
                e.Property(uc => uc.WinningsAmount).HasColumnType("decimal(18,2)");
                e.HasOne(uc => uc.User).WithMany(u => u.UserContests).HasForeignKey(uc => uc.UserId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(uc => uc.Contest).WithMany(c => c.UserContests).HasForeignKey(uc => uc.ContestId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(uc => uc.UserTeam).WithMany(ut => ut.UserContests).HasForeignKey(uc => uc.UserTeamId).OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<Transaction>(e =>
            {
                e.Property(t => t.Amount).HasColumnType("decimal(18,2)");
                e.HasOne(t => t.User).WithMany(u => u.Transactions).HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Cascade);
            });

            // Seed data — use fixed date so EF doesn't detect a model change on every build
            var seedDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            builder.Entity<AppSetting>().HasData(
                new AppSetting { Id = 1, Key = "CommissionPct", Value = "25", Description = "Platform commission percentage on contest prize pool", UpdatedAt = seedDate },
                new AppSetting { Id = 2, Key = "MinWithdrawal", Value = "100", Description = "Minimum withdrawal amount in INR", UpdatedAt = seedDate },
                new AppSetting { Id = 3, Key = "MaxTeamsPerContest", Value = "10", Description = "Max teams a single user can join per contest", UpdatedAt = seedDate },
                new AppSetting { Id = 4, Key = "WelcomeBonus", Value = "50", Description = "Welcome bonus for new users (INR)", UpdatedAt = seedDate }
            );
        }
    }
}