using KrishiSahayAI.Data;
using KrishiSahayAI.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using KrishiSahay.Data;

var builder = WebApplication.CreateBuilder(args);

// Add MVC
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// ------------------------------------------------------------
// Identity Database
// ------------------------------------------------------------
// Local Visual Studio:
//     Uses SQLite.
//
// Cloud Run:
//     Uses Cloud SQL PostgreSQL through the Cloud SQL Unix socket.
//
// Cloud Run environment variables required:
//     INSTANCE_UNIX_SOCKET
//     DB_USER
//     DB_PASS
//     DB_NAME
// ------------------------------------------------------------

var cloudSqlSocket = Environment.GetEnvironmentVariable("INSTANCE_UNIX_SOCKET");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (!string.IsNullOrWhiteSpace(cloudSqlSocket))
    {
        var dbUser = Environment.GetEnvironmentVariable("DB_USER");
        var dbPassword = Environment.GetEnvironmentVariable("DB_PASS");
        var dbName = Environment.GetEnvironmentVariable("DB_NAME");

        if (string.IsNullOrWhiteSpace(dbUser) ||
            string.IsNullOrWhiteSpace(dbPassword) ||
            string.IsNullOrWhiteSpace(dbName))
        {
            throw new InvalidOperationException(
                "Cloud SQL environment variables DB_USER, DB_PASS and DB_NAME are required.");
        }

        var connectionString =
            $"Host={cloudSqlSocket};" +
            $"Username={dbUser};" +
            $"Password={dbPassword};" +
            $"Database={dbName};" +
            "Ssl Mode=Disable;" +
            "Pooling=true;";

        options.UseNpgsql(connectionString);
    }
    else
    {
        // Local development database
        options.UseSqlite("Data Source=Data/krishisahay.db");
    }
});

// ------------------------------------------------------------
// ASP.NET Core Identity
// ------------------------------------------------------------

builder.Services
    .AddDefaultIdentity<IdentityUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>();

// ------------------------------------------------------------
// Register application SQLite database
// ------------------------------------------------------------

builder.Services.AddSingleton<AppDb>();

// ------------------------------------------------------------
// Register crop knowledge service
// ------------------------------------------------------------

builder.Services.AddSingleton<ICropKnowledgeService, CropKnowledgeService>();

// ------------------------------------------------------------
// Register Gemini AI service
// ------------------------------------------------------------

builder.Services.AddHttpClient<IGeminiService, GeminiService>();

// ------------------------------------------------------------
// Register weather service
// ------------------------------------------------------------

builder.Services.AddHttpClient<IWeatherService, WeatherService>();

var app = builder.Build();

// ------------------------------------------------------------
// Initialize application SQLite database
// ------------------------------------------------------------

using (var scope = app.Services.CreateScope())
{
    var database =
        scope.ServiceProvider.GetRequiredService<AppDb>();

    database.Initialize();
}

// ------------------------------------------------------------
// Initialize Identity database
// ------------------------------------------------------------
// Local: existing SQLite Identity database remains unchanged.
// Cloud Run: creates Identity tables in the new Cloud SQL database.
// ------------------------------------------------------------

using (var scope = app.Services.CreateScope())
{
    var db =
        scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    db.Database.EnsureCreated();
}

// ------------------------------------------------------------
// Configure HTTP pipeline
// ------------------------------------------------------------

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

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();