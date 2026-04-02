Our11 — Fantasy Cricket Platform

## 💰 Prize Calculation (Example)

Prerequisites
- .NET 9 SDK
- SQL Server (LocalDB works for dev)
- Visual Studio 2022 or VS Code

1. Clone & Configure

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
3. Access the App

| URL | Description |
|-----|-------------|
| `https://localhost:5001` | Main site |
| `https://localhost:5001/Admin` | Admin panel |


- "Add Funds" in wallet is simulated for demo purposes
- To go production: integrate Razorpay/PayU for real payments
- Player points must be updated manually by admin (or via API extension)
  <img width="955" height="476" alt="3" src="https://github.com/user-attachments/assets/ae2ad438-ff9b-4a7f-92bf-d935e35f8f86" />

