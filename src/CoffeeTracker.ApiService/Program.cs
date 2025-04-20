using CoffeeTracker.ApiService.Endpoints;
using CoffeeTracker.ApiService.Interfaces;
using CoffeeTracker.ApiService.Repositories;
using CoffeeTracker.ApiService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// add service
builder.Services.AddScoped<IWeatherService, WeatherService>();
builder.Services.AddScoped<IWeatherRepository, WeatherRepository>();

// Add services to the container.
builder.Services.AddProblemDetails();

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

// map default extensions form service defaults
app.MapDefaultEndpoints();

app.Run();
