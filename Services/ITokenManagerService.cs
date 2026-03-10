using QuickBooks.SalesTax.API.Models;

namespace QuickBooks.SalesTax.API.Services
{
    public interface ITokenManagerService
    {
        Task<OAuthToken> GetCurrentTokenAsync();
        Task SaveTokenAsync(OAuthToken token);
        Task<bool> RefreshTokenAsync();
        Task<bool> IsTokenValidAsync();
        Task RevokeTokenAsync();
    }
}
