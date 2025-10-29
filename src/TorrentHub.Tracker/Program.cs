using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using TorrentHub.Core.Data;
using TorrentHub.Core.Enums;
using TorrentHub.Core.Services;
using TorrentHub.Tracker.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// --- Database Configuration ---
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
    }));

// --- Redis Cache Configuration ---
var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrEmpty(redisConnectionString))
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnectionString;
        options.InstanceName = "TorrentHubTracker:";
    });
}
else
{
    // Use in-memory cache as a fallback if Redis is not configured
    builder.Services.AddDistributedMemoryCache();
}

// --- Service Registration ---
// Register the lightweight, tracker-specific implementations
builder.Services.AddScoped<ISettingsService, SettingsService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<ITorrentCredentialService, TorrentHub.Services.TorrentCredentialService>();
// Register the main announce service
builder.Services.AddScoped<IAnnounceService, AnnounceService>();
builder.Services.AddSingleton<ITrackerLocalizer, TrackerLocalizer>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// app.UseHttpsRedirection(); // Optional: Depends on deployment scenario

app.UseAuthorization();

app.MapControllers();

app.Run();