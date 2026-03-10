using Newtonsoft.Json;

namespace QuickBooks.SalesTax.API.Models
{
    public class OAuthToken
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public string RealmId { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public string Environment { get; set; } = "sandbox";
    }

    public class QuickBooksConfig
    {
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string RedirectUri { get; set; } = string.Empty;
        public string DiscoveryDocument { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = string.Empty;
        // Sales Tax API GraphQL Endpoints:
        // Production: https://qb.api.intuit.com/graphql
        // Sandbox: https://qb-sandbox.api.intuit.com/graphql
        public string GraphQLEndpoint { get; set; } = "https://qb-sandbox.api.intuit.com/graphql";
        public string Environment { get; set; } = "sandbox"; // "sandbox" or "production"
        public List<string> ProjectScopes { get; set; } = new List<string>();
        public string ScopeDescription { get; set; } = string.Empty;
        public EndpointConfig Endpoints { get; set; } = new EndpointConfig();
    }

    public class EndpointConfig
    {
        public string Production { get; set; } = "https://qb.api.intuit.com/graphql";
        public string Sandbox { get; set; } = "https://qb-sandbox.api.intuit.com/graphql";
    }

    public class ErrorResponse
    {
        public string Message { get; set; } = string.Empty;
        public string? Details { get; set; }
        public string? Code { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string? ErrorMessage { get; set; }
        public List<string>? ValidationErrors { get; set; }
    }
}
