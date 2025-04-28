using CoffeeTracker.Models;

namespace CoffeeTracker.ApiService.Interfaces;
public interface IWeatherRepository
{
    Task<WeatherForecast?> GetForcastForDay(DateOnly day);
    Task SaveForecastForDay(WeatherForecast dayForecast);
}
