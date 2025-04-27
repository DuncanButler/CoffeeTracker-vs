using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using CoffeeTracker.ApiService;
using Microsoft.Extensions.DependencyInjection;
using CoffeeTracker.ApiService.Interfaces;
using CoffeeTracker.ApiService.Repositories;
using CoffeeTracker.ApiService.Services;
using CoffeeTracker.Data;
using CoffeeTracker.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.Hosting;
using Xunit;
using FluentAssertions;
using Moq;

namespace CoffeeTracker.Integration.Tests;

public class AuthTestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {        builder.ConfigureServices(services =>
        {
            // Remove ALL database-related services
            ServiceCollectionExtensions.RemoveAll<DbContextOptions>(services);
            ServiceCollectionExtensions.RemoveAll<DbContextOptions<WeatherDbContext>>(services);
            ServiceCollectionExtensions.RemoveAll<WeatherDbContext>(services);
            
            // Replace the real weather repository with our test version
            ServiceCollectionExtensions.RemoveAll<IWeatherRepository>(services);
            ServiceCollectionExtensions.RemoveAll<IWeatherService>(services);
            
            // Add test implementations that don't use a real database
            services.AddScoped<IWeatherRepository, TestWeatherRepository>();
            services.AddScoped<IWeatherService, TestWeatherService>();
        });
    }
}

// Test repository that doesn't use the database
public class TestWeatherRepository : IWeatherRepository
{
    public Task<WeatherForecast?> GetForcastForDay(DateOnly day)
    {
        // Return a test forecast
        return Task.FromResult<WeatherForecast?>(new WeatherForecast
        {
            Date = day,
            TemperatureC = 25,
            Summary = "Test Weather"
        });
    }

    public Task SaveForcastForDay(WeatherForecast dayForecast)
    {
        // No-op implementation for testing
        return Task.CompletedTask;
    }
}

// Test weather service that doesn't use a real repository
public class TestWeatherService : IWeatherService
{
    public Task<WeatherForecast[]> GetWeatherForecastAsync()
    {
        // Return test forecasts
        var forecasts = new[]
        {
            new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Today),
                TemperatureC = 25,
                Summary = "Hot"
            },
            new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
                TemperatureC = 20,
                Summary = "Mild"
            }
        };
        
        return Task.FromResult(forecasts);
    }
}

public class AuthenticationTests : IClassFixture<AuthTestWebApplicationFactory>
{
    private readonly AuthTestWebApplicationFactory _factory;
    
    public AuthenticationTests(AuthTestWebApplicationFactory factory)
    {
        _factory = factory;
    }
    
    [Fact]
    public async Task API_Authentication_Flow_Works_Correctly()
    {
        // Create HTTP client using the test server
        var client = _factory.CreateClient();

        // Step 1: Get a JWT token using the web app's API key
        var tokenRequest = new 
        {
            ApiKey = "web-app-api-key-1234567890",
            ClientId = "web-client"
        };

        var tokenJson = JsonSerializer.Serialize(tokenRequest);
        var tokenContent = new StringContent(tokenJson, Encoding.UTF8, "application/json");
        var tokenResponse = await client.PostAsync("/auth/token", tokenContent);

        // Assert token request is successful
        tokenResponse.IsSuccessStatusCode.Should().BeTrue("because API key authentication should succeed");
        var tokenResult = await tokenResponse.Content.ReadFromJsonAsync<TokenResponse>();
        tokenResult.Should().NotBeNull("because a token should be returned");
        tokenResult!.AccessToken.Should().NotBeNullOrEmpty("because a valid JWT token should be provided");

        // Step 2: Call the weather endpoint with the JWT token
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenResult.AccessToken);
        var weatherResponse = await client.GetAsync("/weatherforecast");

        // Assert authorized access works
        weatherResponse.IsSuccessStatusCode.Should().BeTrue("because access with a valid token should succeed");
        var forecasts = await weatherResponse.Content.ReadFromJsonAsync<WeatherForecast[]>();
        forecasts.Should().NotBeNull("because weather data should be returned");
        
        // Step 3: Try accessing without a token
        var clientWithoutToken = _factory.CreateClient();
        var unauthorizedResponse = await clientWithoutToken.GetAsync("/weatherforecast");

        // Assert unauthorized access is blocked
        unauthorizedResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized, 
            "because access without a token should be unauthorized");
    }
    
    // Classes to deserialize responses
    private class TokenResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public int ExpiresIn { get; set; }
        public string TokenType { get; set; } = string.Empty;
    }
}
