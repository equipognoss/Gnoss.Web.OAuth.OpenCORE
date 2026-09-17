using Es.Riam.Gnoss.HealthChecks;
using Es.Riam.Gnoss.Util.General;
using Gnoss.Web.OAuth.Application.Settings;
using Gnoss.Web.OAuth.Application.UseCases.VerifyOAuthRequest;
using Gnoss.Web.OAuth.Domain.Interfaces;
using Gnoss.Web.OAuth.Infrastructure.Cache;
using Gnoss.Web.OAuth.Infrastructure.Persistence;
using Gnoss.Web.OAuth.Infrastructure.Persistence.Repositories;
using Gnoss.Web.OAuth.Infrastructure.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Serilog;
using StackExchange.Redis;
using System;
using System.IO;
using System.Reflection;
using System.Threading.RateLimiting;
using System.Threading.Tasks;

Serilog.ILogger _startupLogger = LoggingService.ConfigurarBasicStartupSerilog().CreateBootstrapLogger().ForContext<Program>(); ;
try
{
    var builder = WebApplication.CreateBuilder(args);

    StartupValidator.Validate(builder.Configuration);
    AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

    builder.Host.ConfigureAppConfiguration((hostContext, config) => LoggingService.ConfigurarSeguimientoFicheros(hostContext, config, _startupLogger))
        .UseSerilog((context, services, configuration) => LoggingService.ConfigurarSerilog(context.Configuration, services, configuration))
        .ConfigureServices((context, services) =>
        {
            LoggingService.SuscribirCambios(context, _startupLogger);
            _startupLogger.Information("Suscripcion a cambios de configuracion registrada");
        });
    var apiPort = builder.Configuration.GetValue("ApiPort", 8080);
    var managementPort = builder.Configuration.GetValue("ManagementPort", 8081);
#if !DEBUG
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.AddServerHeader = false;
        options.ListenAnyIP(apiPort);        // API expuesto en ingress / reverse proxy
        options.ListenAnyIP(managementPort); // Management solo red interna del cluster
    });
#endif

    builder.Services.AddControllers();

    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "OAuthVerification API",
            Version = "v1",
            Description = "Servicio de verificacion de firmas OAuth 1.0 HMAC-SHA1. " +
                          "Valida timestamp, unicidad de nonce, existencia del token y firma HMAC-SHA1 " +
                          "antes de devolver el GUID del usuario propietario de los tokens."
        });

        // Incluir los comentarios XML del proyecto en la documentacion Swagger
        var xmlPath = Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
        options.IncludeXmlComments(xmlPath);
    });

    // IOptions<OAuthSettings> - validado con DataAnnotations al arrancar
    builder.Services.AddOptions<OAuthSettings>()
        .BindConfiguration("OAuthSettings")
        .ValidateDataAnnotations()
        .ValidateOnStart();

    var dbProvider = builder.Configuration.GetValue<string>("connectionType", "2")!;
    var connectionString = builder.Configuration.GetValue<string>("oauth")!;

    builder.Services.AddDbContextFactory<AppDbContext>(options =>
    {
        if (dbProvider.Equals("0", StringComparison.OrdinalIgnoreCase))
        {
            options.UseSqlServer(connectionString, o =>
            {
                o.EnableRetryOnFailure(maxRetryCount: 3);
                o.CommandTimeout(15);
            });
        }
        else if (dbProvider.Equals("1", StringComparison.OrdinalIgnoreCase))
        {
            options.UseOracle(connectionString, o =>
            {                
                o.CommandTimeout(15);
            });
        }
        else
        {
            options.UseNpgsql(connectionString, o =>
            {
                o.EnableRetryOnFailure(maxRetryCount: 3);
                o.CommandTimeout(15);
            });
        }            

        options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);

        if (builder.Environment.IsDevelopment())
            options.EnableSensitiveDataLogging();
    });

    builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
        ConnectionMultiplexer.Connect(builder.Configuration.GetValue<string>("redis:redis:ip:master")!));

    // Application
    builder.Services.AddScoped<IVerifyOAuthRequestUseCase, VerifyOAuthRequestUseCase>();

    // Infrastructure
    builder.Services.AddScoped<ITokenRepository, TokenRepository>();
    builder.Services.AddScoped<IConsumerRepository, ConsumerRepository>();
    builder.Services.AddSingleton<INonceStore, RedisNonceStore>();

    builder.Services.AddHealthChecks()
        .AddGnossDatabaseHealthCheck<AppDbContext>(dbProvider, connectionString)
        .AddGnossRedisHealthCheck(builder.Configuration.GetValue<string>("redis:redis:ip:master")!);

    // Rate limiting ventana deslizante particionada por token OAuth (fallback a IP)
    // Cada token tiene su propio bucket independiente, por lo que un token comprometido
    // o abusivo no afecta al cupo del resto de tokens, aunque compartan IP.
    OAuthSettings oAuthSettings = new OAuthSettings();
    var oauthCfg = builder.Configuration.GetSection("OAuthSettings");
    builder.Services.AddRateLimiter(rl =>
    {
        rl.AddPolicy<string>("oauth", context =>
        {
            var key = OAuthRateLimitKeyExtractor.Extract(context.Request);

            return RateLimitPartition.GetSlidingWindowLimiter(key, _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = oauthCfg.GetValue("RateLimitPermitLimit", oAuthSettings.RateLimitPermitLimit),
                Window = TimeSpan.FromSeconds(oauthCfg.GetValue("RateLimitWindowSeconds", oAuthSettings.RateLimitWindowSeconds)),
                SegmentsPerWindow = oauthCfg.GetValue("RateLimitSegments", oAuthSettings.RateLimitSegments),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
        });

        rl.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        rl.OnRejected = (ctx, _) =>
        {
            ctx.HttpContext.Response.Headers.RetryAfter =
                oauthCfg.GetValue("RateLimitWindowSeconds", oAuthSettings.RateLimitWindowSeconds).ToString();
            return ValueTask.CompletedTask;
        };
    });

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        //await using var scope = app.Services.CreateAsyncScope();
        //var db = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
        //await using var ctx = await db.CreateDbContextAsync();
        //await DbSeeder.SeedAsync(ctx);
    }

    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "OAuthVerification v1");
        options.RoutePrefix = "swagger";
        options.DocumentTitle = "OAuthVerification API";
        options.DefaultModelsExpandDepth(-1); // ocultar seccion de schemas por defecto
    });

    app.UseRateLimiter();
    app.MapControllers();

    app.MapGnossHealthEndpoints(managementPort);

    app.Run();
}
catch (Exception ex)
{
    _startupLogger.Fatal(ex, "Error fatal durante el arranque");
}
finally
{
    (_startupLogger as IDisposable)?.Dispose();
    Log.CloseAndFlush(); // asegura que se escriben todos los logs pendientes
}

