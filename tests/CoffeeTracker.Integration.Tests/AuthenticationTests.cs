using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using CoffeeTracker.ApiService;
using CoffeeTracker.ApiService.Interfaces;
using CoffeeTracker.ApiService.Repositories;
using CoffeeTracker.ApiService.Services;
using CoffeeTracker.Data;
using CoffeeTracker.Models;
using Xunit;
using FluentAssertions;
using Moq;

namespace CoffeeTracker.Integration.Tests
{
    /// <summary>
    /// Test factory that configures a test environment for authentication and authorization tests.
    /// This factory sets up JWT authentication with test keys and replaces database access with in-memory implementations.
    /// </summary>
    public class AuthTestWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // Use Testing environment
            builder.UseEnvironment("Testing");
            
            // Configure services
            builder.ConfigureAppConfiguration((context, config) => 
            {
                // JWT configuration - NOTE: These values are only used for testing 
                // and should never be used in a production environment
                var inMemorySettings = new Dictionary<string, string?>
                {
                    {"ConnectionStrings:weatherdb", ""},
                    {"Jwt:Issuer", "https://coffeetracker-test.com"},
                    {"Jwt:Audience", "https://coffeetracker-test.com"},
                    // Test-only JWT key - never use in production
                    {"Jwt:Key", "ThisIsAVeryLongSecretKeyForTestingPurposesOnly12345"}, 
                    {"Jwt:ExpiryInMinutes", "60"}
                };
                config.AddInMemoryCollection(inMemorySettings);
            });
            
            builder.ConfigureTestServices(services => 
            {
                // Remove the DbContext registration
                services.RemoveAll<DbContextOptions>();
                services.RemoveAll<DbContextOptions<WeatherDbContext>>();
                services.RemoveAll<WeatherDbContext>();
                
                // Add an in-memory database
                services.AddDbContext<WeatherDbContext>(options => 
                    options.UseInMemoryDatabase("TestDb_" + Guid.NewGuid().ToString()));
                
                // Replace the weather service with our test implementation
                services.RemoveAll<IWeatherRepository>();
                services.RemoveAll<IWeatherService>();
                services.AddScoped<IWeatherRepository, TestWeatherRepository>();
                services.AddScoped<IWeatherService, TestWeatherService>();
                
                // Ensure auth service is registered with our test configuration
                services.RemoveAll<IAuthService>();
                services.AddScoped<IAuthService, AuthService>();
            });
        }
    }

    /// <summary>
    /// Test repository implementation that doesn't use a real database.
    /// This implementation provides predictable responses for testing.
    /// </summary>
    public class TestWeatherRepository : IWeatherRepository
    {
        /// <summary>
        /// Returns a test forecast for any given day.
        /// </summary>
        public Task<WeatherForecast?> GetForecastForDay(DateOnly day)
        {
            return Task.FromResult<WeatherForecast?>(new WeatherForecast
            {
                Date = day,
                TemperatureC = 25,
                Summary = "Test Weather"
            });
        }

        /// <summary>
        /// No-op implementation for testing.
        /// </summary>
        public Task SaveForecastForDay(WeatherForecast dayForecast)
        {
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Test weather service implementation that provides consistent test data.
    /// </summary>
    public class TestWeatherService : IWeatherService
    {
        public Task<WeatherForecast[]> GetWeatherForecastAsync()
        {
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
}
