using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CoffeeTracker.ApiService.Services;

public interface IAuthService
{
    string GenerateJwtToken(string clientId, string clientType);
    bool ValidateApiKey(string apiKey, string clientId, out string clientType);
}

public class AuthService : IAuthService
{
    private readonly IConfiguration _configuration;
    
    // In a real application, these would be stored in a database
    private readonly Dictionary<string, (string ClientId, string ClientType)> _apiKeys = new()
    {
        // API key for the web application
        { "web-app-api-key-1234567890", ("web-client", "WebApplication") },
        
        // API key for internal services
        { "internal-service-key-0987654321", ("internal-service", "InternalService") }
    };

    public AuthService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateJwtToken(string clientId, string clientType)
    {
        var issuer = _configuration["Jwt:Issuer"];
        var audience = _configuration["Jwt:Audience"];
        var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? "DefaultDevelopmentKeyThatShouldBeReplaced");
        var expiryMinutesStr = _configuration["Jwt:ExpiryInMinutes"] ?? "60";
        var expiryMinutes = int.TryParse(expiryMinutesStr, out var mins) ? mins : 60;
        
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, clientId),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("ClientType", clientType)
            }),
            Expires = DateTime.UtcNow.AddMinutes(expiryMinutes),
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        
        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        
        return tokenHandler.WriteToken(token);
    }

    public bool ValidateApiKey(string apiKey, string clientId, out string clientType)
    {
        clientType = string.Empty;
        
        if (string.IsNullOrEmpty(apiKey) || !_apiKeys.TryGetValue(apiKey, out var clientInfo))
            return false;
            
        if (clientInfo.ClientId != clientId)
            return false;
            
        clientType = clientInfo.ClientType;
        return true;
    }
}
