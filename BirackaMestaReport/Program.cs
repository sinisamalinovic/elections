using BirackaMestaReport.Data;
using BirackaMestaReport.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using PosmatraciApp.Shared.Models;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Blazor Server + MudBlazor
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddMudServices();

// EF Core + SQLite — koristi ContentRootPath da bi radilo i na Azure
var dbFolder = Path.Combine(builder.Environment.ContentRootPath, "data");
Directory.CreateDirectory(dbFolder);
var dbPath = Path.Combine(dbFolder, "posmatraci.db");
builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

// App services
builder.Services.AddScoped<ReportService>();

// Cookie auth
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.Cookie.Name = "posmatraci_admin_auth";
        options.ExpireTimeSpan = TimeSpan.FromHours(12);
    });
builder.Services.AddAuthorization();

// CORS (za PosmatraciApp)
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

// Migrate / create DB
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

// Global exception handler — uvek vraća JSON čak i u production
app.UseExceptionHandler(errApp => errApp.Run(async ctx =>
{
    var ex = ctx.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
    ctx.Response.StatusCode = 500;
    ctx.Response.ContentType = "application/json";
    await ctx.Response.WriteAsJsonAsync(new { error = ex?.Message ?? "Unknown error", type = ex?.GetType().Name });
}));

app.UseCors();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Auth middleware: protect all non-API, non-login routes
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value ?? "";
    if (!path.StartsWith("/api") && !path.StartsWith("/login") && !path.StartsWith("/_blazor") && !path.StartsWith("/_framework"))
    {
        if (!(context.User.Identity?.IsAuthenticated ?? false))
        {
            context.Response.Redirect("/login");
            return;
        }
    }
    await next();
});

// API endpoints
app.MapPost("/api/submit", async (SubmitRequest req, ReportService reportService, ILogger<Program> logger) =>
{
    if (string.IsNullOrWhiteSpace(req.Email))
        return Results.BadRequest("Email je obavezan.");
    if (req.BmState == null && req.BmIzlaznost == null)
        return Results.BadRequest("BmState ili BmIzlaznost moraju biti prisutni.");

    try
    {
        await reportService.SaveSubmissionAsync(req.Email, req.BmState, req.BmIzlaznost);
        return Results.Ok(new { message = "Primljeno." });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Greška pri čuvanju submisije za {Email}", req.Email);
        return Results.Problem(detail: ex.Message, title: "Greška pri čuvanju", statusCode: 500);
    }
});

app.MapPost("/api/upload-json", async (HttpRequest request, ReportService reportService) =>
{
    if (!request.HasFormContentType || request.Form.Files.Count == 0)
        return Results.BadRequest("Fajl nije priložen.");

    var file = request.Form.Files[0];
    using var stream = file.OpenReadStream();
    SubmitRequest? req;
    try
    {
        req = await JsonSerializer.DeserializeAsync<SubmitRequest>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }
    catch
    {
        return Results.BadRequest("Neispravan JSON format.");
    }

    if (req?.BmState == null || string.IsNullOrWhiteSpace(req.Email))
        return Results.BadRequest("Email i BmState su obavezni.");

    await reportService.SaveSubmissionAsync(req.Email, req.BmState, req.BmIzlaznost);
    return Results.Ok(new { message = "Fajl učitan." });
});

// Health check — proverava DB putanju i konekciju + test write
app.MapGet("/api/health", async (IWebHostEnvironment env, IDbContextFactory<AppDbContext> dbFactory) =>
{
    var dbFolder = Path.Combine(env.ContentRootPath, "data");
    var dbPath = Path.Combine(dbFolder, "posmatraci.db");

    string? dbWriteError = null;
    int rowCount = 0;
    try
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        rowCount = await db.Submissions.CountAsync();
    }
    catch (Exception ex)
    {
        dbWriteError = ex.Message;
    }

    return Results.Ok(new
    {
        status = dbWriteError == null ? "ok" : "db-error",
        contentRoot = env.ContentRootPath,
        dbPath = dbPath,
        dbExists = File.Exists(dbPath),
        dataFolderExists = Directory.Exists(dbFolder),
        rowCount = rowCount,
        dbError = dbWriteError
    });
});

app.MapRazorPages();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
