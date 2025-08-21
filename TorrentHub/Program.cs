using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using StackExchange.Redis;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using HealthChecks.Redis;
using TorrentHub.Data;
using TorrentHub.Services;
using Meilisearch;
using Microsoft.Extensions.Options;

namespace TorrentHub
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(
                        new System.Text.Json.Serialization.JsonStringEnumConverter());
                });
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            // Add CORS services
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowSpecificOrigin",
                    builder =>
                    {
                        builder.WithOrigins("http://localhost:3000") // 允许前端的源
                            .AllowAnyHeader()
                            .AllowAnyMethod()
                            .AllowCredentials(); // 允许凭据
                    });
            });

            // Add DbContext
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

            // Add custom services
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IAnnounceService, AnnounceService>();
            builder.Services.AddScoped<IStoreService, StoreService>();
            builder.Services.AddScoped<ITorrentService, TorrentService>();
            builder.Services.AddScoped<ICommentService, CommentService>();
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
            builder.Services.AddScoped<IForumService, ForumService>();

            // Configure Garnet with resilience and health checks
            var garnetConnectionString = builder.Configuration.GetConnectionString("Garnet");
            if (!string.IsNullOrEmpty(garnetConnectionString))
            {
                var configOptions = ConfigurationOptions.Parse(garnetConnectionString);
                configOptions.AbortOnConnectFail = false; // Key setting for resilience
                configOptions.ConnectRetry = 5;
                configOptions.ConnectTimeout = 5000;
                configOptions.ReconnectRetryPolicy = new ExponentialRetry(1000);

                builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(configOptions));
                builder.Services.AddStackExchangeRedisCache(options =>
                {
                    options.ConfigurationOptions = configOptions;
                    options.InstanceName = "TorrentHub:";
                });

                // Add Health Checks for Redis
                builder.Services.AddHealthChecks().AddRedis(garnetConnectionString, name: "garnet-cache");
            }

            // Configure SMTP Settings
            builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));
            builder.Services.AddTransient<IEmailService, EmailService>();

            // Configure Coin Settings
            builder.Services.Configure<CoinSettings>(builder.Configuration.GetSection("CoinSettings"));

            // Configure Torrent Settings
            builder.Services.Configure<TorrentSettings>(builder.Configuration.GetSection("TorrentSettings"));

            // Configure TMDb Settings and Service
            builder.Services.Configure<TMDbSettings>(builder.Configuration.GetSection("TMDbSettings"));
            builder.Services.AddHttpClient<ITMDbService, TMDbService>((serviceProvider, client) =>
            {
                var settings = serviceProvider.GetRequiredService<IOptions<TMDbSettings>>().Value;
                if (string.IsNullOrEmpty(settings.BaseUrl) || !Uri.TryCreate(settings.BaseUrl, UriKind.Absolute, out _))
                {
                    throw new InvalidOperationException("TMDb BaseUrl is not configured or is not a valid absolute URI.");
                }
                client.BaseAddress = new Uri(settings.BaseUrl);
            });

            // Configure MeiliSearch
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

            // Add background services
            builder.Services.AddHostedService<CoinGenerationService>();
            builder.Services.AddHostedService<UserLevelBackgroundService>();
            builder.Services.AddHostedService<TopPlayersCacheRefreshService>();
            builder.Services.AddHostedService<PeerCountUpdateService>();
            builder.Services.AddHostedService<StatsCacheRefreshService>();

            // Add Authentication
            var jwtKey = builder.Configuration["Jwt:Key"];
            if (string.IsNullOrEmpty(jwtKey))
            {
                throw new InvalidOperationException(
                    "JWT Key is not configured. Please set 'Jwt:Key' in your configuration file (e.g., appsettings.json).");
            }

            var key = Encoding.UTF8.GetBytes(jwtKey);

            builder.Services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.RequireHttpsMetadata = false; // In production, this should be true
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

                    // Configure the JWT bearer to read the token from the cookie
                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            context.Token = context.Request.Cookies["authToken"];
                            return Task.CompletedTask;
                        }
                    };
                });
            ;

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage(); // 添加详细异常页面
                app.MapOpenApi();
                app.MapScalarApiReference(); // scalar/v1

                // Seed default admin user
                using (var scope = app.Services.CreateScope())
                {
                    var services = scope.ServiceProvider;
                    try
                    {
                        var context = services.GetRequiredService<ApplicationDbContext>();
                        var logger = services.GetRequiredService<ILogger<Program>>();
                        var tmdbService = services.GetRequiredService<ITMDbService>();

                        // 确保数据库已迁移到最新版本
                        await context.Database.MigrateAsync();

                        await DataSeeder.SeedAllDataAsync(context, logger, tmdbService);
                    }
                    catch (Exception ex)
                    {
                        var logger = services.GetRequiredService<ILogger<Program>>();
                        logger.LogError(ex, "An error occurred while seeding the database.");
                    }
                }
            }

            app.UseHttpsRedirection();

            // Use CORS policy
            app.UseCors("AllowSpecificOrigin");

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.MapHealthChecks("/health");

            app.Run();
        }
    }
}
