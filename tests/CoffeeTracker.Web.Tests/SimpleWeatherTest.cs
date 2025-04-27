using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using CoffeeTracker.Web.Services;
using CoffeeTracker.Web.Clients;
using CoffeeTracker.Web.Components.Pages;
using Xunit;

namespace CoffeeTracker.Web.Tests
{
    public class SimpleWeatherTest : Bunit.TestContext
    {
        [Fact]
        public void CanRenderWeatherComponent_WithLoadingState()
        {
            // Setup
            // 1. Create mock dependencies for WeatherClient
            var mockHttpClientFactory = new Mock<IHttpClientFactory>();
            var mockLogger = new Mock<ILogger<WeatherClient>>();
            var mockLoggerAuth = new Mock<ILogger<AuthenticationService>>();
            var mockConfig = new Mock<IConfiguration>();
            
            // 2. Create real AuthenticationService with mock dependencies
            var authService = new AuthenticationService(
                mockHttpClientFactory.Object,
                mockLoggerAuth.Object,
                mockConfig.Object);
            
            // 3. Create real WeatherClient with real AuthenticationService and mock logger
            var weatherClient = new WeatherClient(authService, mockLogger.Object);
            
            // 4. Register the service in the test context
            Services.AddSingleton<IWeatherClient>(weatherClient);
            
            // Act
            var component = RenderComponent<Weather>();
            
            // Assert - Check that the component renders with loading state
            Assert.Contains("Loading...", component.Markup);
        }
    }
}
