using CoffeeTracker.Models;

namespace CoffeeTracker.Web.Clients;

public interface IWeatherClient
{
    Task<WeatherForecast[]?> GetForecastAsync();
}
