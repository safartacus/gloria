using System.Text;
using Gloria.Api.Auth;
using Gloria.Api.Configuration;
using Gloria.Api.Middleware;
using Gloria.Application;
using Gloria.Application.Abstractions;
using Gloria.Infrastructure;
using Gloria.Infrastructure.Persistence;
using Gloria.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

// A minimal logger for the window before configuration is read; replaced by the configured
// pipeline below. Guarantees a startup crash still lands somewhere.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Serilog is the whole logging pipeline: reads the "Serilog" section (levels, Console + Seq
    // sinks), enriches every event from the ambient LogContext (correlation id, request info).
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    builder.Services.Configure<GloriaOptions>(builder.Configuration.GetSection(GloriaOptions.SectionName));
    builder.Services.Configure<SampleImportOptions>(builder.Configuration.GetSection(SampleImportOptions.SectionName));
    builder.Services.AddSingleton<SampleImportSource>();

    builder.Services.AddControllers();
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<ICurrentUser, CurrentUser>();

    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddApplication();

    var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.MapInboundClaims = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwt.Issuer,
                ValidAudience = jwt.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                ClockSkew = TimeSpan.FromSeconds(30),
                NameClaimType = System.Security.Claims.ClaimTypes.Name,
                RoleClaimType = System.Security.Claims.ClaimTypes.Role,
            };
        });
    builder.Services.AddAuthorization();

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Gloria Commission API",
            Version = "v1",
            Description = "Automated staff commission system. Log in via POST /api/auth/login (seeded default " +
                          "password: Gloria2026! — you must change it at first login), then click **Authorize** and paste the access token.",
        });

        var scheme = new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "JWT access token from /api/auth/login",
            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" },
        };
        c.AddSecurityDefinition("Bearer", scheme);
        c.AddSecurityRequirement(new OpenApiSecurityRequirement { [scheme] = Array.Empty<string>() });
    });

    var app = builder.Build();

    // Request logging is the outermost middleware so its one-line-per-request summary records the
    // final status the exception handler writes (e.g. 401/409), not the in-flight exception.
    app.UseSerilogRequestLogging(options =>
    {
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("CorrelationId", System.Diagnostics.Activity.Current?.Id ?? httpContext.TraceIdentifier);
            diagnosticContext.Set("User", httpContext.User.Identity?.Name ?? "anonymous");
        };
    });
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Gloria Commission API v1"));

    app.UseAuthentication();
    app.UseAuthorization();

    // After auth so the log context can capture the authenticated user; still wraps every endpoint.
    app.UseMiddleware<ExceptionHandlingMiddleware>();

    app.MapControllers();
    app.MapGet("/", () => Results.Redirect("/swagger"));

    await InitializeAsync(app);

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

static async Task InitializeAsync(WebApplication app)
{
    var options = app.Services.GetRequiredService<IOptions<GloriaOptions>>().Value;
    if (!options.AutoMigrate) return;

    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
    var db = scope.ServiceProvider.GetRequiredService<GloriaDbContext>();

    logger.LogInformation("Applying database migrations (includes reference-data seed)...");
    await db.Database.MigrateAsync();
}
