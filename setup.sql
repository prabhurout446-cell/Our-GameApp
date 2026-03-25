-- =====================================================
-- Our11 - SQL Server Database Setup Script
-- Run this to create the database manually if needed
-- Or use: dotnet ef database update
-- =====================================================

USE master;
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'Our11DB')
BEGIN
    CREATE DATABASE Our11DB;
    PRINT 'Database Our11DB created.';
END
GO

USE Our11DB;
GO

-- ─── Identity Tables (created by EF Core) ─────────────────────────────────
-- AspNetRoles, AspNetUsers, AspNetUserRoles, etc. are auto-created by EF Core.
-- Run: dotnet ef migrations add InitialCreate
--      dotnet ef database update

-- ─── Verify Setup ──────────────────────────────────────────────────────────
PRINT 'Database ready. Run EF Core migrations to create tables.';
PRINT 'Admin credentials: admin@our11.com / Admin@123';
PRINT 'Welcome bonus per new user: check AppSettings table (key=WelcomeBonus)';
GO
