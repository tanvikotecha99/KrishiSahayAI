using KrishiSahayAI.Data;
using KrishiSahayAI.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using KrishiSahay.Data;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("ApplicationDbContext") ?? throw new InvalidOperationException("Connection string 'ApplicationDbContext' not found.");

// Add MVC
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite("Data Source=Data/krishisahay.db"));
builder.Services
    .AddDefaultIdentity<IdentityUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>();
// Register database
builder.Services.AddSingleton<AppDb>();

// Register crop knowledge service
builder.Services.AddSingleton<ICropKnowledgeService, CropKnowledgeService>();

// Register Gemini AI service
builder.Services.AddHttpClient<IGeminiService, GeminiService>();
// Register weather service
builder.Services.AddHttpClient<IWeatherService, WeatherService>();

var app = builder.Build();

// Initialize SQLite database
using (var scope = app.Services.CreateScope())
{
    var database =
        scope.ServiceProvider.GetRequiredService<AppDb>();

    database.Initialize();
}

// Configure HTTP pipeline
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