using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using JO2024andyrtv.Areas.Identity.Data;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Utiliser les variables d'environnement pour les connexions Heroku
var defaultConnectionString = Environment.GetEnvironmentVariable("DATABASE_URL") ??
                              builder.Configuration.GetConnectionString("DefaultConnection");

// Traiter la chaîne de connexion DATABASE_URL pour la compatibilité Heroku
if (defaultConnectionString != null && defaultConnectionString.StartsWith("postgres://"))
{
    defaultConnectionString = defaultConnectionString.Replace("postgres://", string.Empty);
    var pgUserPass = defaultConnectionString.Split("@")[0];
    var pgHostPortDb = defaultConnectionString.Split("@")[1];
    var pgHostPort = pgHostPortDb.Split("/")[0];
    var pgDb = pgHostPortDb.Split("/")[1];

    var pgUser = pgUserPass.Split(":")[0];
    var pgPass = pgUserPass.Split(":")[1];
    var pgHost = pgHostPort.Split(":")[0];
    var pgPort = pgHostPort.Split(":")[1];

    defaultConnectionString = $"Host={pgHost};Port={pgPort};Database={pgDb};Username={pgUser};Password={pgPass};sslmode=Prefer;Trust Server Certificate=true";
}

builder.Services.AddDbContext<JO2024Context>(options =>
    options.UseNpgsql(defaultConnectionString));

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
