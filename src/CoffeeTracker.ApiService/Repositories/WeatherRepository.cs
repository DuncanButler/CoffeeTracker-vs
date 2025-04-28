using CoffeeTracker.Data;
using CoffeeTracker.ApiService.Interfaces;
using CoffeeTracker.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace CoffeeTracker.ApiService.Repositories;

public class WeatherRepository : IWeatherRepository
{
    private readonly WeatherDbContext _dbContext;
    private readonly ILogger<WeatherRepository>? _logger;

    public WeatherRepository(WeatherDbContext dbContext, ILogger<WeatherRepository>? logger = null)
    {
        _dbContext = dbContext;
        _logger = logger;
    }    public async Task<WeatherForecast?> GetForecastForDay(DateOnly day)
    {
        try
        {
            // Fetch from the database using EF Core
            // Use AsNoTracking for read operations when you don't need to update the entity
            return await _dbContext.Forecasts.AsNoTracking().FirstOrDefaultAsync(f => f.Date == day);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error retrieving weather forecast for {Date}: {Message}", day, ex.Message);
            throw; // Rethrow so service layer can decide how to handle it
        }
    }    public async Task SaveForcastForDay(WeatherForecast dayforcast)
    {
        try
        {
            // Check if the forecast already exists
            var existingForecast = await _dbContext.Forecasts.FindAsync(dayforcast.Date);

            if (existingForecast == null)
            {
                _logger?.LogInformation("Adding new forecast for {Date}", dayforcast.Date);
                await AddNewForcast(dayforcast);
            }
            else
            {
                _logger?.LogInformation("Updating existing forecast for {Date}", dayforcast.Date);
                await UpdatedExistingForcast(existingForecast, dayforcast);
            }
            
            // Save changes to the database
            await _dbContext.SaveChangesAsync();
            _logger?.LogInformation("Successfully saved forecast for {Date}", dayforcast.Date);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger?.LogError(ex, "Concurrency conflict when saving forecast for {Date}: {Message}", dayforcast.Date, ex.Message);
            throw; // Rethrow so service layer can handle it
        }
        catch (DbUpdateException ex)
        {
            _logger?.LogError(ex, "Database error when saving forecast for {Date}: {Message}", dayforcast.Date, ex.Message);
            throw; // Rethrow so service layer can handle it
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Unexpected error when saving forecast for {Date}: {Message}", dayforcast.Date, ex.Message);
            throw; // Rethrow so service layer can handle it
        }
    }private Task UpdatedExistingForcast(WeatherForecast existingForecast, WeatherForecast dayforcast)
    {
        _dbContext.Entry(existingForecast).CurrentValues.SetValues(dayforcast);
        return Task.CompletedTask;
    }

    private async Task AddNewForcast(WeatherForecast dayforcast)
    {
        await _dbContext.Forecasts.AddAsync(dayforcast);
    }
}
