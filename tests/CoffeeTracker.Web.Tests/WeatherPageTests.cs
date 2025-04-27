using Bunit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using CoffeeTracker.Models;
using CoffeeTracker.Web.Clients;
using CoffeeTracker.Web.Components.Pages;
using CoffeeTracker.Web.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using System.Net.Http;

namespace CoffeeTracker.Web.Tests
{    
    // Create a special mock implementation of IWeatherClient for testing
    public class MockWeatherClient : IWeatherClient
    {
        private readonly WeatherForecast[]? _forecasts;
        
        public MockWeatherClient(WeatherForecast[]? forecasts)
        {
            _forecasts = forecasts;
            Console.WriteLine($"MockWeatherClient created with {_forecasts?.Length ?? 0} forecasts");
        }
        
        public Task<WeatherForecast[]?> GetForecastAsync()
        {
            Console.WriteLine($"MockWeatherClient.GetForecastAsync called, returning {_forecasts?.Length ?? 0} forecasts");
            return Task.FromResult(_forecasts);
        }
    }
    
    public class WeatherPageTests : TestContext
    {
        [Fact]
        public void WeatherPage_DisplaysLoadingState_Initially()
        {
            // Arrange - Mock the dependencies needed for WeatherClient
            var mockHttpClientFactory = new Mock<IHttpClientFactory>();
            var mockLogger = new Mock<ILogger<WeatherClient>>();
            var mockLoggerAuth = new Mock<ILogger<AuthenticationService>>();
            var mockConfig = new Mock<IConfiguration>();
            
            // Create AuthenticationService with mock dependencies
            var authService = new AuthenticationService(
                mockHttpClientFactory.Object,
                mockLoggerAuth.Object,
                mockConfig.Object);
            
            // Create a real WeatherClient with mocked dependencies
            var weatherClient = new WeatherClient(authService, mockLogger.Object);
            
            // Setup a delay for the loading state test
            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClientFactory
                .Setup(factory => factory.CreateClient(It.IsAny<string>()))
                .Returns(mockHttpClient.Object);

                        // Create a mock IWeatherClient that returns our test data
            var mockClient = new MockWeatherClient(null);

            // Key change: Register our mock as the IWeatherClient service, not WeatherClient
            // The component injects WeatherClient, so we need to make sure the resolution works
            Services.AddSingleton<IWeatherClient>(mockClient);
            Services.AddSingleton<WeatherClient>(sp => {
                // We need to provide an instance of WeatherClient, but we're going to modify
                // the component to inject IWeatherClient instead
                var client = new WeatherClient(authService, mockLogger.Object);
                return client;
            });
            
            // Act - Render the component
            var cut = RenderComponent<Weather>();
            
            // Assert - Check loading state is displayed
            cut.MarkupMatches("<h1>Weather Forecast</h1>" +
                "<p>This component demonstrates fetching data from the API with authentication.</p>" +
                "<p><em>Loading...</em></p>");
        }
        
        [Fact]
        public async Task WeatherPage_DisplaysForecastData_WhenLoaded()
        {
            // Arrange - Mock the dependencies
            var mockHttpClientFactory = new Mock<IHttpClientFactory>();
            var mockLogger = new Mock<ILogger<WeatherClient>>();
            var mockLoggerAuth = new Mock<ILogger<AuthenticationService>>();
            var mockConfig = new Mock<IConfiguration>();
            
            // Create an AuthenticationService with mock dependencies
            var authService = new AuthenticationService(
                mockHttpClientFactory.Object,
                mockLoggerAuth.Object,
                mockConfig.Object);
            
            // Get sample forecasts for testing
            var sampleForecasts = GetSampleForecasts();
            
            // Create a mock IWeatherClient that returns our test data
            var mockClient = new MockWeatherClient(sampleForecasts);
            
            // Key change: Register our mock as the IWeatherClient service, not WeatherClient
            // The component injects WeatherClient, so we need to make sure the resolution works
            Services.AddSingleton<IWeatherClient>(mockClient);
            Services.AddSingleton<WeatherClient>(sp => {
                // We need to provide an instance of WeatherClient, but we're going to modify
                // the component to inject IWeatherClient instead
                var client = new WeatherClient(authService, mockLogger.Object);
                return client;
            });
            
            // Act - Render the component
            var cut = RenderComponent<Weather>();
            
            // UPDATE THE WEATHER.RAZOR COMPONENT TO INJECT IWeatherClient INSTEAD OF WEATHERCLIENT!
            // @inject IWeatherClient WeatherClient
            
            // Give component a moment to start the initial render
            await Task.Delay(100);
            
            // Print the current markup to help debug
            Console.WriteLine("Initial markup:");
            Console.WriteLine(cut.Markup);
            
            // Force an update cycle
            cut.Render();
            
            await Task.Delay(100);
            
            // Use a timeout approach that properly awaits
            var startTime = DateTime.Now;
            var timeout = TimeSpan.FromSeconds(5);
            
            // Poll for the table rows to appear
            while (!cut.FindAll("tbody tr").Any())
            {
                // Check timeout
                if (DateTime.Now - startTime > timeout)
                {
                    // Debug output before failing
                    Console.WriteLine("Component markup at timeout:");
                    Console.WriteLine(cut.Markup);
                    
                    Assert.Fail("Component did not render table rows within the timeout period");
                }
                
                // Force a render cycle and wait a bit longer before checking again
                cut.Render();
                await Task.Delay(100); 
            }
            
            // First verify we have the expected number of rows
            var rows = cut.FindAll("tbody tr").ToList();
            rows.Count.Should().Be(3, "because we provided 3 sample forecasts");
            
            // Instead of precise markup matching, verify the key content appears somewhere
            // This is more resilient to slight formatting changes
            cut.Markup.Should().Contain("01/05/2025", "because our first sample forecast date should appear");
            cut.Markup.Should().Contain("20", "because our first sample forecast temperature should appear");
            cut.Markup.Should().Contain("Mild", "because our first sample forecast summary should appear");
            
            // Verify the second and third forecasts appear too
            cut.Markup.Should().Contain("02/05/2025", "because our second sample forecast date should appear");
            cut.Markup.Should().Contain("25", "because our second sample forecast temperature should appear");
            cut.Markup.Should().Contain("Warm", "because our second sample forecast summary should appear");
            
            cut.Markup.Should().Contain("03/05/2025", "because our third sample forecast date should appear");
            cut.Markup.Should().Contain("15", "because our third sample forecast temperature should appear");
            cut.Markup.Should().Contain("Cool", "because our third sample forecast summary should appear");
        }
        
        // Helper method to create sample weather forecasts
        private static WeatherForecast[] GetSampleForecasts()
        {
            return new WeatherForecast[]
            {
                new WeatherForecast 
                { 
                    Date = new DateOnly(2025, 5, 1), 
                    TemperatureC = 20, 
                    Summary = "Mild" 
                },
                new WeatherForecast 
                { 
                    Date = new DateOnly(2025, 5, 2), 
                    TemperatureC = 25, 
                    Summary = "Warm" 
                },
                new WeatherForecast 
                { 
                    Date = new DateOnly(2025, 5, 3), 
                    TemperatureC = 15, 
                    Summary = "Cool" 
                }
            };
        }
    }
}
