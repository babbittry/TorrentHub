using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
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
            builder.Services.AddControllers();
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

            // Configure Garnet as distributed cache
            builder.Services.AddStackExchangeRedisCache(options =>
            {
                var configString = builder.Configuration.GetSection("Garnet:Configuration").Value;
                var username = builder.Configuration.GetSection("Garnet:Username").Value;
                var password = builder.Configuration.GetSection("Garnet:Password").Value;

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
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
                };
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage(); // 添加详细异常页面
                
                // 添加全局异常处理中间件
                app.Use(async (context, next) =>
                {
                    try
                    {
                        await next();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"=== 全局异常捕获 ===");
                        Console.WriteLine($"异常类型: {ex.GetType().FullName}");
                        Console.WriteLine($"异常消息: {ex.Message}");
                        Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
                        
                        if (ex.InnerException != null)
                        {
                            Console.WriteLine($"内部异常类型: {ex.InnerException.GetType().FullName}");
                            Console.WriteLine($"内部异常消息: {ex.InnerException.Message}");
                        }
                        
                        throw; // 重新抛出异常
                    }
                });
                
                // 临时调试：如果还有问题，可以注释掉这些行来测试
                try
                {
                    app.MapScalarApiReference(); // scalar/v1
                    app.MapOpenApi();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"OpenAPI配置错误: {ex.Message}");
                    Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
                }
            }

            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
