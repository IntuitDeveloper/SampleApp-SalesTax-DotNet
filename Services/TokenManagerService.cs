using QuickBooks.SalesTax.API.Models;
using System.Text.Json;

namespace QuickBooks.SalesTax.API.Services
{
    public class TokenManagerService : ITokenManagerService
    {
        private readonly QuickBooksConfig _config;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<TokenManagerService> _logger;
        private OAuthToken? _currentToken;
        private readonly string _tokenFilePath;

        public TokenManagerService(QuickBooksConfig config, IHttpClientFactory httpClientFactory, ILogger<TokenManagerService> logger)
        {
            _config = config;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _tokenFilePath = Path.Combine(Directory.GetCurrentDirectory(), "token.json");
            _logger.LogInformation("TokenManagerService initialized. Token file path: {TokenFilePath}", _tokenFilePath);
        }

        public async Task<OAuthToken> GetCurrentTokenAsync()
        {
            if (_currentToken == null)
            {
                await LoadTokenFromFileAsync();
            }

            if (_currentToken == null)
            {
                throw new InvalidOperationException("No valid token available. Please authenticate first using /api/oauth/authorize endpoint.");
            }

            // Check if token is expired and refresh if needed
            if (_currentToken.ExpiresAt <= DateTime.UtcNow.AddMinutes(5))
            {
                _logger.LogInformation("Token expires soon, attempting refresh. Current expiry: {ExpiresAt}", _currentToken.ExpiresAt);
                var refreshed = await RefreshTokenAsync();
                if (!refreshed)
                {
                    throw new InvalidOperationException("Token expired and refresh failed. Please re-authenticate using /api/oauth/authorize endpoint.");
                }
            }

            return _currentToken;
        }

        public async Task SaveTokenAsync(OAuthToken token)
        {
            _currentToken = token;
            await SaveTokenToFileAsync(token);
            _logger.LogInformation("OAuth token saved to JSON file for realm: {RealmId}, expires at: {ExpiresAt}", 
                token.RealmId, token.ExpiresAt);
        }

        public async Task<bool> RefreshTokenAsync()
        {
            if (_currentToken?.RefreshToken == null)
            {
                _logger.LogWarning("Cannot refresh token: no refresh token available");
                return false;
            }

            try
            {
                _logger.LogInformation("Attempting to refresh OAuth token for realm: {RealmId}", _currentToken.RealmId);
                
                using var httpClient = _httpClientFactory.CreateClient();
                
                var requestBody = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "refresh_token"),
                    new KeyValuePair<string, string>("refresh_token", _currentToken.RefreshToken),
                    new KeyValuePair<string, string>("client_id", _config.ClientId),
                    new KeyValuePair<string, string>("client_secret", _config.ClientSecret)
                });

                var response = await httpClient.PostAsync("https://oauth.platform.intuit.com/oauth2/v1/tokens/bearer", requestBody);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var tokenResponse = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);
                    
                    if (tokenResponse != null)
                    {
                        var newToken = new OAuthToken
                        {
                            AccessToken = tokenResponse["access_token"].ToString() ?? "",
                            RefreshToken = tokenResponse.ContainsKey("refresh_token") ? tokenResponse["refresh_token"].ToString() ?? _currentToken.RefreshToken : _currentToken.RefreshToken,
                            RealmId = _currentToken.RealmId,
                            ExpiresAt = DateTime.UtcNow.AddSeconds(int.Parse(tokenResponse["expires_in"].ToString() ?? "3600")),
                            Environment = _config.Environment
                        };

                        await SaveTokenAsync(newToken);
                        _logger.LogInformation("Token refresh successful. New expiry: {ExpiresAt}", newToken.ExpiresAt);
                        return true;
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Token refresh failed with status {StatusCode}: {ErrorContent}", response.StatusCode, errorContent);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token refresh failed with exception");
            }

            return false;
        }

        public async Task<bool> IsTokenValidAsync()
        {
            try
            {
                if (_currentToken == null)
                {
                    await LoadTokenFromFileAsync();
                }
                
                if (_currentToken == null)
                {
                    _logger.LogInformation("No token available - not authenticated");
                    return false;
                }

                var isValid = _currentToken.ExpiresAt > DateTime.UtcNow;
                _logger.LogInformation("Token validity check: {IsValid}, expires: {ExpiresAt}", isValid, _currentToken.ExpiresAt);
                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking token validity");
                return false;
            }
        }

        public async Task RevokeTokenAsync()
        {
            // Load token from file if not in memory, so we can attempt revocation
            if (_currentToken == null)
            {
                await LoadTokenFromFileAsync();
            }

            try
            {
                if (_currentToken?.RefreshToken != null)
                {
                    _logger.LogInformation("Revoking OAuth token for realm: {RealmId}", _currentToken.RealmId);
                    
                    using var httpClient = _httpClientFactory.CreateClient();
                    
                    var requestBody = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("token", _currentToken.RefreshToken)
                    });

                    httpClient.DefaultRequestHeaders.Add("Authorization", $"Basic {Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{_config.ClientId}:{_config.ClientSecret}"))}");
                    
                    var response = await httpClient.PostAsync("https://developer.api.intuit.com/v2/oauth2/tokens/revoke", requestBody);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("Token revocation successful");
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        _logger.LogWarning("Token revocation failed with status {StatusCode}: {ErrorContent}", response.StatusCode, errorContent);
                    }
                }
                else
                {
                    _logger.LogWarning("No refresh token available to revoke, proceeding to delete token file");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token revocation request failed, proceeding to delete token file");
            }
            finally
            {
                _currentToken = null;
                if (File.Exists(_tokenFilePath))
                {
                    File.Delete(_tokenFilePath);
                    _logger.LogInformation("Token file deleted: {TokenFilePath}", _tokenFilePath);
                }
            }
        }

        private async Task LoadTokenFromFileAsync()
        {
            try
            {
                if (File.Exists(_tokenFilePath))
                {
                    _logger.LogInformation("Loading token from file: {TokenFilePath}", _tokenFilePath);
                    var tokenJson = await File.ReadAllTextAsync(_tokenFilePath);
                    _currentToken = JsonSerializer.Deserialize<OAuthToken>(tokenJson);
                    
                    if (_currentToken != null)
                    {
                        _logger.LogInformation("Token loaded successfully for realm: {RealmId}, expires: {ExpiresAt}", 
                            _currentToken.RealmId, _currentToken.ExpiresAt);
                    }
                    else
                    {
                        _logger.LogWarning("Token file exists but could not be deserialized");
                    }
                }
                else
                {
                    _logger.LogInformation("No token file found at: {TokenFilePath}", _tokenFilePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load token from file: {TokenFilePath}", _tokenFilePath);
            }
        }

        private async Task SaveTokenToFileAsync(OAuthToken token)
        {
            try
            {
                var tokenJson = JsonSerializer.Serialize(token, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(_tokenFilePath, tokenJson);
                _logger.LogInformation("Token saved to file: {TokenFilePath}", _tokenFilePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save token to file: {TokenFilePath}", _tokenFilePath);
                throw; // Re-throw since this is a critical operation
            }
        }
    }
}
