using CoffeeTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace CoffeeTracker.ApiService.Data;

public class WeatherDbContext : DbContext
{
    public WeatherDbContext(DbContextOptions<WeatherDbContext> options) : base(options)
    {
    }

    public DbSet<WeatherForecast> Forecasts { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure the WeatherForecast entity
        modelBuilder.Entity<WeatherForecast>()
            .HasKey(w => w.Date); // Use Date as the primary key
            
        modelBuilder.Entity<WeatherForecast>()
            .Property(w => w.Summary)
            .HasMaxLength(255);
    }
}
