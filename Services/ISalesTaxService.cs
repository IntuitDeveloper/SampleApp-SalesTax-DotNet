using QuickBooks.SalesTax.API.Models;

namespace QuickBooks.SalesTax.API.Services
{
    public interface ISalesTaxService
    {
        /// <summary>
        /// Calculate sales tax for a transaction
        /// </summary>
        Task<TaxCalculation?> CalculateSaleTransactionTaxAsync(OAuthToken token, CalculateSaleTransactionTaxRequest request);
        
        /// <summary>
        /// Get tax rates for a specific address
        /// </summary>
        Task<List<TaxRate>> GetTaxRatesAsync(OAuthToken token, TaxRateQueryRequest request);
        
        /// <summary>
        /// Get tax jurisdictions for an address
        /// </summary>
        Task<List<TaxJurisdiction>> GetTaxJurisdictionsAsync(OAuthToken token, AddressInput address);
        
        /// <summary>
        /// Get customers from QuickBooks
        /// </summary>
        Task<List<QBCustomer>> GetCustomersAsync(OAuthToken token);
    }
}
