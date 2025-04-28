using Microsoft.AspNetCore.Mvc;
using CoffeeTracker.ApiService.Services;

namespace CoffeeTracker.ApiService.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/auth");
        
        // Endpoint to get a JWT token using an API key
        group.MapPost("/token", GetToken)
            .AllowAnonymous()
            .WithName("GetToken")
            .WithDescription("Generates a JWT token for an authenticated client using an API key");
    }

    public static IResult GetToken([FromBody] TokenRequest request, [FromServices] IAuthService authService)
    {
        // Check if an API key is provided
        if (string.IsNullOrEmpty(request.ApiKey))
            return Results.BadRequest("API key is required");
        
        // Check if client ID is provided
        if (string.IsNullOrEmpty(request.ClientId))
            return Results.BadRequest("Client ID is required");
        
        // Validate API key
        if (!authService.ValidateApiKey(request.ApiKey, request.ClientId, out var clientType))
            return Results.Unauthorized();
        
        // Generate JWT token
        var token = authService.GenerateJwtToken(request.ClientId, clientType);
        
        // Return the token
        return Results.Ok(new TokenResponse
        {
            AccessToken = token,
            ExpiresIn = 3600, // 1 hour
            TokenType = "Bearer"
        });
    }
}

public class TokenRequest
{
    public string ApiKey { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
}

public class TokenResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public string TokenType { get; set; } = string.Empty;
}
