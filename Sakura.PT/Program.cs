using System.Text;
using Elasticsearch.Net;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Nest;
using Sakura.PT.Data;
using Sakura.PT.Services;
using Scalar.AspNetCore;
using StackExchange.Redis;

namespace Sakura.PT
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
                });
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();
            
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
            builder.Services.AddScoped<IElasticsearchService, ElasticsearchService>();

            // Configure Garnet as distributed cache
            builder.Services.AddStackExchangeRedisCache(options =>
            {
                var configString = builder.Configuration.GetSection("Garnet:Configuration").Value;
                var username = builder.Configuration.GetSection("Garnet:Username").Value;
                var password = builder.Configuration.GetSection("Garnet:Password").Value;

                if (string.IsNullOrEmpty(configString)) return;
                var configOptions = ConfigurationOptions.Parse(configString);
                if (!string.IsNullOrEmpty(username))
                {
                    configOptions.User = username;
                }
                if (!string.IsNullOrEmpty(password))
                {
                    configOptions.Password = password;
                }
                options.ConfigurationOptions = configOptions;
                options.InstanceName = "SakuraPT:"; // Optional: prefix for cache keys
            });

            // Configure SMTP Settings
            builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));
            builder.Services.AddTransient<IEmailService, EmailService>();

            // Configure SakuraCoin Settings
            builder.Services.Configure<SakuraCoinSettings>(builder.Configuration.GetSection("SakuraCoinSettings"));

            // Configure Torrent Settings
            builder.Services.Configure<TorrentSettings>(builder.Configuration.GetSection("TorrentSettings"));

            // Configure Elasticsearch
            var esUri = builder.Configuration["Elasticsearch:Uri"];
            var esUsername = builder.Configuration["Elasticsearch:Username"];
            var esPassword = builder.Configuration["Elasticsearch:Password"];

            if (string.IsNullOrEmpty(esUri))
            {
                throw new InvalidOperationException("Elasticsearch URI is not configured.");
            }

            var settings = new ConnectionSettings(new Uri(esUri))
                .DefaultIndex("torrents")
                .BasicAuthentication(esUsername, esPassword);

            var client = new ElasticClient(settings);
            builder.Services.AddSingleton<IElasticClient>(client);

            // Add background services
            builder.Services.AddHostedService<SakuraCoinGenerationService>();
            builder.Services.AddHostedService<UserLevelBackgroundService>();
            builder.Services.AddHostedService<TopPlayersCacheRefreshService>();

            // Add Authentication
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key is not configured.")))
                };
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage(); // 添加详细异常页面
                app.MapOpenApi();
                app.MapScalarApiReference(); // scalar/v1
            }

            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
