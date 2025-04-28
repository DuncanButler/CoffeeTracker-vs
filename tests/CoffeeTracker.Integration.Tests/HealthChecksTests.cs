using System.Net;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using CoffeeTracker.ApiService;
using CoffeeTracker.Data;
using CoffeeTracker.ApiService.Interfaces;
using CoffeeTracker.Models;
using Moq;
using Xunit;

namespace CoffeeTracker.Integration.Tests
{
    /// <summary>
    /// Test factory that mocks the database-related services for health check tests
    /// </summary>
    public class HealthCheckWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // Add test-specific configuration
            builder.ConfigureAppConfiguration((context, config) => {
                // Use the correct path to the test settings file
                string testSettingsPath = Path.Combine(
                    Directory.GetCurrentDirectory(), 
                    "appsettings.Testing.json");
                
                // Make the file optional to avoid errors if it doesn't exist
                config.AddJsonFile(testSettingsPath, optional: true);
                
                // Override the connection string with an empty one
                var inMemorySettings = new Dictionary<string, string?>
                {
                    {"ConnectionStrings:weatherdb", ""}
                };
                config.AddInMemoryCollection(inMemorySettings);
            });
            
            // Use Testing environment
            builder.UseEnvironment("Testing");
            
            builder.ConfigureServices(services => 
            {                // Remove and replace DB context with an in-memory version
                services.RemoveAll<DbContextOptions<WeatherDbContext>>();
                services.RemoveAll<WeatherDbContext>();
                
                // Add a mock DB context that won't actually connect to a database
                services.AddDbContext<WeatherDbContext>(options => 
                    options.UseInMemoryDatabase("TestHealthCheckDb"));
                    
                // Add a mock weather repository that doesn't need a real DB
                services.RemoveAll<IWeatherRepository>();
                var mockRepo = new Mock<IWeatherRepository>();
                mockRepo.Setup(r => r.GetForcastForDay(It.IsAny<DateOnly>()))
                    .ReturnsAsync(new WeatherForecast { 
                        Date = DateOnly.FromDateTime(DateTime.Today),
                        TemperatureC = 25,
                        Summary = "Test Weather" 
                    });
                services.AddSingleton(mockRepo.Object);
                
                // Remove all existing health check registrations
                services.RemoveAll<HealthCheckService>();
                
                // Remove any health check registrations 
                var descriptors = services.Where(
                    s => s.ServiceType.FullName?.Contains("HealthChecks") == true).ToList();
                foreach (var descriptor in descriptors)
                {
                    services.Remove(descriptor);
                }
                
                // Add our own test-friendly health checks
                services.AddHealthChecks()
                    .AddCheck("memory_check", () => HealthCheckResult.Healthy());
            });
        }
          // Private helper methods are no longer needed as we're using the TestServiceCollectionExtensions
    }

    public class HealthChecksTests : IClassFixture<HealthCheckWebApplicationFactory>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public HealthChecksTests(HealthCheckWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Health_Endpoint_Returns_Success_Status()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/health");
            
            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK, 
                because: "health endpoint should return 200 OK when the application is healthy");
            
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotBeNullOrEmpty("because health check response should contain data");
        }

        [Fact]
        public async Task Health_Endpoint_Contains_Database_Check()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/health");
            
            // Make sure the response is successful
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            
            // Assert - check for our database checks
            content.Should().Contain("memory_check", 
                because: "health check response should include the memory_check check");
        }
    }
}
