using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace CoffeeTracker.Migrations.Data;

// This class helps EF Core tools like migrations to create an instance of our DbContext
public class WeatherDbContextFactory : IDesignTimeDbContextFactory<WeatherDbContext>
{
    public WeatherDbContext CreateDbContext(string[] args)
    {
        // First try to get connection info from environment (Aspire may set this)
        string connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__weatherdb");
        
        // If not found in environment (typically when running migrations directly),
        // use a local development connection
        if (string.IsNullOrEmpty(connectionString))
        {
            // This is a pragmatic default for development migrations
            connectionString = "Host=localhost;Database=weatherdb;Username=postgres;Password=postgres";
            Console.WriteLine("Using development connection string for migrations. In production, you should set the ConnectionStrings__weatherdb environment variable.");
        }
        
        var optionsBuilder = new DbContextOptionsBuilder<WeatherDbContext>();
        optionsBuilder.UseNpgsql(connectionString, 
            npgsqlOptions => npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "public"));
        
        return new WeatherDbContext(optionsBuilder.Options);
    }
}
