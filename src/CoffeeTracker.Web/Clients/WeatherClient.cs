using System.Net.Http.Json;
using CoffeeTracker.Models;
using CoffeeTracker.Web.Services;

namespace CoffeeTracker.Web.Clients;

public class WeatherClient : IWeatherClient
{
    private readonly AuthenticationService _authService;
    private readonly ILogger<WeatherClient> _logger;

    public WeatherClient(
        AuthenticationService authService,
        ILogger<WeatherClient> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    public async Task<WeatherForecast[]?> GetForecastAsync()
    {
        try
        {
            // Get an authenticated HTTP client
            using var client = await _authService.GetAuthenticatedHttpClientAsync();
            
            // Make the API call
            var response = await client.GetAsync("/weatherforecast");
            
            // Ensure we got a successful response
            response.EnsureSuccessStatusCode();
            
            // Deserialize the response
            var forecasts = await response.Content.ReadFromJsonAsync<WeatherForecast[]>();
            return forecasts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get weather forecast");
            return null;
        }
    }
}
