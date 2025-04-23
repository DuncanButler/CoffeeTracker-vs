using CoffeeTracker.Migrations.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations
builder.AddServiceDefaults();

// Add PostgreSQL DbContext using Aspire service discovery
builder.AddNpgsqlDbContext<WeatherDbContext>("weatherdb", 
    configureDbContextOptions: options =>
    {
        options.UseNpgsql(npgsqlBuilder =>
        {
            npgsqlBuilder.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorCodesToAdd: null);
            npgsqlBuilder.CommandTimeout(30);
            npgsqlBuilder.MigrationsHistoryTable("__EFMigrationsHistory", "public");
        });
    });

var app = builder.Build();

// Run migrations and exit
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<WeatherDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    logger.LogInformation("Starting database migration process");
    
    try
    {
        // Apply migrations
        logger.LogInformation("Applying migrations to database...");
        await dbContext.Database.MigrateAsync();
        logger.LogInformation("Migrations successfully applied. Exiting migration service.");
        
        // Additional seeding could happen here if needed
        // await SeedDatabase(dbContext, logger);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while applying migrations");
        // In a production scenario, you might want to rethrow to fail the startup
        throw;
    }
}

// No need to keep the web server running - service has completed its job
await app.StopAsync();
return 0;
