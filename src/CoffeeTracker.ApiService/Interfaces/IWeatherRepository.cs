using CoffeeTracker.ApiService.Models;

namespace CoffeeTracker.ApiService.Interfaces;
public interface IWeatherRepository
{
    Task<WeatherForecast?> GetForcastForDay(DateOnly day);
    Task SaveForcastForDay(WeatherForecast dayforcast);
}
