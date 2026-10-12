using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using GestionGimnasios.Web.Data;
using GestionGimnasios.Web.Data.Seed;
using GestionGimnasios.Web.Domain;
using GestionGimnasios.Web.Infrastructure;
using GestionGimnasios.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

// Español de Bolivia con punto decimal: los formularios HTML envían decimales con punto.
var cultura = (System.Globalization.CultureInfo)System.Globalization.CultureInfo.GetCultureInfo("es-BO").Clone();
cultura.NumberFormat.NumberDecimalSeparator = ".";
cultura.NumberFormat.NumberGroupSeparator = ",";
System.Globalization.CultureInfo.DefaultThreadCurrentCulture = cultura;
System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = cultura;

var builder = WebApplication.CreateBuilder(args);

// ── MVC, API y JSON ──────────────────────────────────────────────────────
// Razor no escapa tildes ni eñes como entidades (&#xE1;) en los textos dinámicos.
builder.Services.Configure<Microsoft.Extensions.WebEncoders.WebEncoderOptions>(o =>
    o.TextEncoderSettings = new System.Text.Encodings.Web.TextEncoderSettings(System.Text.Unicode.UnicodeRanges.All));
builder.Services.AddControllersWithViews()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });
builder.Services.AddOpenApi();
builder.Services.AddExceptionHandler<ManejadorErrores>();
builder.Services.AddProblemDetails();

// ── Datos y usuario actual (multi-tenant) ────────────────────────────────
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<UsuarioActual>();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// ── Autenticación: cookie para el panel, JWT para la app móvil ───────────
var jwt = builder.Configuration.GetSection("Jwt").Get<OpcionesJwt>() ?? new OpcionesJwt();
if (string.IsNullOrWhiteSpace(jwt.Clave) || jwt.Clave.Length < 32)
    throw new InvalidOperationException("Configura Jwt:Clave (32 caracteres o más) en appsettings o en variables de entorno.");
builder.Services.AddSingleton(jwt);
builder.Services.AddScoped<ServicioAutenticacion>();

builder.Services.AddAuthentication(EsquemasAuth.Mixto)
    .AddPolicyScheme(EsquemasAuth.Mixto, EsquemasAuth.Mixto, o =>
        o.ForwardDefaultSelector = ctx =>
            ctx.Request.Headers.Authorization.ToString().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                ? EsquemasAuth.Jwt
                : EsquemasAuth.Cookie)
    .AddCookie(EsquemasAuth.Cookie, o =>
    {
        o.LoginPath = "/Cuenta/Login";
        o.AccessDeniedPath = "/Cuenta/AccesoDenegado";
        o.Cookie.Name = "gg.panel";
        o.Cookie.HttpOnly = true;
        o.SlidingExpiration = true;
        o.ExpireTimeSpan = TimeSpan.FromHours(12);
        o.Events = new CookieAuthenticationEvents
        {
            // La API responde 401/403 en lugar de redirigir al login del panel.
            OnRedirectToLogin = ctx => EsApi(ctx.Request) ? Estado(ctx.Response, 401) : Redirigir(ctx),
            OnRedirectToAccessDenied = ctx => EsApi(ctx.Request) ? Estado(ctx.Response, 403) : Redirigir(ctx),
            // Como el proyecto base: una cuenta desactivada o eliminada pierde la sesión.
            OnValidatePrincipal = async ctx =>
            {
                if (!await UsuarioActivo(ctx.HttpContext, ctx.Principal)) ctx.RejectPrincipal();
            },
        };
    })
    .AddJwtBearer(EsquemasAuth.Jwt, o =>
    {
        o.MapInboundClaims = false;
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwt.Emisor,
            ValidAudience = jwt.Emisor,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Clave)),
            NameClaimType = ClaimTypes.Email,
            RoleClaimType = ClaimTypes.Role,
        };
        o.Events = new JwtBearerEvents
        {
            OnTokenValidated = async ctx =>
            {
                if (!await UsuarioActivo(ctx.HttpContext, ctx.Principal)) ctx.Fail("Esta cuenta ya no está disponible.");
            },
        };
    });

// Niveles jerárquicos del proyecto base (guards de NestJS → políticas).
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(Politicas.Staff, p => p.RequireAssertion(c => Nivel(c.User) >= Niveles.Staff))
    .AddPolicy(Politicas.Asesor, p => p.RequireAssertion(c => Nivel(c.User) >= Niveles.Asesor))
    .AddPolicy(Politicas.Admin, p => p.RequireAssertion(c => Nivel(c.User) >= Niveles.Recepcionista))
    .AddPolicy(Politicas.Gerente, p => p.RequireAssertion(c => Nivel(c.User) >= Niveles.Gerente))
    .AddPolicy(Politicas.SuperAdmin, p => p.RequireAssertion(c => Nivel(c.User) >= Niveles.SuperAdmin))
    .AddPolicy(Politicas.Escaner, p => p.RequireAssertion(c => Nivel(c.User) is >= Niveles.Recepcionista and < Niveles.SuperAdmin));

// Límite de intentos (igual que @Throttle del proyecto base).
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy(Politicas.LimiteLogin, ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "anon",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));
    o.AddPolicy(Politicas.LimiteRecuperacion, ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "anon",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(15) }));
});

builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins(builder.Configuration.GetSection("Cors:Origenes").Get<string[]>() ?? [])
        .AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddServiciosDeNegocio(builder.Configuration);

var app = builder.Build();

// ── Base de datos: migraciones y datos de prueba ─────────────────────────
if (app.Configuration.GetValue("BaseDatos:MigrarAlIniciar", true))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    if (app.Configuration.GetValue("BaseDatos:CargarDatosDePrueba", app.Environment.IsDevelopment()))
        await SeedData.CargarAsync(db, app.Environment.ContentRootPath, app.Logger);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseExceptionHandler();
    app.MapOpenApi();
}

app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture(cultura),
    SupportedCultures = [cultura],
    SupportedUICultures = [cultura],
});
app.UseHttpsRedirection();
app.UseRouting();
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapControllers();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// Verificación del entorno: la API responde y llega a SQL Server.
app.MapGet("/api/salud", async (AppDbContext db) =>
{
    var baseDeDatos = await db.Database.CanConnectAsync();
    return Results.Ok(new { estado = "ok", baseDeDatos });
});

app.Run();

static bool EsApi(HttpRequest r) => r.Path.StartsWithSegments("/api");

static Task Estado(HttpResponse r, int codigo)
{
    r.StatusCode = codigo;
    return Task.CompletedTask;
}

static Task Redirigir(RedirectContext<CookieAuthenticationOptions> ctx)
{
    ctx.Response.Redirect(ctx.RedirectUri);
    return Task.CompletedTask;
}

static int Nivel(ClaimsPrincipal u) => int.TryParse(u.FindFirstValue(Claims.Nivel), out var n) ? n : 0;

static async Task<bool> UsuarioActivo(HttpContext http, ClaimsPrincipal? principal)
{
    if (!int.TryParse(principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id)) return false;
    var db = http.RequestServices.GetRequiredService<AppDbContext>();
    return await db.Usuarios.AnyAsync(u => u.Id == id && u.Activo);
}

public partial class Program;
