using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using subscription_api.Data;
using subscription_api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// PostgreSQL with Entity Framework Core
var connectionString = builder.Configuration.GetConnectionString("PostgreSql");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString)
);

// Redis for rate limiting
var redisConnection = builder.Configuration.GetConnectionString("Redis");
var redis = ConnectionMultiplexer.Connect(redisConnection ?? "localhost:6379");
builder.Services.AddSingleton(redis);

// Services
builder.Services.AddScoped<IRateLimitService, RateLimitService>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();

// Hangfire with PostgreSQL storage
builder.Services.AddHangfire(config =>
    config.UsePostgreSqlStorage(connectionString)
);
builder.Services.AddHangfireServer();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Hangfire Dashboard (optional, accessible at /hangfire)
app.UseHangfireDashboard();

// Configure Hangfire recurring job
RecurringJob.AddOrUpdate<ISubscriptionService>(
    "DailySubscriptionJob",
    service => service.RunDailyJobAsync(),
    "0 2 * * *" // Cron: 02:00 UTC every day
);

// Run migrations
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate();
}

app.Run();
