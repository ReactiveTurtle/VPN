using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using VpnPortal.Application.Interfaces;
using VpnPortal.Application.Services;
using VpnPortal.Infrastructure.Options;
using VpnPortal.Infrastructure.Persistence.Ef;
using VpnPortal.Infrastructure.Persistence.Ef.Repositories;
using VpnPortal.Infrastructure.Security;
using ConsoleEmailService = VpnPortal.Infrastructure.Services.ConsoleEmailService;
using DatabaseStatusService = VpnPortal.Infrastructure.Services.DatabaseStatusService;
using SmtpEmailService = VpnPortal.Infrastructure.Services.SmtpEmailService;
using VpnOnboardingInstructionService = VpnPortal.Infrastructure.Services.VpnOnboardingInstructionService;
using VpnRuntimeControlService = VpnPortal.Infrastructure.Services.VpnRuntimeControlService;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<DatabaseOptions>(builder.Configuration.GetSection(DatabaseOptions.SectionName));
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection(EmailOptions.SectionName));
builder.Services.Configure<InternalApiOptions>(builder.Configuration.GetSection(InternalApiOptions.SectionName));
builder.Services.Configure<VpnAccessOptions>(builder.Configuration.GetSection(VpnAccessOptions.SectionName));
builder.Services.Configure<VpnRuntimeOptions>(builder.Configuration.GetSection(VpnRuntimeOptions.SectionName));

var databaseOptions = builder.Configuration.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>() ?? new DatabaseOptions();
var vpnAccessOptions = builder.Configuration.GetSection(VpnAccessOptions.SectionName).Get<VpnAccessOptions>() ?? new VpnAccessOptions();

databaseOptions.ConnectionString = DatabaseConnectionStringValidator.EnsureValid(
    databaseOptions.ConnectionString,
    "Database:ConnectionString must be configured.");

if (string.IsNullOrWhiteSpace(vpnAccessOptions.ServerAddress))
{
    throw new InvalidOperationException("VpnAccess:ServerAddress must be configured.");
}

if (string.Equals(vpnAccessOptions.ServerAddress.Trim(), "vpn.example.com", StringComparison.OrdinalIgnoreCase))
{
    throw new InvalidOperationException("VpnAccess:ServerAddress must be set to the real VPN server IP or hostname, not the example placeholder.");
}

builder.Services.AddSingleton(databaseOptions);
builder.Services.AddDbContext<VpnPortalDbContext>(options => options.UseNpgsql(databaseOptions.ConnectionString, npgsql => npgsql.MigrationsAssembly("VpnPortal.Migrations")));
builder.Services.AddScoped<IRequestRepository, EfRequestRepository>();
builder.Services.AddScoped<IUserRepository, EfUserRepository>();
builder.Services.AddScoped<IAccountTokenRepository, EfAccountTokenRepository>();
builder.Services.AddScoped<ISuperAdminRepository, EfSuperAdminRepository>();
builder.Services.AddScoped<IDeviceRepository, EfDeviceRepository>();
builder.Services.AddScoped<IDeviceCredentialRepository, EfDeviceCredentialRepository>();
builder.Services.AddScoped<ITrustedIpRepository, EfTrustedIpRepository>();
builder.Services.AddScoped<IAuditLogRepository, EfAuditLogRepository>();
builder.Services.AddScoped<ISessionRepository, EfSessionRepository>();

builder.Services.AddScoped<IRequestService, RequestService>();
builder.Services.AddScoped<IUserPortalService, UserPortalService>();
builder.Services.AddScoped<IAccountActivationService, AccountActivationService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAdminOperationsService, AdminOperationsService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IVpnAccountingService, VpnAccountingService>();
builder.Services.AddSingleton<IVpnRuntimeControlService, VpnRuntimeControlService>();
builder.Services.AddSingleton<IPasswordHasher, Argon2PasswordHasher>();
builder.Services.AddSingleton<IVpnOnboardingInstructionService, VpnOnboardingInstructionService>();
builder.Services.AddSingleton<IVpnPasswordMaterialService, VpnPasswordMaterialService>();
builder.Services.AddSingleton<ITokenProtector, Sha256TokenProtector>();
builder.Services.AddSingleton<ISystemStatusService, DatabaseStatusService>();

var emailOptions = builder.Configuration.GetSection(EmailOptions.SectionName).Get<EmailOptions>() ?? new EmailOptions();
if (emailOptions.Enabled && string.Equals(emailOptions.Provider, "Smtp", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddScoped<IEmailService, SmtpEmailService>();
}
else
{
    builder.Services.AddScoped<IEmailService, ConsoleEmailService>();
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:4200", "http://localhost:5500")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "vpnportal.auth";
        options.Cookie.HttpOnly = true;
        options.SlidingExpiration = true;
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-XSRF-TOKEN";
    options.Cookie.Name = "XSRF-TOKEN";
    options.Cookie.HttpOnly = false;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});
builder.Services.AddOpenApi();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseForwardedHeaders();
app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseCors("frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

var spaIndexPath = Path.Combine(app.Environment.WebRootPath ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot"), "index.html");
if (File.Exists(spaIndexPath))
{
    app.MapFallbackToFile("index.html");
}

app.Run();
