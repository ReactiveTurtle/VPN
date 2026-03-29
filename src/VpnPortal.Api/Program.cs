using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using VpnPortal.Application.Interfaces;
using VpnPortal.Infrastructure.Options;
using VpnPortal.Infrastructure.Persistence.InMemory;
using VpnPortal.Infrastructure.Persistence.PostgreSql;
using VpnPortal.Infrastructure.Security;
using VpnPortal.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<DatabaseOptions>(builder.Configuration.GetSection(DatabaseOptions.SectionName));
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection(EmailOptions.SectionName));
builder.Services.Configure<InternalApiOptions>(builder.Configuration.GetSection(InternalApiOptions.SectionName));

var databaseOptions = builder.Configuration.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>() ?? new DatabaseOptions();

if (string.Equals(databaseOptions.Provider, "PostgreSql", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddSingleton(databaseOptions);
    builder.Services.AddSingleton<PostgreSqlConnectionFactory>();
    builder.Services.AddSingleton<PostgreSqlDatabaseBootstrapper>();
    builder.Services.AddScoped<IRequestRepository, PostgreSqlRequestRepository>();
    builder.Services.AddScoped<IUserRepository, PostgreSqlUserRepository>();
    builder.Services.AddScoped<IAccountTokenRepository, PostgreSqlAccountTokenRepository>();
    builder.Services.AddScoped<ISuperAdminRepository, PostgreSqlSuperAdminRepository>();
    builder.Services.AddScoped<IDeviceRepository, PostgreSqlDeviceRepository>();
    builder.Services.AddScoped<IDeviceCredentialRepository, PostgreSqlDeviceCredentialRepository>();
    builder.Services.AddScoped<ITrustedIpRepository, PostgreSqlTrustedIpRepository>();
    builder.Services.AddScoped<IIpChangeConfirmationRepository, PostgreSqlIpChangeConfirmationRepository>();
    builder.Services.AddScoped<IAuditLogRepository, PostgreSqlAuditLogRepository>();
    builder.Services.AddScoped<ISessionRepository, PostgreSqlSessionRepository>();
}
else
{
    builder.Services.AddSingleton<InMemoryPortalStore>();
    builder.Services.AddScoped<IRequestRepository, InMemoryRequestRepository>();
    builder.Services.AddScoped<IUserRepository, InMemoryUserRepository>();
    builder.Services.AddScoped<IAccountTokenRepository, InMemoryAccountTokenRepository>();
    builder.Services.AddScoped<ISuperAdminRepository, InMemorySuperAdminRepository>();
    builder.Services.AddScoped<IDeviceRepository, InMemoryDeviceRepository>();
    builder.Services.AddScoped<IDeviceCredentialRepository, InMemoryDeviceCredentialRepository>();
    builder.Services.AddScoped<ITrustedIpRepository, InMemoryTrustedIpRepository>();
    builder.Services.AddScoped<IIpChangeConfirmationRepository, InMemoryIpChangeConfirmationRepository>();
    builder.Services.AddSingleton<IAuditLogRepository, InMemoryAuditLogRepository>();
    builder.Services.AddScoped<ISessionRepository, InMemorySessionRepository>();
}

builder.Services.AddScoped<IRequestService, RequestService>();
builder.Services.AddScoped<IUserPortalService, UserPortalService>();
builder.Services.AddScoped<IAccountActivationService, AccountActivationService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAdminOperationsService, AdminOperationsService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IVpnAccountingService, VpnAccountingService>();
builder.Services.AddSingleton<IPasswordHasher, Argon2PasswordHasher>();
builder.Services.AddSingleton<IVpnPasswordMaterialService, VpnPasswordMaterialService>();
builder.Services.AddSingleton<ITokenProtector, Sha256TokenProtector>();
builder.Services.AddSingleton<DatabaseStatusService>();

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
            .WithOrigins("http://localhost:4200")
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

builder.Services.AddControllers(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});
builder.Services.AddOpenApi();

var app = builder.Build();

if (string.Equals(databaseOptions.Provider, "PostgreSql", StringComparison.OrdinalIgnoreCase))
{
    using var scope = app.Services.CreateScope();
    var bootstrapper = scope.ServiceProvider.GetRequiredService<PostgreSqlDatabaseBootstrapper>();
    await bootstrapper.InitializeAsync(CancellationToken.None);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

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
