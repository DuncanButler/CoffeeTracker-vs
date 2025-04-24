using System.Net;
using WireMock.Server;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using FluentAssertions;
using CoffeeTracker.Models;
using CoffeeTracker.Web.Clients;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CoffeeTracker.Integration.Tests;

public class ResiliencePatternsTests : IDisposable
{
    private readonly WireMockServer _mockServer;
    private readonly HttpClient _httpClient;
    private readonly IWeatherApiClient _weatherApiClient;
    private readonly ILogger<WeatherApiClient> _logger;

    public ResiliencePatternsTests()
    {
        // Setup mock server
        _mockServer = WireMockServer.Start();
        
        // Setup HttpClient that points to the mock server
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(_mockServer.Urls[0])
        };

        // Create a test logger that will capture log messages
        _logger = new NullLogger<WeatherApiClient>();
        
        // Create the client with our test HttpClient
        _weatherApiClient = new WeatherApiClient(_httpClient, _logger);
    }

    [Fact]
    public async Task WeatherApiClient_ReturnsEmptyArray_WhenApiUnavailable()
    {
        // Arrange - Setup the mock to simulate a server error
        _mockServer
            .Given(Request.Create().WithPath("/weatherforecast").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(HttpStatusCode.ServiceUnavailable));

        // Act
        var result = await _weatherApiClient.GetWeatherAsync();

        // Assert
        result.Should().NotBeNull("because the client should never return null");
        result.Should().BeEmpty("because the API returned an error");
    }

    [Fact]
    public async Task WeatherApiClient_ReturnsEmptyArray_WhenDeserializationFails()
    {
        // Arrange - Setup the mock to return invalid JSON
        _mockServer
            .Given(Request.Create().WithPath("/weatherforecast").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithBody("{ invalid json }"));

        // Act
        var result = await _weatherApiClient.GetWeatherAsync();

        // Assert
        result.Should().NotBeNull("because the client should never return null");
        result.Should().BeEmpty("because the response couldn't be deserialized");
    }

    [Fact]
    public async Task WeatherApiClient_ReturnsForecasts_WhenApiSucceeds()
    {
        // Arrange - Setup the mock to return a valid response
        var validJson = @"[
            {""date"":""2025-05-01"",""temperatureC"":20,""summary"":""Mild""},
            {""date"":""2025-05-02"",""temperatureC"":25,""summary"":""Warm""}
        ]";

        _mockServer
            .Given(Request.Create().WithPath("/weatherforecast").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody(validJson));

        // Act
        var result = await _weatherApiClient.GetWeatherAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result[0].TemperatureC.Should().Be(20);
        result[0].Summary.Should().Be("Mild");
    }

    [Fact]
    public async Task WeatherApiClient_RespectsCancellationToken()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        
        // Setup the mock to delay the response
        _mockServer
            .Given(Request.Create().WithPath("/weatherforecast").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithDelay(TimeSpan.FromSeconds(5))); // Long delay        // Act & Assert
        cts.CancelAfter(100); // Cancel after 100ms
        
        // Since TaskCanceledException derives from OperationCanceledException,
        // we need to handle both types
        await FluentActions.Invoking(async () => 
        {
            await _weatherApiClient.GetWeatherAsync(cancellationToken: cts.Token);
        }).Should().ThrowAsync<OperationCanceledException>();
    }

    public void Dispose()
    {
        _mockServer.Stop();
        _httpClient.Dispose();
    }
}
