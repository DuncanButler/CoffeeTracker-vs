using CoffeeTracker.Models;

namespace CoffeeTracker.Web.Clients;

public interface IWeatherApiClient
{
    Task<WeatherForecast[]> GetWeatherAsync(int maxItems = 10, CancellationToken cancellationToken = default);
}
public class WeatherApiClient : IWeatherApiClient
{
    private readonly ILogger<WeatherApiClient>? _logger;
    private readonly HttpClient httpClient;

    // Constructor with logger injection for better diagnostics
    public WeatherApiClient(HttpClient httpClient, ILogger<WeatherApiClient>? logger = null)
    {
        _logger = logger;
        this.httpClient = httpClient;        
    }
    
    public async Task<WeatherForecast[]> GetWeatherAsync(int maxItems = 10, CancellationToken cancellationToken = default)
    {
        List<WeatherForecast>? forecasts = null;
        
        try
        {
            await foreach (var forecast in httpClient.GetFromJsonAsAsyncEnumerable<WeatherForecast>("/weatherforecast", cancellationToken))
            {
                if (forecasts?.Count >= maxItems)
                {
                    break;
                }
                if (forecast is not null)
                {
                    forecasts ??= [];
                    forecasts.Add(forecast);
                }
            }
            
            return forecasts?.ToArray() ?? [];
        }
        catch (HttpRequestException ex)
        {
            _logger?.LogError(ex, "Network error occurred while fetching weather forecasts: {Message}", ex.Message);
            // Return empty array to avoid null reference exceptions in UI
            return Array.Empty<WeatherForecast>();
        }
        catch (System.Text.Json.JsonException ex)  // Add System.Text.Json namespace at the top to use this
        {
            _logger?.LogError(ex, "Error deserializing weather forecast data: {Message}", ex.Message);
            return Array.Empty<WeatherForecast>();
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            _logger?.LogInformation("Weather forecast request was canceled");
            throw; // Rethrow cancellation - this is expected behavior
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Unexpected error occurred while fetching weather forecasts: {Message}", ex.Message);
            return Array.Empty<WeatherForecast>();
        }
    }
}

// Removed duplicate WeatherForecast definition
// Now using CoffeeTracker.Models.WeatherForecast instead
