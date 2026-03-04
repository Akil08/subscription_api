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

// is redis is down , what will happen to the api ?
// will not it cause the api to crash ?  dont we need try castch bock here ? 
// If Redis is down, the API will not crash because 
// the RateLimitService is designed to fail open.

var redisConnection = builder.Configuration.GetConnectionString("Redis");
var redis = ConnectionMultiplexer.Connect(redisConnection ?? "localhost:6379");
builder.Services.AddSingleton(redis);

// Services
builder.Services.AddScoped<IRateLimitService, RateLimitService>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();

// Hangfire with PostgreSQL storage

// where did we configure as a tool in our project ?


// so hangfire must use a db or can it work without a db?
// Hangfire requires a persistent storage to manage background jobs, 
//and it does not work without a database. 
//The storage is used to keep track of job states, schedules, and execution history. 
//In this case, we are using PostgreSQL as the storage for Hangfire, 
//which allows us to store all the necessary information for managing background jobs effectively.

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

// so we tell hangfire to go to ISubscriptionService , right ? if yes then how do it know which method to call ?
// Yes, we specify the service and the method to be called. 
//Hangfire will resolve the service from the DI container and invoke the specified method.
 
// where did dailysubscriptionjob come from ?
// The "DailySubscriptionJob" is a unique identifier for the recurring job. 
//It's used to manage and track the job within Hangfire.
// but we already gave a fun name to the method in ISubscriptionService, so why do we need this ?
// The "DailySubscriptionJob" is a name for the recurring job itself,
// while the method name (RunDailyJobAsync) is the actual code that will be executed when the job runs.

// can we do without giving a name to the job ?
// No, you cannot create a recurring job in Hangfire without giving it a name.
// The name is essential for managing the job, such as updating or deleting it in the future
// but the job is in a fun which already have a fun name, right ? so why do we need to give a name to the job ?
// The method name (RunDailyJobAsync) is the code that will be executed, 
//while the job name (DailySubscriptionJob) is an identifier for the job itself.
// The job name allows you to manage the job independently of the method name, 
//such as updating the schedule or disabling the job without changing the method implementation.


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
