# 🏏 Our11 — Fantasy Cricket Platform
### A Dream11-Inspired ASP.NET MVC App with SQL Server

---

## 📋 Features

### For Users
- 🔐 Register / Login with welcome bonus (₹50)
- 🏏 Browse upcoming & live cricket matches (via CricAPI)
- 👥 Build fantasy team (11 players, 100 credits, role rules)
- 🏆 Join public or private contests (entry fee deducted from wallet)
- 🎯 Create own contests & share invite codes for private ones
- 💰 Win real cash — prizes credited to wallet automatically
- 📊 Track contest leaderboard & personal stats
- 💳 Wallet: Add money, withdraw, full transaction history

### For Admins
- 📊 Admin dashboard with revenue, users, contests overview
- ⚙️ **Editable platform commission %** (default 25%) — admin-only
- 🏏 Create/manage matches & player rosters manually or via API sync
- 🏆 Create mega contests, finalize & distribute prizes
- ❌ Cancel contests with auto-refund to all participants
- 👤 Manage users (activate/deactivate)
- ⚙️ App-wide settings management

---

## 💰 Prize Calculation (Example)

```
Contest: 4 members × ₹10 entry fee
Gross Pool  = 4 × 10   = ₹40
Commission  = 25% of ₹40 = ₹10   (editable by admin)
Net Prizes  = ₹40 - ₹10 = ₹30   ← distributed to winners
```

**Commission is editable** from Admin → Dashboard or Admin → Settings.
Each contest locks its commission at creation time, so changes only affect new contests.

---

## 🚀 Quick Start

### Prerequisites
- .NET 8 SDK
- SQL Server (LocalDB works for dev)
- Visual Studio 2022 or VS Code

### 1. Clone & Configure

```bash
# Edit connection string in appsettings.json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=Our11DB;Trusted_Connection=True;"
}

# Optional: Add CricAPI key for live match data
"CricApi": {
  "ApiKey": "YOUR_KEY_FROM_cricapi.com"
}
```

> **Without API key**: The app uses built-in demo match data automatically — fully functional for testing.

### 2. Install & Run

```bash
cd Our11

# Install EF Core tools (if not already)
dotnet tool install --global dotnet-ef

# Create & migrate database
dotnet ef migrations add InitialCreate
dotnet ef database update

# Run the app
dotnet run
```

### 3. Access the App

| URL | Description |
|-----|-------------|
| `https://localhost:5001` | Main site |
| `https://localhost:5001/Admin` | Admin panel |

### Admin Credentials (auto-seeded)
```
Email:    admin@our11.com
Password: Admin@123
```

---

## 🗂️ Project Structure

```
Our11/
├── Controllers/
│   ├── HomeController.cs      # Dashboard, Wallet, MyContests
│   ├── AccountController.cs   # Login, Register, Profile
│   ├── MatchController.cs     # Match listing, detail, live score
│   ├── TeamController.cs      # Team builder, My Teams
│   ├── ContestController.cs   # Join, Create, Invite code
│   └── AdminController.cs     # Full admin panel
├── Models/
│   └── Models.cs              # All entities + ViewModels
├── Data/
│   └── ApplicationDbContext.cs # EF Core DbContext + seed data
├── Services/
│   ├── CricketApiService.cs   # CricAPI integration + fallback data
│   ├── ContestService.cs      # Prize math, join, finalize logic
│   └── MatchSyncService.cs    # Background auto-sync every 5 min
├── Views/
│   ├── Shared/_Layout.cshtml  # Main layout with nav, bottom bar
│   ├── Home/                  # Index, Wallet, MyContests
│   ├── Account/               # Login, Register, Profile
│   ├── Match/                 # Index (list), Detail (contests+players)
│   ├── Team/                  # Create (team builder)
│   ├── Contest/               # Detail, Create, InviteCode
│   └── Admin/                 # Full admin panel views
├── wwwroot/
│   ├── css/site.css           # Dream11-inspired dark theme
│   └── js/site.js             # TeamBuilder, countdowns, toasts
├── Program.cs                 # App setup, DI, seed admin
└── appsettings.json           # DB connection, API key
```

---

## 🎨 UI Theme

- **Dark mode** design inspired by Dream11
- **Accent color**: Neon yellow-green (`#d4f52a`)
- **Fonts**: Rajdhani (display) + Inter (body)
- **Fully mobile responsive** with bottom navigation bar
- Live countdown timers, animated fill bars, toast notifications

---

## 🔑 Team Building Rules

| Rule | Value |
|------|-------|
| Total players | Exactly 11 |
| Max credits | 100 |
| Wicket Keepers (WK) | 1 – 4 |
| Batsmen (BAT) | 3 – 6 |
| All-rounders (ALL) | 1 – 4 |
| Bowlers (BOWL) | 3 – 6 |
| Max from one team | 7 |
| Captain bonus | 2× points |
| Vice-Captain bonus | 1.5× points |

---

## 🌐 Live Score API

Uses **CricAPI** (free tier available at cricapi.com):
- Auto-fetches upcoming matches
- Syncs match status & scores every 5 minutes (background service)
- Falls back to demo data if no API key is configured

---

## 🛡️ Security

- ASP.NET Core Identity with hashed passwords
- Anti-forgery tokens on all forms
- Role-based authorization (Admin / User)
- Commission % editable by Admin role only
- Input validation on team builder (server + client side)

---

## 📝 Notes

- This is a **demo/educational project** — payment gateway not integrated
- "Add Funds" in wallet is simulated for demo purposes
- To go production: integrate Razorpay/PayU for real payments
- Player points must be updated manually by admin (or via API extension)
  <img width="955" height="476" alt="3" src="https://github.com/user-attachments/assets/ae2ad438-ff9b-4a7f-92bf-d935e35f8f86" />

