using CoffeeTracker.ApiService.Interfaces;
using CoffeeTracker.ApiService.Models;

namespace CoffeeTracker.ApiService.Repositories;

public class WeatherRepository : IWeatherRepository
{
    public async Task<WeatherForecast?> GetForcastForDay(DateOnly day)
    {
        // Simulate fetching from the database (always returns null for testing)
        return await Task.FromResult<WeatherForecast?>(null);
    }

    public async Task SaveForcastForDay(WeatherForecast dayforcast)
    {
        // Simulate saving to the database (currently does nothing)
        await Task.CompletedTask;
    }
}
