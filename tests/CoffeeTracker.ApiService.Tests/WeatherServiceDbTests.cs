using CoffeeTracker.ApiService.Interfaces;
using CoffeeTracker.ApiService.Repositories;
using CoffeeTracker.ApiService.Services;
using CoffeeTracker.Data;
using CoffeeTracker.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace CoffeeTracker.ApiService.Tests;

public class WeatherServiceDbTests
{
    [Fact]
    public async Task GetWeatherForecastAsync_WithEmptyDb_Returns5Forecasts()
    {
        // Arrange - Set up in-memory database
        var dbContextOptions = CreateNewInMemoryDatabase();
        
        // Create fresh context for test setup
        using var context = new WeatherDbContext(dbContextOptions);
        
        // Create the repository and service
        var repository = new WeatherRepository(context);
        IWeatherService service = new WeatherService(repository);
        
        // Act
        var forecasts = await service.GetWeatherForecastAsync();
        
        // Assert
        Assert.Equal(5, forecasts.Length);
        
        // Verify data was saved to database
        Assert.Equal(5, await context.Forecasts.CountAsync());
    }
    
    [Fact]
    public async Task GetWeatherForecastAsync_WithExistingData_UsesExistingForecasts()
    {
        // Arrange - Set up in-memory database with seed data
        var dbContextOptions = CreateNewInMemoryDatabase();
        
        // Create context and seed data
        var today = DateOnly.FromDateTime(DateTime.Now);
        using (var seedContext = new WeatherDbContext(dbContextOptions))
        {
            seedContext.Forecasts.AddRange(new List<WeatherForecast> 
            {
                new WeatherForecast(today.AddDays(2), 25, "Sunny"), // Existing forecast for day 2
                new WeatherForecast(today.AddDays(4), 30, "Hot")    // Existing forecast for day 4
            });
            await seedContext.SaveChangesAsync();
        }
        
        // Create fresh context for test
        using var context = new WeatherDbContext(dbContextOptions);
        
        // Create repository and service
        var repository = new WeatherRepository(context);
        IWeatherService service = new WeatherService(repository);
        
        // Act
        var forecasts = await service.GetWeatherForecastAsync();
        
        // Assert
        Assert.Equal(5, forecasts.Length);
        Assert.Equal(5, await context.Forecasts.CountAsync()); // Should now have 5 forecasts in DB
        
        // Verify the service returns forecasts for 5 consecutive days
        var existingSunnyForecast = forecasts.SingleOrDefault(f => f.Date == today.AddDays(2));
        Assert.NotNull(existingSunnyForecast);
        Assert.Equal("Sunny", existingSunnyForecast.Summary);
        Assert.Equal(25, existingSunnyForecast.TemperatureC);
        
        var existingHotForecast = forecasts.SingleOrDefault(f => f.Date == today.AddDays(4));
        Assert.NotNull(existingHotForecast);
        Assert.Equal("Hot", existingHotForecast.Summary);
        Assert.Equal(30, existingHotForecast.TemperatureC);
    }
    
    [Fact]
    public async Task GetWeatherForecastAsync_GeneratesProperForecasts()
    {
        // Arrange - Set up in-memory database
        var dbContextOptions = CreateNewInMemoryDatabase();
        using var context = new WeatherDbContext(dbContextOptions);
        
        // Create repository and service
        var repository = new WeatherRepository(context);
        IWeatherService service = new WeatherService(repository);
        
        // Act
        var forecasts = await service.GetWeatherForecastAsync();
        
        // Assert
        Assert.Equal(5, forecasts.Length);
        
        // Verify forecasts are for the next 5 days
        var today = DateOnly.FromDateTime(DateTime.Now);
        for (int i = 0; i < forecasts.Length; i++)
        {
            var expectedDate = today.AddDays(i + 1);
            Assert.Equal(expectedDate, forecasts[i].Date);
        }
        
        // Verify forecast properties are within expected ranges
        string[] possibleSummaries = ["Freezing", "Bracing", "Chilly", "Cool", "Mild", 
                                     "Warm", "Balmy", "Hot", "Sweltering", "Scorching"];
        
        foreach (var forecast in forecasts)
        {
            Assert.Contains(forecast.Summary, possibleSummaries);
            Assert.InRange(forecast.TemperatureC, -20, 55);
        }
    }
    
    /// <summary>
    /// Helper method to create a unique in-memory database for each test
    /// </summary>
    private static DbContextOptions<WeatherDbContext> CreateNewInMemoryDatabase()
    {
        return new DbContextOptionsBuilder<WeatherDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Unique DB name per test
            .Options;
    }
}
