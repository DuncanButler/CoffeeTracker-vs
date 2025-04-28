using CoffeeTracker.Models;

namespace CoffeeTracker.ApiService.Interfaces;

public interface IWeatherRepository
{
    Task<WeatherForecast?> GetForecastForDay(DateOnly day);
    Task SaveForecastForDay(WeatherForecast dayForecast);
}



