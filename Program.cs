using EenJaarGratis.Components;
using EenJaarGratis.Data;
using EenJaarGratis.Services;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents().AddInteractiveServerComponents();

builder.Services.AddDbContextFactory<AppDbContext>(o =>
    o.UseSqlite(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddSingleton<GameState>();   // one show, shared by all screens
builder.Services.AddScoped<QuizService>();

var app = builder.Build();

// Sketch shortcut: create the database if it doesn't exist.
// For real use, add migrations (dotnet ef migrations add Initial).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext();
    db.Database.EnsureCreated();

    // Sketch shortcut: EnsureCreated() only builds a brand-new database, so patch
    // in columns added after the db file already existed (see Data/Entities.cs).
    try
    {
        db.Database.ExecuteSqlRaw("ALTER TABLE Questions ADD COLUMN SortOrder INTEGER NOT NULL DEFAULT 0");
        db.Database.ExecuteSqlRaw("UPDATE Questions SET SortOrder = Id");
    }
    catch (Microsoft.Data.Sqlite.SqliteException) { /* column already exists */ }
}

// Container deployments sit behind a TLS-terminating reverse proxy, so trust its
// X-Forwarded-Proto header rather than looping on UseHttpsRedirection below.
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();   // phones only allow camera access over HTTPS
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();
