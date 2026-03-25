using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Our11.Data;
using Our11.Models;
using Our11.Services;

var builder = WebApplication.CreateBuilder(args);

// ─── DB ─────────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
           .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));

// ─── Identity ────────────────────────────────────────────────────────────────
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(opt =>
{
    opt.Password.RequiredLength = 6;
    opt.Password.RequireNonAlphanumeric = false;
    opt.Password.RequireUppercase = false;
    opt.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(opt =>
{
    opt.LoginPath = "/Account/Login";
    opt.LogoutPath = "/Account/Logout";
    opt.AccessDeniedPath = "/Account/AccessDenied";
});

// ─── MVC ─────────────────────────────────────────────────────────────────────
builder.Services.AddControllersWithViews().AddRazorRuntimeCompilation();

// ─── Services ────────────────────────────────────────────────────────────────
builder.Services.AddHttpClient<ICricketApiService, CricketApiService>();
builder.Services.AddScoped<IContestService, ContestService>();
builder.Services.AddHostedService<MatchSyncService>();

var app = builder.Build();

// ─── Middleware ───────────────────────────────────────────────────────────────
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
app.MapControllerRoute("area", "{area:exists}/{controller=Home}/{action=Index}/{id?}");

// ─── Seed Admin ───────────────────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleMgr = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    db.Database.EnsureCreated();

    if (!await roleMgr.RoleExistsAsync("Admin"))
        await roleMgr.CreateAsync(new IdentityRole("Admin"));
    if (!await roleMgr.RoleExistsAsync("User"))
        await roleMgr.CreateAsync(new IdentityRole("User"));

    if (await userMgr.FindByEmailAsync("admin@our11.com") == null)
    {
        var admin = new ApplicationUser { FullName = "Admin", UserName = "admin@our11.com", Email = "admin@our11.com", WalletBalance = 0, EmailConfirmed = true };
        var r = await userMgr.CreateAsync(admin, "Admin@123");
        if (r.Succeeded) await userMgr.AddToRoleAsync(admin, "Admin");
    }
}

app.Run();