using CoffeeTracker.ApiService.Data;
using CoffeeTracker.ApiService.Interfaces;
using CoffeeTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace CoffeeTracker.ApiService.Repositories;

public class WeatherRepository : IWeatherRepository
{
    private readonly WeatherDbContext _dbContext;

    public WeatherRepository(WeatherDbContext dbContext)
    {
        _dbContext = dbContext;
    }    public async Task<WeatherForecast?> GetForcastForDay(DateOnly day)
    {
        // Fetch from the database using EF Core
        // Use AsNoTracking for read operations when you don't need to update the entity
        return await _dbContext.Forecasts.AsNoTracking().FirstOrDefaultAsync(f => f.Date == day);
    }

    public async Task SaveForcastForDay(WeatherForecast dayforcast)
    {
        // Check if the forecast already exists
        var existingForecast = await _dbContext.Forecasts.FindAsync(dayforcast.Date);

        if (existingForecast == null)
        {
            await AddNewForcast(dayforcast);
        }
        else
        {
            await UpdatedExistingForcast(existingForecast, dayforcast);
        }
        
        // Save changes to the database
        await _dbContext.SaveChangesAsync();
    }

    private async Task UpdatedExistingForcast(WeatherForecast existingForecast, WeatherForecast dayforcast)
    {
        _dbContext.Entry(existingForecast).CurrentValues.SetValues(dayforcast);
    }

    private async Task AddNewForcast(WeatherForecast dayforcast)
    {
        await _dbContext.Forecasts.AddAsync(dayforcast);
    }
}
