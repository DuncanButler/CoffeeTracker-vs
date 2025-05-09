using CoffeeTracker.Web.Clients;
using CoffeeTracker.Web.Components;
using CoffeeTracker.Web.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;
using System;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddOutputCache();

// Register the authentication service
builder.Services.AddScoped<AuthenticationService>();

// Register the weather client with authentication
builder.Services.AddScoped<IWeatherClient, WeatherClient>();

// Create a named HttpClient for API communication with authentication
builder.Services.AddHttpClient("API", client =>
    {
        // This URL uses "https+http://" to indicate HTTPS is preferred over HTTP.
        // Learn more about service discovery scheme resolution at https://aka.ms/dotnet/sdschemes.
        client.BaseAddress = new("https+http://apiservice");
        client.Timeout = TimeSpan.FromSeconds(10); // Set a reasonable timeout
    })
    // Add retry policy for transient errors
    .AddTransientHttpErrorPolicy(policy => policy
        .WaitAndRetryAsync(3, // Retry 3 times
            retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)) // Exponential backoff
        ))
    // Add circuit breaker policy to prevent overwhelming the service when it's failing
    .AddTransientHttpErrorPolicy(policy => policy
        .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)) // Break after 5 failures, reset after 30 seconds
    );

// If you still have the IWeatherApiClient interface and implementation, you can keep this registration
// but we'll use our new authenticated WeatherClient going forward
if (typeof(IWeatherApiClient).IsInterface && typeof(WeatherApiClient).IsClass)
{
    builder.Services.AddHttpClient<IWeatherApiClient, WeatherApiClient>(client =>
    {
        client.BaseAddress = new("https+http://apiservice");
        client.Timeout = TimeSpan.FromSeconds(10);
    })
    .AddTransientHttpErrorPolicy(policy => policy
        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))))
    .AddTransientHttpErrorPolicy(policy => policy
        .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)));
}

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.UseOutputCache();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();

app.Run();
