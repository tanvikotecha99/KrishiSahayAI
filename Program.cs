using KrishiSahay.Data;
using KrishiSahayAI.Data;
using KrishiSahayAI.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

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

var cloudSqlSocket =
    Environment.GetEnvironmentVariable("INSTANCE_UNIX_SOCKET");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (!string.IsNullOrWhiteSpace(cloudSqlSocket))
    {
        var dbUser =
            Environment.GetEnvironmentVariable("DB_USER");

        var dbPassword =
            Environment.GetEnvironmentVariable("DB_PASS");

        var dbName =
            Environment.GetEnvironmentVariable("DB_NAME");

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
// ASP.NET Core Data Protection
// ------------------------------------------------------------
// Stores authentication encryption keys in the database.
//
// This is important for Cloud Run because the container filesystem
// is not a permanent storage location.
//
// It allows authentication cookies to remain valid when Cloud Run
// restarts or creates a new container instance.
// ------------------------------------------------------------

builder.Services
    .AddDataProtection()
    .PersistKeysToDbContext<ApplicationDbContext>();

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
// Register application database
// Local: SQLite
// Cloud Run: Cloud SQL PostgreSQL
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
// Initialize application database
// Local: SQLite
// Cloud Run: Cloud SQL PostgreSQL
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
// Local: SQLite
// Cloud Run: Cloud SQL PostgreSQL
//
// Cloud Run currently uses EnsureCreated() because the existing
// Cloud SQL Identity database was initialized with EnsureCreated().
//
// Local development continues to use EF migrations.
// ------------------------------------------------------------

using (var scope = app.Services.CreateScope())
{
    var db =
        scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    if (!string.IsNullOrWhiteSpace(cloudSqlSocket))
    {
        // Cloud Run: Cloud SQL PostgreSQL
        db.Database.EnsureCreated();
    }
    else
    {
        // Local Visual Studio: SQLite
        db.Database.Migrate();
    }
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
