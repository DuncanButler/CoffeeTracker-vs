using System;
using CoffeeTracker.ApiService.Interfaces;
using CoffeeTracker.ApiService.Models;

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
        WeatherForecast[] forecast = await Task.WhenAll(Enumerable.Range(1, 5).Select(async index =>
        {
            var day = DateOnly.FromDateTime(DateTime.Now.AddDays(index));
            WeatherForecast? dayforcast = await _repository.GetForcastForDay(day);

            if (dayforcast is null)
            {
                dayforcast = GenerateForecastForDay(day);
                await _repository.SaveForcastForDay(dayforcast);
            }
            return dayforcast;
        }));

        return forecast;
    }

    WeatherForecast GenerateForecastForDay(DateOnly day)
    {
        string[] summaries = ["Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"];

        return new WeatherForecast(day,
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]);
    }
}
