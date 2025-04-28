using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace CoffeeTracker.Web.Services;

public class AuthenticationService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AuthenticationService> _logger;
    private readonly IConfiguration _configuration;
    private TokenInfo? _tokenInfo;

    public AuthenticationService(
        IHttpClientFactory httpClientFactory,
        ILogger<AuthenticationService> logger,
        IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<string?> GetTokenAsync()
    {
        // Check if we already have a valid token
        if (_tokenInfo != null && _tokenInfo.Expiration > DateTime.UtcNow.AddMinutes(5))
        {
            return _tokenInfo.AccessToken;
        }

        // Need to request a new token
        try
        {
            var client = _httpClientFactory.CreateClient("API");
            
            // Get API key from configuration
            var apiKey = _configuration["ApiAuthentication:ApiKey"] 
                ?? throw new InvalidOperationException("API key not configured");
            
            var clientId = _configuration["ApiAuthentication:ClientId"] 
                ?? throw new InvalidOperationException("Client ID not configured");

            // Create token request
            var tokenRequest = new TokenRequest
            {
                ApiKey = apiKey,
                ClientId = clientId
            };

            // Request token from API
            var response = await client.PostAsJsonAsync("/auth/token", tokenRequest);
            response.EnsureSuccessStatusCode();

            // Parse token response
            var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>();
            if (tokenResponse == null)
            {
                _logger.LogError("Failed to deserialize token response");
                return null;
            }

            // Store token
            _tokenInfo = new TokenInfo
            {
                AccessToken = tokenResponse.AccessToken,
                Expiration = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn)
            };

            return _tokenInfo.AccessToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to acquire authentication token");
            return null;
        }
    }
    
    public async Task<HttpClient> GetAuthenticatedHttpClientAsync()
    {
        var client = _httpClientFactory.CreateClient("API");
        var token = await GetTokenAsync();
        
        if (token != null)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        else
        {
            _logger.LogWarning("Could not get authentication token. Request will be unauthorized.");
        }
        
        return client;
    }
    
    // Token request and response models
    private class TokenRequest
    {
        public string ApiKey { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
    }

    private class TokenResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public int ExpiresIn { get; set; }
        public string TokenType { get; set; } = string.Empty;
    }

    private class TokenInfo
    {
        public string AccessToken { get; set; } = string.Empty;
        public DateTime Expiration { get; set; }
    }
}
