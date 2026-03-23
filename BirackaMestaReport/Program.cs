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

// EF Core + SQLite
builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

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
app.MapPost("/api/submit", async (SubmitRequest req, ReportService reportService) =>
{
    if (req.BmState == null || string.IsNullOrWhiteSpace(req.Email))
        return Results.BadRequest("Email i BmState su obavezni.");

    await reportService.SaveSubmissionAsync(req.Email, req.BmState, req.BmIzlaznost);
    return Results.Ok(new { message = "Primljeno." });
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

app.MapRazorPages();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
