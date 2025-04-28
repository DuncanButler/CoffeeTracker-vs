using CoffeeTracker.Models;
using CoffeeTracker.ApiService.Services;
using Microsoft.AspNetCore.Authorization;

namespace CoffeeTracker.ApiService.Endpoints
{
    public static class WeatherEndpoints
    {
        public static WebApplication MapWeatherEndpoints(this WebApplication app)
        {
            // Secure the weather forecast endpoint - requires authentication
            app.MapGet("/weatherforecast", async (IWeatherService weatherService) =>
            {
                // call the weather service to get the weather forecasts
                WeatherForecast[] forecasts = await weatherService.GetWeatherForecastAsync();
                return forecasts;
            })
            .WithName("GetWeatherForecast")
            .RequireAuthorization(policy => 
            {
                // Allow access to both web applications and internal services
                policy.RequireAuthenticatedUser();
            });

            return app;
        }
    }
}
