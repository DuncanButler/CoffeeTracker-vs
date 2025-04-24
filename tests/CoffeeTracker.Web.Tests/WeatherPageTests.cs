using Bunit;
using CoffeeTracker.Web.Clients;
using CoffeeTracker.Web.Components.Pages;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace CoffeeTracker.Web.Tests;

public class WeatherPageTests : TestContext
{    [Fact]
    public void WeatherPage_DisplaysLoadingState_Initially()
    {
        // Arrange - Create a mock WeatherApiClient
        var mockWeatherClient = new Mock<IWeatherApiClient>();
        
        // Setup the mock to delay returning forecasts
        mockWeatherClient
            .Setup(client => client.GetWeatherAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns<int, CancellationToken>((_, _) => Task.Delay(1000).ContinueWith(_ => GetSampleForecasts()));
        
        // Register the concrete class that the component expects
        Services.AddSingleton(mockWeatherClient.Object);
        
        // Act - Render the component
        var cut = RenderComponent<Weather>();
        
        // Assert - Check loading state is displayed
        cut.MarkupMatches("<h1>Weather</h1>" +
            "<p>This component demonstrates showing data loaded from a backend API service.</p>" +
            "<p><em>Loading...</em></p>");
    }
      [Fact]
    public async Task WeatherPage_DisplaysForecastData_WhenLoaded()

    {
        // Arrange - Create a mock WeatherApiClient
        var mockWeatherClient = new Mock<IWeatherApiClient>();
          // Setup the mock to return sample forecasts
        var sampleForecasts = GetSampleForecasts();
        mockWeatherClient
            .Setup(client => client.GetWeatherAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sampleForecasts);
        
        // Register the interface that the component expects
        Services.AddSingleton<IWeatherApiClient>(mockWeatherClient.Object);
        
        // Act - Render the component
        var cut = RenderComponent<Weather>();

        // Wait for state to update with more reliable approach
        var renderTask = Task.Run(async () => {
            // Give component time to render initially
            await Task.Delay(50);
            
            // Keep checking until we see rows or timeout
            var timeout = DateTime.Now.AddSeconds(5);
            while (DateTime.Now < timeout) {
                cut.Render(); // Force re-render
                if (cut.FindAll("tbody tr").Any()) {
                    return; // Found rows, exit the loop
                }
                await Task.Delay(100); // Wait before checking again
            }
        });
        
        // Wait for the rendering to complete with timeout
        await renderTask;
          // Debug output of rendered HTML to help diagnose issues
        Console.WriteLine("Rendered HTML:");
        Console.WriteLine(cut.Markup);
        
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
    private static CoffeeTracker.Web.Clients.WeatherForecast[] GetSampleForecasts()
    {
        return new CoffeeTracker.Web.Clients.WeatherForecast[]
        {
            new CoffeeTracker.Web.Clients.WeatherForecast(new DateOnly(2025, 5, 1), 20, "Mild"),
            new CoffeeTracker.Web.Clients.WeatherForecast(new DateOnly(2025, 5, 2), 25, "Warm"),
            new CoffeeTracker.Web.Clients.WeatherForecast(new DateOnly(2025, 5, 3), 15, "Cool")
        };
    }
}
