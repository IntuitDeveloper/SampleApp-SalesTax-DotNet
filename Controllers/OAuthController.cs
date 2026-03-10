using Microsoft.AspNetCore.Mvc;
using QuickBooks.SalesTax.API.Models;
using QuickBooks.SalesTax.API.Services;
using Intuit.Ipp.OAuth2PlatformClient;
using System.Security.Cryptography;
using System.Text;

namespace QuickBooks.SalesTax.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OAuthController : ControllerBase
    {
        private readonly QuickBooksConfig _config;
        private readonly ITokenManagerService _tokenManager;
        private readonly ILogger<OAuthController> _logger;

        public OAuthController(QuickBooksConfig config, ITokenManagerService tokenManager, ILogger<OAuthController> logger)
        {
            _config = config;
            _tokenManager = tokenManager;
            _logger = logger;
        }

        /// <summary>
        /// Initiate OAuth 2.0 authorization flow using configured scope (Swagger-only interface)
        /// </summary>
        [HttpGet("authorize")]
        public IActionResult Authorize()
        {
            try
            {
                var oauth2Client = new OAuth2Client(_config.ClientId, _config.ClientSecret, _config.RedirectUri, _config.Environment);
                
                // Generate state parameter for security
                var state = GenerateState();
                HttpContext.Session.SetString("oauth_state", state);

                // Request standard OAuth scopes (excluding Accounting as it's in custom scopes config)
                var standardScopes = new List<OidcScopes> 
                { 
                    OidcScopes.OpenId,
                    OidcScopes.Profile,
                    OidcScopes.Email,
                    OidcScopes.Phone,
                    OidcScopes.Address
                };
                
                var authorizeUrl = oauth2Client.GetAuthorizationURL(standardScopes, state);
                
                // Add custom scopes from configuration
                if (_config.ProjectScopes?.Any() == true)
                {
                    var customScopes = string.Join("%20", _config.ProjectScopes);
                    if (authorizeUrl.Contains("scope="))
                    {
                        authorizeUrl = authorizeUrl.Replace("scope=", $"scope={customScopes}%20");
                        _logger.LogInformation("Added custom scopes from configuration: {Scopes}", string.Join(", ", _config.ProjectScopes));
                    }
                }

                _logger.LogInformation("Generated OAuth authorization URL with configured scopes: {Scopes}", string.Join(", ", _config.ProjectScopes ?? new List<string>()));

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Data = new
                    {
                        AuthorizationUrl = authorizeUrl,
                        State = state,
                        Message = "Redirect to this URL to authorize with QuickBooks (Swagger-only interface)",
                        Scopes = (_config.ProjectScopes ?? new List<string>()).ToArray(),
                        ScopeDescription = _config.ScopeDescription,
                        Interface = "Swagger UI only - no secondary UI components"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating OAuth authorization URL");
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    ErrorMessage = ex.Message
                });
            }
        }

        /// <summary>
        /// Handle OAuth callback from QuickBooks
        /// </summary>
        [HttpGet("callback")]
        public async Task<IActionResult> Callback([FromQuery] string? code, [FromQuery] string? state, [FromQuery] string? realmId, [FromQuery] string? error = "")
        {
            try
            {
                if (!string.IsNullOrEmpty(error))
                {
                    _logger.LogError("OAuth callback received error: {Error}", error);
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        ErrorMessage = $"OAuth authorization failed: {error}"
                    });
                }

                // Verify state parameter
                var sessionState = HttpContext.Session.GetString("oauth_state");
                
                if (string.IsNullOrEmpty(state))
                {
                    _logger.LogError("OAuth callback received empty state parameter");
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        ErrorMessage = "Missing state parameter in OAuth callback."
                    });
                }
                
                if (string.IsNullOrEmpty(sessionState))
                {
                    _logger.LogWarning("Session state missing, but state parameter present: {State}", state);
                }
                else if (sessionState != state)
                {
                    _logger.LogError("OAuth state mismatch. Expected: {Expected}, Received: {Received}", sessionState, state);
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        ErrorMessage = "Invalid state parameter. Possible CSRF attack."
                    });
                }

                if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(realmId))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        ErrorMessage = "Missing authorization code or realm ID"
                    });
                }

                var oauth2Client = new OAuth2Client(_config.ClientId, _config.ClientSecret, _config.RedirectUri, _config.Environment);
                
                // Exchange authorization code for access token
                var tokenResponse = await oauth2Client.GetBearerTokenAsync(code);

                if (tokenResponse == null)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        ErrorMessage = "Failed to retrieve access token"
                    });
                }

                // Create and save token
                var token = new OAuthToken
                {
                    AccessToken = tokenResponse.AccessToken,
                    RefreshToken = tokenResponse.RefreshToken,
                    RealmId = realmId,
                    ExpiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.AccessTokenExpiresIn),
                    Environment = _config.Environment
                };

                await _tokenManager.SaveTokenAsync(token);

                _logger.LogInformation("OAuth token successfully saved for realm: {RealmId}", realmId);

                // Return JSON response instead of HTML
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Data = new
                    {
                        Message = "OAuth authorization successful! Token saved to JSON file.",
                        RealmId = realmId,
                        ExpiresAt = token.ExpiresAt,
                        TokenLocation = "token.json"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing OAuth callback");
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    ErrorMessage = ex.Message
                });
            }
        }

        /// <summary>
        /// Get current token status
        /// </summary>
        [HttpGet("status")]
        public async Task<IActionResult> GetTokenStatus()
        {
            try
            {
                var isValid = await _tokenManager.IsTokenValidAsync();
                var tokenFileExists = System.IO.File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "token.json"));

                if (!isValid)
                {
                    return Ok(new ApiResponse<object>
                    {
                        Success = true,
                        Data = new
                        {
                            IsAuthenticated = false,
                            TokenFileExists = tokenFileExists
                        }
                    });
                }

                var token = await _tokenManager.GetCurrentTokenAsync();

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Data = new
                    {
                        IsAuthenticated = true,
                        RealmId = token?.RealmId,
                        ExpiresAt = token?.ExpiresAt,
                        IsExpired = token?.ExpiresAt < DateTime.UtcNow,
                        MinutesUntilExpiry = token?.ExpiresAt > DateTime.UtcNow 
                            ? (int)(token.ExpiresAt - DateTime.UtcNow).TotalMinutes 
                            : 0,
                        TokenFileExists = tokenFileExists
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking token status");
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Data = new
                    {
                        IsAuthenticated = false,
                        Error = ex.Message,
                        TokenFileExists = System.IO.File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "token.json"))
                    }
                });
            }
        }

        /// <summary>
        /// Refresh the current access token
        /// </summary>
        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken()
        {
            try
            {
                var success = await _tokenManager.RefreshTokenAsync();
                
                if (success)
                {
                    var token = await _tokenManager.GetCurrentTokenAsync();
                    return Ok(new ApiResponse<object>
                    {
                        Success = true,
                        Data = new
                        {
                            Message = "Token refreshed successfully and saved to JSON file",
                            ExpiresAt = token?.ExpiresAt,
                            TokenLocation = "token.json"
                        }
                    });
                }
                else
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        ErrorMessage = "Failed to refresh token"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token");
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    ErrorMessage = ex.Message
                });
            }
        }

        /// <summary>
        /// Revoke the current token and disconnect from QuickBooks
        /// </summary>
        [HttpPost("disconnect")]
        public async Task<IActionResult> Disconnect()
        {
            try
            {
                await _tokenManager.RevokeTokenAsync();
                
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Data = new
                    {
                        Message = "Successfully disconnected from QuickBooks and removed token.json file"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disconnecting from QuickBooks");
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    ErrorMessage = ex.Message
                });
            }
        }

        /// <summary>
        /// Get information about OAuth implementation and Sales Tax API requirements
        /// </summary>
        [HttpGet("info")]
        public IActionResult GetOAuthInfo()
        {
            return Ok(new ApiResponse<object>
            {
                Success = true,
                Data = new
                {
                    Title = "QuickBooks Sales Tax API OAuth Implementation",
                    Description = "This API uses the Intuit .NET SDK for OAuth 2.0 authentication with QuickBooks Online - Swagger-only interface",
                    Scopes = _config.ProjectScopes.ToArray(),
                    ScopeDescription = _config.ScopeDescription,
                    ScopeNote = "Granular permissions limit access to only what's necessary for tax calculations",
                    Endpoints = _config.Endpoints,
                    CurrentEnvironment = _config.Environment,
                    CurrentEndpoint = _config.GraphQLEndpoint,
                    Interface = new
                    {
                        Type = "Swagger UI only",
                        Description = "No secondary UI components - all interactions through Swagger interface",
                        Access = "http://localhost:5038"
                    },
                    TokenStorage = new
                    {
                        Method = "JSON file storage",
                        Location = "token.json",
                        Description = "OAuth tokens are automatically stored in a JSON file for use by subsequent API calls via Swagger"
                    },
                    OAuthFlow = new
                    {
                        Step1 = "Call /api/oauth/authorize to get authorization URL",
                        Step2 = "Redirect user to authorization URL",
                        Step3 = "User authorizes and is redirected to /api/oauth/callback",
                        Step4 = "Token is automatically saved to token.json",
                        Step5 = "Use other API endpoints - token is automatically loaded from JSON file"
                    },
                    ManagementEndpoints = new
                    {
                        Status = "/api/oauth/status - Check current token status",
                        Refresh = "/api/oauth/refresh - Refresh expired tokens",
                        Disconnect = "/api/oauth/disconnect - Revoke tokens and delete JSON file"
                    }
                }
            });
        }

        private string GenerateState()
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[32];
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
        }
    }
} 