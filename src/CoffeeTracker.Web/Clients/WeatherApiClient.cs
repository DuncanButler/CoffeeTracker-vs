using CoffeeTracker.Models;

namespace CoffeeTracker.Web.Clients;

public interface IWeatherApiClient
{
    Task<WeatherForecast[]> GetWeatherAsync(int maxItems = 10, CancellationToken cancellationToken = default);
}
public class WeatherApiClient(HttpClient httpClient): IWeatherApiClient
{
    public async Task<WeatherForecast[]> GetWeatherAsync(int maxItems = 10, CancellationToken cancellationToken = default)
    {
        List<WeatherForecast>? forecasts = null;

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
}

// Removed duplicate WeatherForecast definition
// Now using CoffeeTracker.Models.WeatherForecast instead
