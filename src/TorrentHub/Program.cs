using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using StackExchange.Redis;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using HealthChecks.Redis;
using Meilisearch;
using Serilog;
using Serilog.Formatting.Compact;
using TorrentHub.Core.Data;
using TorrentHub.Services;
using TorrentHub.Services.Interfaces;
using TorrentHub.Services.Background;
using TorrentHub.Services.Configuration;
using TorrentHub.Core.Services;
using TorrentHub.Core.Enums;
using TorrentHub.Data;

namespace TorrentHub;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // --- Serilog Configuration ---
        builder.Host.UseSerilog((context, services, configuration) => configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File(new CompactJsonFormatter(), "logs/log.json", rollingInterval: RollingInterval.Day)
        );

        // --- End Serilog --- 

        builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

        builder.Services.AddControllers();

        builder.Services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, context, cancellationToken) =>
            {
                // Configure enums to show as strings with descriptions in OpenAPI
                document.Components ??= new();
                document.Components.Schemas ??= new Dictionary<string, Microsoft.OpenApi.Models.OpenApiSchema>();
                
                return Task.CompletedTask;
            });
        });

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowSpecificOrigin",
                builder =>
                {
                    builder.WithOrigins("http://localhost:3000")
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
        });

        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"), npgsqlOptions =>
            {
                npgsqlOptions.MapEnum<UserRole>();
                npgsqlOptions.MapEnum<BadgeCode>();
                npgsqlOptions.MapEnum<ForumCategoryCode>();
                npgsqlOptions.MapEnum<ReportReason>();
                npgsqlOptions.MapEnum<RequestStatus>();
                npgsqlOptions.MapEnum<StoreItemCode>();
                npgsqlOptions.MapEnum<TorrentCategory>();
                npgsqlOptions.MapEnum<TorrentStickyStatus>();
                npgsqlOptions.MapEnum<BanStatus>();
                npgsqlOptions.MapEnum<ReactionType>();
                npgsqlOptions.MapEnum<CommentableType>(); // Add new enum mapping
            }));

        // Add custom services
        builder.Services.AddScoped<IUserService, UserService>();
        builder.Services.AddScoped<ITorrentCredentialService, TorrentCredentialService>();
        builder.Services.AddScoped<IRssFeedTokenService, RssFeedTokenService>();
        
        builder.Services.AddScoped<IStoreService, StoreService>();
        builder.Services.AddScoped<ITorrentService, TorrentService>();
        builder.Services.AddScoped<ICommentService, CommentService>(); // New unified comment service
        builder.Services.AddScoped<IRequestService, RequestService>();
        builder.Services.AddScoped<IMessageService, MessageService>();
        builder.Services.AddScoped<IReportService, ReportService>();
        builder.Services.AddScoped<IAnnouncementService, AnnouncementService>();
        builder.Services.AddScoped<IUserLevelService, UserLevelService>();
        builder.Services.AddScoped<ITopPlayersService, TopPlayersService>();
        builder.Services.AddScoped<ITorrentListingService, TorrentListingService>();
        builder.Services.AddScoped<IMeiliSearchService, MeiliSearchService>();
        builder.Services.AddScoped<IStatsService, StatsService>();
        builder.Services.AddScoped<ISettingsService, SettingsService>();
        builder.Services.AddScoped<IForumTopicService, ForumTopicService>(); // New forum topic management service
        builder.Services.AddScoped<INotificationService, NotificationService>();
        builder.Services.AddScoped<IPollService, PollService>();
        builder.Services.AddScoped<IAdminService, AdminService>();
        builder.Services.AddScoped<IReactionService, ReactionService>();

        var garnetConnectionString = builder.Configuration.GetConnectionString("Garnet");
        if (!string.IsNullOrEmpty(garnetConnectionString))
        {
            var configOptions = ConfigurationOptions.Parse(garnetConnectionString);
            configOptions.AbortOnConnectFail = false;
            configOptions.ConnectRetry = 5;
            configOptions.ConnectTimeout = 5000;
            configOptions.ReconnectRetryPolicy = new ExponentialRetry(1000);

            builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(configOptions));
            builder.Services.AddStackExchangeRedisCache(options =>
            {
                options.ConfigurationOptions = configOptions;
                options.InstanceName = "TorrentHub:";
            });

            builder.Services.AddHealthChecks().AddRedis(garnetConnectionString, name: "garnet-cache");
        }

        builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));
        builder.Services.AddTransient<IEmailService, EmailService>();

        builder.Services.Configure<TMDbSettings>(builder.Configuration.GetSection("TMDbSettings"));
        builder.Services.AddHttpClient<ITMDbService, TMDbService>((serviceProvider, client) =>
        {
            var settings = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<TMDbSettings>>().Value;
            if (string.IsNullOrEmpty(settings.BaseUrl) || !Uri.TryCreate(settings.BaseUrl, UriKind.Absolute, out _))
            {
                throw new InvalidOperationException("TMDb BaseUrl is not configured or is not a valid absolute URI.");
            }
            if (string.IsNullOrEmpty(settings.AccessToken))
            {
                throw new InvalidOperationException("TMDb AccessToken is not configured. Please set 'TMDbSettings:AccessToken' in your configuration file.");
            }
            
            client.BaseAddress = new Uri(settings.BaseUrl);
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {settings.AccessToken}");
            client.DefaultRequestHeaders.Add("accept", "application/json");
        });

        // 配置豆瓣服务
        builder.Services.Configure<DoubanSettings>(builder.Configuration.GetSection("DoubanSettings"));
        builder.Services.AddHttpClient<DoubanService>();
        
        // 配置凭证清理服务
        builder.Services.Configure<CredentialSettings>(builder.Configuration.GetSection("CredentialSettings"));
        
        // 注册输入解析器 (单例即可)
        builder.Services.AddSingleton<MediaInputParser>();

        builder.Services.AddSingleton(sp =>
        {
            var configuration = sp.GetRequiredService<IConfiguration>();
            var url = configuration.GetValue<string>("MeiliSearch:Url");
            var apiKey = configuration.GetValue<string>("MeiliSearch:ApiKey");

            if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("MeiliSearch URL or ApiKey is not configured.");
            }
            
            return new MeilisearchClient(url, apiKey);
        });

        builder.Services.AddHostedService<CoinGenerationService>();
        builder.Services.AddHostedService<UserLevelBackgroundService>();
        builder.Services.AddHostedService<TopPlayersCacheRefreshService>();
        builder.Services.AddHostedService<PeerCountUpdateService>();
        builder.Services.AddHostedService<StatsCacheRefreshService>();
        builder.Services.AddHostedService<RequestConfirmationService>();
        builder.Services.AddHostedService<CredentialCleanupService>();
        builder.Services.AddHostedService<RssFeedTokenCleanupService>();

        var jwtKey = builder.Configuration["Jwt:Key"];
        if (string.IsNullOrEmpty(jwtKey))
        {
            throw new InvalidOperationException("JWT Key is not configured. Please set 'Jwt:Key' in your configuration file (e.g., appsettings.json).");
        }

        var key = Encoding.UTF8.GetBytes(jwtKey);

        builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            });

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.MapOpenApi();
            app.MapScalarApiReference();

            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var context = services.GetRequiredService<ApplicationDbContext>();
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    var tmdbService = services.GetRequiredService<ITMDbService>();

                    await context.Database.MigrateAsync();

                    await DataSeeder.SeedFoundationalDataAsync(context, logger);
                    if (app.Environment.IsDevelopment())
                    {
                        await DataSeeder.SeedMockDataAsync(context, logger, tmdbService, app.Environment);
                    }

                    // Preheat configuration cache
                    var settingsService = services.GetRequiredService<ISettingsService>();
                    await settingsService.GetSiteSettingsAsync();
                    logger.LogInformation("Configuration cache preheated successfully");
                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "An error occurred while seeding the database.");
                }
            }
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseCors("AllowSpecificOrigin");

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        app.MapHealthChecks("/health");

        // Preheat configuration cache for production
        using (var scope = app.Services.CreateScope())
        {
            var services = scope.ServiceProvider;
            try
            {
                var logger = services.GetRequiredService<ILogger<Program>>();
                var settingsService = services.GetRequiredService<ISettingsService>();
                
                // Preheat site settings cache
                await settingsService.GetSiteSettingsAsync();
                logger.LogInformation("Configuration cache preheated at startup");
            }
            catch (Exception ex)
            {
                var logger = services.GetRequiredService<ILogger<Program>>();
                logger.LogWarning(ex, "Failed to preheat configuration cache");
                // Don't block application startup
            }
        }

        app.Run();
    }
}

