using CoffeeTracker.Data;
using CoffeeTracker.ApiService.Endpoints;
using CoffeeTracker.ApiService.Interfaces;
using CoffeeTracker.ApiService.Repositories;
using CoffeeTracker.ApiService.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add PostgreSQL DbContext using Aspire service discovery with improved connection settings
builder.AddNpgsqlDbContext<WeatherDbContext>("weatherdb", 
    configureDbContextOptions: options =>
    {
        // Configure PostgreSQL-specific options with improved reliability
        options.UseNpgsql(npgsqlBuilder =>
        {
            // Enable retry on failure with more attempts and longer intervals
            npgsqlBuilder.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorCodesToAdd: null);
            
            // Set appropriate timeout to avoid stream reading errors
            npgsqlBuilder.CommandTimeout(30);
            
            // Explicitly set the migrations history table schema to public
            npgsqlBuilder.MigrationsHistoryTable("__EFMigrationsHistory", "public");
        });
    });

// add service
builder.Services.AddScoped<IWeatherService, WeatherService>();
builder.Services.AddScoped<IWeatherRepository, WeatherRepository>();

// Add services to the container.
builder.Services.AddProblemDetails();

// Add health checks
builder.Services.AddHealthChecks()
    // Add database health check with more detailed reporting
    .AddDbContextCheck<WeatherDbContext>("weatherdb_ef_check", 
        tags: new[] { "database", "ef" },
        customTestQuery: async (context, cancellationToken) => 
            await context.Forecasts.AnyAsync(cancellationToken: cancellationToken))
    // Add PostgreSQL connection health check
    .AddNpgSql(builder.Configuration.GetConnectionString("weatherdb") ?? "",
        name: "weatherdb_connection",
        tags: new[] { "database", "postgres" },
        timeout: TimeSpan.FromSeconds(5));

// Add Swagger services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

// Configure Swagger middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// map weather endpoint
app.MapWeatherEndpoints();

// Add health check endpoint with detailed results
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) => 
    {
        context.Response.ContentType = "application/json";
        
        var result = new 
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new 
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration.ToString()
            }),
            totalDuration = report.TotalDuration.ToString()
        };
        
        await System.Text.Json.JsonSerializer.SerializeAsync(
            context.Response.Body, result);
    }
});

// map default extensions from service defaults
app.MapDefaultEndpoints();

app.Run();
