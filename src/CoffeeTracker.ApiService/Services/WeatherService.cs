using System;
using CoffeeTracker.ApiService.Interfaces;
using CoffeeTracker.Models;

namespace CoffeeTracker.ApiService.Services;

public interface IWeatherService
{
    Task<WeatherForecast[]> GetWeatherForecastAsync();
}

public class WeatherService : IWeatherService
{
    private readonly IWeatherRepository _repository;

    public WeatherService(IWeatherRepository repository)
    {
        this._repository = repository;
    }

    async Task<WeatherForecast[]> IWeatherService.GetWeatherForecastAsync()
    {
        var forecasts = new List<WeatherForecast>();

        // Process forecasts sequentially instead of in parallel to avoid DbContext concurrency issues
        for (int index = 1; index <= 5; index++)
        {
            var day = DateOnly.FromDateTime(DateTime.Now.AddDays(index));
            WeatherForecast? dayForecast = await _repository.GetForcastForDay(day);

            if (dayForecast is null)
            {
                dayForecast = GenerateForecastForDay(day);
                await _repository.SaveForcastForDay(dayForecast);
            }
            forecasts.Add(dayForecast);
        }

        return forecasts.ToArray();
    }

    WeatherForecast GenerateForecastForDay(DateOnly day)
    {
        string[] summaries = ["Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"];

        return new WeatherForecast(day,
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]);
    }
}
