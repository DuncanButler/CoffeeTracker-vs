using CoffeeTracker.Models;
using CoffeeTracker.ApiService.Services;

namespace CoffeeTracker.ApiService.Endpoints
{
    public static class WeatherEndpoints
    {
        public static WebApplication MapWeatherEndpoints(this WebApplication app)
        {

            app.MapGet("/weatherforecast", async (IWeatherService weatherService) =>
            {
                // call the weather service to get the weather forecasts
                WeatherForecast[] forecasts = await weatherService.GetWeatherForecastAsync();
                return forecasts;
            })
            .WithName("GetWeatherForecast");

            return app;
        }
    }
}
