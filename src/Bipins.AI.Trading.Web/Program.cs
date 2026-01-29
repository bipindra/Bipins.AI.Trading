using Bipins.AI.Trading.Application;
using Bipins.AI.Trading.Application.Options;
using Bipins.AI.Trading.Infrastructure;
using Bipins.AI.Trading.Infrastructure.Persistence;
using Bipins.AI.Trading.Web;
using MassTransit;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;
using System.Data.Common;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

// Add services to the container
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddHttpClient(); // Required for Bipins.AI vector store providers

// Add Entity Framework
builder.Services.AddDbContext<TradingDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
        ?? builder.Configuration["Storage:ConnectionString"] 
        ?? "Data Source=trading.db";
    
    options.UseSqlite(connectionString);
    options.EnableSensitiveDataLogging(builder.Environment.IsDevelopment());
});

// Add Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 4;
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<TradingDbContext>();

// Configure options
builder.Services.Configure<BrokerOptions>(builder.Configuration.GetSection("Broker"));
builder.Services.Configure<TradingOptions>(builder.Configuration.GetSection("Trading"));
builder.Services.Configure<RiskOptions>(builder.Configuration.GetSection("Risk"));
builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection("Storage"));
builder.Services.Configure<CacheOptions>(builder.Configuration.GetSection("Cache"));
builder.Services.Configure<VectorDbOptions>(builder.Configuration.GetSection("VectorDb"));
builder.Services.Configure<LLMOptions>(builder.Configuration.GetSection("LLM"));

// Add MassTransit
builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();
    
    // Add consumers
    x.AddConsumer<Bipins.AI.Trading.Application.Consumers.CandleClosedConsumer>();
    x.AddConsumer<Bipins.AI.Trading.Infrastructure.Consumers.FeaturesComputedConsumer>();
    x.AddConsumer<Bipins.AI.Trading.Application.Consumers.IndicatorsCalculatedConsumer>();
    x.AddConsumer<Bipins.AI.Trading.Application.Consumers.TradeProposedConsumer>();
    x.AddConsumer<Bipins.AI.Trading.Application.Consumers.TradeApprovedConsumer>();
    x.AddConsumer<Bipins.AI.Trading.Application.Consumers.OrderFilledConsumer>();
    
    x.UsingInMemory((context, cfg) =>
    {
        cfg.ConfigureEndpoints(context);
    });
});

// Add health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<TradingDbContext>("database")
    .AddCheck("qdrant", () =>
    {
        var qdrantEndpoint = builder.Configuration["VectorDb:Qdrant:Endpoint"] ?? "http://localhost:6333";
        try
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(2);
            var response = client.GetAsync($"{qdrantEndpoint}/health").GetAwaiter().GetResult();
            return response.IsSuccessStatusCode 
                ? Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Qdrant is available")
                : Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy("Qdrant health check failed");
        }
        catch (Exception ex)
        {
            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy($"Qdrant is unavailable: {ex.Message}");
        }
    }, tags: new[] { "qdrant", "vector" });

// Register application services
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddInfrastructureServices(builder.Configuration);

// Register hosted services
builder.Services.AddHostedService<MarketDataIngestionHostedService>();
builder.Services.AddHostedService<FeatureComputeHostedService>();
builder.Services.AddHostedService<TradingDecisionHostedService>();
builder.Services.AddHostedService<PortfolioReconciliationHostedService>();

var app = builder.Build();

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<TradingDbContext>();
    dbContext.Database.EnsureCreated();
    
    // Add FinalAction column if it doesn't exist (migration for existing databases)
    try
    {
        var connection = dbContext.Database.GetDbConnection();
        await connection.OpenAsync();
        using var command = connection.CreateCommand();
        
        // Check if FinalAction column exists
        command.CommandText = "PRAGMA table_info(Strategies);";
        using var reader = await command.ExecuteReaderAsync();
        var columns = new List<string>();
        while (await reader.ReadAsync())
        {
            columns.Add(reader.GetString(1)); // Column name is at index 1
        }
        await reader.CloseAsync();
        
        // Add FinalAction column if it doesn't exist
        if (!columns.Contains("FinalAction", StringComparer.OrdinalIgnoreCase))
        {
            command.CommandText = "ALTER TABLE Strategies ADD COLUMN FinalAction TEXT;";
            await command.ExecuteNonQueryAsync();
        }
        
        await connection.CloseAsync();
    }
    catch (Exception ex)
    {
        // Log but don't fail startup - column might already exist or table might not exist yet
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogWarning(ex, "Could not add FinalAction column. It may already exist or the table may not exist yet.");
    }
    
    // Seed identity user
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    if (!userManager.Users.Any())
    {
        var adminUser = new ApplicationUser { UserName = "admin", Email = "admin@trading.local" };
        await userManager.CreateAsync(adminUser, "admin");
    }
}

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

// Map health check endpoint
app.MapHealthChecks("/health");

app.Run();
