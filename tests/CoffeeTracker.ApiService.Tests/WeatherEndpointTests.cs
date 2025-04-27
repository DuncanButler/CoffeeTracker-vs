using CoffeeTracker.ApiService.Endpoints;
using CoffeeTracker.Data;
using CoffeeTracker.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
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
        
        // Debug the response
        var responseContent = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Response Status: {response.StatusCode}");
        Console.WriteLine($"Response Content: {responseContent}");
        
        response.EnsureSuccessStatusCode();
        
        // Check if we have any content
        if (string.IsNullOrEmpty(responseContent))
        {
            Assert.Fail("Response content is empty");
        }
        
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
    }    
    
    // Use a static database name to ensure shared database across requests in the same test
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

        // Create a mock weather service that returns predefined test data
        var mockWeatherService = new Mock<CoffeeTracker.ApiService.Services.IWeatherService>();
        var testForecasts = new WeatherForecast[]
        {
            new WeatherForecast { Date = DateOnly.FromDateTime(DateTime.Today), TemperatureC = 20, Summary = "Mild" },
            new WeatherForecast { Date = DateOnly.FromDateTime(DateTime.Today.AddDays(1)), TemperatureC = 25, Summary = "Warm" },
            new WeatherForecast { Date = DateOnly.FromDateTime(DateTime.Today.AddDays(2)), TemperatureC = 15, Summary = "Cool" },
            new WeatherForecast { Date = DateOnly.FromDateTime(DateTime.Today.AddDays(3)), TemperatureC = 10, Summary = "Bracing" },
            new WeatherForecast { Date = DateOnly.FromDateTime(DateTime.Today.AddDays(4)), TemperatureC = 30, Summary = "Hot" }
        };
        
        mockWeatherService
            .Setup(service => service.GetWeatherForecastAsync())
            .ReturnsAsync(testForecasts);
            
        // Register the mock service instead of the real one
        builder.Services.AddSingleton<CoffeeTracker.ApiService.Services.IWeatherService>(mockWeatherService.Object);

        // Set up authentication for testing
        builder.Services.AddAuthentication("Test")
            .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, TestAuthHandler>("Test", 
                options => { });

        builder.Services.AddAuthorization(options => {
            options.DefaultPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddAuthenticationSchemes("Test")
                .Build();
        });

        // Build the WebApplication
        var app = builder.Build();

        // Configure the HTTP request pipeline
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();

        // Map endpoints using your extension method
        app.MapWeatherEndpoints();

        // Start the server
        await app.StartAsync();

        // Return the host
        return app;
    }
}

// Custom authentication handler for testing that always authenticates
public class TestAuthHandler : Microsoft.AspNetCore.Authentication.AuthenticationHandler<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions>
{
    public TestAuthHandler(
        Microsoft.Extensions.Options.IOptionsMonitor<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions> options,
        Microsoft.Extensions.Logging.ILoggerFactory logger,
        System.Text.Encodings.Web.UrlEncoder encoder,
        Microsoft.AspNetCore.Authentication.ISystemClock clock) 
        : base(options, logger, encoder, clock)
    {
    }

    protected override Task<Microsoft.AspNetCore.Authentication.AuthenticateResult> HandleAuthenticateAsync()
    {
        // Create a test identity that is always authenticated
        var claims = new[] { 
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, "TestUser"),
            new System.Security.Claims.Claim("client_id", "test-client")
        };
        var identity = new System.Security.Claims.ClaimsIdentity(claims, "Test");
        var principal = new System.Security.Claims.ClaimsPrincipal(identity);
        var ticket = new Microsoft.AspNetCore.Authentication.AuthenticationTicket(principal, "Test");
        
        // Return success with the ticket
        return Task.FromResult(Microsoft.AspNetCore.Authentication.AuthenticateResult.Success(ticket));
    }
}
