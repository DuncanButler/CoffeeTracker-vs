namespace CoffeeTracker.Models;

// Changed from record to class for better EF Core compatibility
public class WeatherForecast
{
    // Primary key
    public DateOnly Date { get; set; }
    
    public int TemperatureC { get; set; }
    
    public string? Summary { get; set; }
    
    // Computed property (not stored in database)
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
    
    // Parameterless constructor for EF Core
    public WeatherForecast()
    {
    }
    
    // Constructor for convenience
    public WeatherForecast(DateOnly date, int temperatureC, string? summary = null)
    {
        Date = date;
        TemperatureC = temperatureC;
        Summary = summary;
    }
}
