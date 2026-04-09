using Microsoft.EntityFrameworkCore;
using WorldCupFormations.Data;
using WorldCupFormations.Data.Repositories;
using WorldCupFormations.Data.Seed;
using WorldCupFormations.Services;
using WorldCupFormations.Web.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var dbDir  = Path.Combine(builder.Environment.ContentRootPath, "App_Data");
Directory.CreateDirectory(dbDir);
var dbPath = Path.Combine(dbDir, "worldcup.db");

builder.Services.AddDbContextFactory<AppDbContext>(opts =>
    opts.UseSqlite($"Data Source={dbPath}"));

builder.Services.AddScoped<IWorldCupRepository, WorldCupRepository>();
builder.Services.AddSingleton<FormationLayoutService>();

var app = builder.Build();

// Migrate and seed on startup
using (var scope = app.Services.CreateScope())
{
    var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
    await using var db = await factory.CreateDbContextAsync();
    await db.Database.MigrateAsync();
    if (!await db.WorldCups.AnyAsync())
        await SeedData.RunAsync(db, app.Environment.WebRootPath);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
