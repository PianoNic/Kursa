using Kursa.API.DevAuth;
using Kursa.API.Middleware;
using Kursa.Application;
using Kursa.Infrastructure;
using Kursa.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, configuration) =>
        configuration.ReadFrom.Configuration(context.Configuration));

    #region Configure Services
    builder.Services.AddControllers();
    builder.Services.AddSpaStaticFiles(options => { options.RootPath = "wwwroot/browser"; });
    builder.Services.AddProblemDetails();
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

    if (builder.Environment.IsDevelopment())
    {
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.WithOrigins("http://localhost:4200")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });
    }
    #endregion

    #region API Documentation
    builder.Services.AddSwaggerGen();
    builder.Services.AddEndpointsApiExplorer();
    #endregion

    #region Authentication
    bool useDevAuth = builder.Environment.IsDevelopment()
        && builder.Configuration.GetValue("DevAuth:Enabled", false);

    if (useDevAuth)
    {
        Log.Warning("⚠️  No OIDC authority configured — using DevAuth bypass. DO NOT use in production.");
        builder.Services.AddAuthentication(DevAuthenticationHandler.SchemeName)
            .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, DevAuthenticationHandler>(
                DevAuthenticationHandler.SchemeName, _ => { });
    }
    else
    {
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = builder.Configuration["Oidc:Authority"];
                options.RequireHttpsMetadata = builder.Configuration.GetValue("Oidc:RequireHttpsMetadata", true);
                options.TokenValidationParameters.NameClaimType = "name";
                options.TokenValidationParameters.RoleClaimType = "role";
                options.TokenValidationParameters.ValidateAudience = false;
            });
    }

    builder.Services.AddAuthorization();
    #endregion

    #region Application & Infrastructure
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);
    #endregion

    var app = builder.Build();

    #region Database Initialization with Retry Logic
    bool dbConnected = false;
    int retryCount = 0;
    const int maxRetries = 10;
    const int retryDelaySeconds = 5;

    var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();

    while (!dbConnected && retryCount < maxRetries)
    {
        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            try
            {
                startupLogger.LogInformation(
                    "Attempting to connect to the database and apply migrations (Attempt {Attempt}/{MaxRetries})...",
                    retryCount + 1, maxRetries);
                dbContext.Database.Migrate();
                dbConnected = true;
                startupLogger.LogInformation("Database connection successful and migrations applied.");
            }
            catch (NpgsqlException ex)
            {
                startupLogger.LogError(ex, "Database connection failed: {ErrorMessage}", ex.Message);
                retryCount++;
                if (retryCount < maxRetries)
                {
                    startupLogger.LogInformation("Retrying in {Delay} seconds...", retryDelaySeconds);
                    Thread.Sleep(TimeSpan.FromSeconds(retryDelaySeconds));
                }
                else
                {
                    startupLogger.LogCritical(
                        "Failed to connect to the database after {MaxRetries} retries. Application will now terminate.",
                        maxRetries);
                    throw;
                }
            }
            catch (Exception ex)
            {
                startupLogger.LogError(ex, "An unexpected error occurred during database migration: {ErrorMessage}", ex.Message);
                retryCount++;
                if (retryCount < maxRetries)
                {
                    startupLogger.LogInformation("Retrying in {Delay} seconds...", retryDelaySeconds);
                    Thread.Sleep(TimeSpan.FromSeconds(retryDelaySeconds));
                }
                else
                {
                    startupLogger.LogCritical(
                        "Failed to perform database operations after {MaxRetries} retries. Application will now terminate.",
                        maxRetries);
                    throw;
                }
            }
        }
    }
    #endregion

    #region Dev Auth Seeding
    if (useDevAuth)
    {
        using var scope = app.Services.CreateScope();
        var userSync = scope.ServiceProvider.GetRequiredService<Kursa.Application.Common.Interfaces.IUserSyncService>();
        await userSync.SyncUserAsync(
            DevAuthenticationHandler.DevUserId,
            "dev@kursa.local",
            "Dev User");
    }
    #endregion

    #region Configure HTTP Pipeline
    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseExceptionHandler();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Kursa API V1");
        });
    }

    app.UseSerilogRequestLogging();
    app.UseStaticFiles();

    if (!app.Environment.IsDevelopment())
    {
        app.UseSpaStaticFiles();
    }

    app.UseRouting();

    if (app.Environment.IsDevelopment())
    {
        app.UseCors();
    }

    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
    #endregion

    #region SPA Configuration
    if (!app.Environment.IsDevelopment())
    {
        app.UseSpa(spa =>
        {
            spa.Options.SourcePath = "wwwroot";
        });
    }
    #endregion

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
