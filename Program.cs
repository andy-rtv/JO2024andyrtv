using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using JO2024andyrtv.Areas.Identity.Data;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Get the connection string from environment variables
var connectionString = builder.Configuration.GetConnectionString("JO2024ContextConnection") ?? throw new InvalidOperationException("Connection string 'JO2024ContextConnection' not found.");

builder.Services.AddDbContext<JO2024Context>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddDefaultIdentity<JO2024User>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<JO2024Context>();

// Add session handling
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages(); // Ensure Razor Pages are added
builder.Services.AddScoped<IUserClaimsPrincipalFactory<JO2024User>, ApplicationUserClaimsPrincipalFactory>();

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

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

app.UseSession();

app.MapRazorPages(); // Ensure Razor Pages are mapped
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var userManager = services.GetRequiredService<UserManager<JO2024User>>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    SeedData.Initialize(services, userManager, roleManager).Wait();
}

app.Run();
