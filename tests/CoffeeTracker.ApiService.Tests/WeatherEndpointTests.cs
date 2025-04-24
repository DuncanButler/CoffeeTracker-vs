using CoffeeTracker.ApiService.Endpoints;
using CoffeeTracker.Data;
using CoffeeTracker.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net.Http.Json;

namespace CoffeeTracker.ApiService.Tests;

public class WeatherEndpointTests
{
    [Fact]
    public async Task GetWeatherForecast_ReturnsForecasts()
    {
        // Arrange
        using var host = await CreateTestHost();
        using var client = host.GetTestClient();

        // Act
        var response = await client.GetAsync("/weatherforecast");
        response.EnsureSuccessStatusCode();
        
        var forecasts = await response.Content.ReadFromJsonAsync<WeatherForecast[]>();

        // Assert
        Assert.NotNull(forecasts);
        Assert.Equal(5, forecasts.Length);
        
        // Validate forecast properties
        foreach (var forecast in forecasts)
        {
            Assert.NotEqual(default, forecast.Date);
            Assert.InRange(forecast.TemperatureC, -20, 55);
            
            string[] possibleSummaries = ["Freezing", "Bracing", "Chilly", "Cool", "Mild", 
                                         "Warm", "Balmy", "Hot", "Sweltering", "Scorching"];
            Assert.Contains(forecast.Summary, possibleSummaries);
        }
    }

    [Fact]
    public async Task GetWeatherForecast_PersistsForecasts()
    {
        // Let's use a direct approach with the in-memory database
        // Create the DbContext options
        var dbContextOptions = new DbContextOptionsBuilder<WeatherDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_" + Guid.NewGuid().ToString())
            .Options;
        
        // Create services for the repository and weather service
        var services = new ServiceCollection();
        services.AddScoped<CoffeeTracker.ApiService.Interfaces.IWeatherRepository, 
            CoffeeTracker.ApiService.Repositories.WeatherRepository>();
        services.AddScoped<CoffeeTracker.ApiService.Services.IWeatherService, 
            CoffeeTracker.ApiService.Services.WeatherService>();
        services.AddSingleton(dbContextOptions);
        services.AddScoped<WeatherDbContext>();
        
        var serviceProvider = services.BuildServiceProvider();
        
        // First call: Generate and save forecasts
        WeatherForecast[] forecasts1;
        using (var scope = serviceProvider.CreateScope())
        {
            var weatherService = scope.ServiceProvider.GetRequiredService<CoffeeTracker.ApiService.Services.IWeatherService>();
            forecasts1 = await weatherService.GetWeatherForecastAsync();
            
            // Verify we got 5 forecasts
            Assert.Equal(5, forecasts1.Length);
            
            // Check the database directly to verify forecasts were saved
            var dbContext = scope.ServiceProvider.GetRequiredService<WeatherDbContext>();
            var savedCount = await dbContext.Forecasts.CountAsync();
            Assert.Equal(5, savedCount);
        }
        
        // Second call: Should use existing forecasts instead of generating new ones
        WeatherForecast[] forecasts2;
        using (var scope = serviceProvider.CreateScope()) 
        {
            var weatherService = scope.ServiceProvider.GetRequiredService<CoffeeTracker.ApiService.Services.IWeatherService>();
            forecasts2 = await weatherService.GetWeatherForecastAsync();
            
            // Verify we still have 5 forecasts
            Assert.Equal(5, forecasts2.Length);
            
            // Verify the database still has 5 forecasts (not 10)
            var dbContext = scope.ServiceProvider.GetRequiredService<WeatherDbContext>();
            var savedCount = await dbContext.Forecasts.CountAsync();
            Assert.Equal(5, savedCount);
        }
        
        // Order forecasts by date to ensure consistent comparison
        var orderedForecasts1 = forecasts1.OrderBy(f => f.Date).ToArray();
        var orderedForecasts2 = forecasts2.OrderBy(f => f.Date).ToArray();
        
        // Compare forecasts from first and second calls
        for (int i = 0; i < 5; i++)
        {
            Assert.Equal(orderedForecasts1[i].Date, orderedForecasts2[i].Date);
            Assert.Equal(orderedForecasts1[i].TemperatureC, orderedForecasts2[i].TemperatureC);
            Assert.Equal(orderedForecasts1[i].Summary, orderedForecasts2[i].Summary);
        }
    }    // Use a static database name to ensure shared database across requests in the same test
    
    private static readonly string _databaseName = "TestWeatherDb_" + Guid.NewGuid().ToString();
    
    private async Task<IHost> CreateTestHost()
    {
        // Create a test WebApplicationFactory that correctly supports minimal APIs
        var builder = WebApplication.CreateBuilder();

        // Use TestServer
        builder.WebHost.UseTestServer();

        // Add services to the container - use the SAME database name across all calls
        // This ensures the database is shared between requests in the same test
        builder.Services.AddDbContext<WeatherDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: _databaseName));

        // Add services and repositories
        builder.Services.AddScoped<CoffeeTracker.ApiService.Interfaces.IWeatherRepository,
            CoffeeTracker.ApiService.Repositories.WeatherRepository>();

        builder.Services.AddScoped<CoffeeTracker.ApiService.Services.IWeatherService,
            CoffeeTracker.ApiService.Services.WeatherService>();

        // Build the WebApplication
        var app = builder.Build();

        // Configure the HTTP request pipeline
        app.UseRouting();

        // Map endpoints using your extension method
        app.MapWeatherEndpoints();

        // Start the server
        await app.StartAsync();

        // Return the host
        return app;
    }
}
