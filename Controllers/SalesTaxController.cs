using Microsoft.AspNetCore.Mvc;
using QuickBooks.SalesTax.API.Models;
using QuickBooks.SalesTax.API.Services;

namespace QuickBooks.SalesTax.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SalesTaxController : ControllerBase
    {
        private readonly ISalesTaxService _salesTaxService;
        private readonly ITokenManagerService _tokenManager;

        public SalesTaxController(ISalesTaxService salesTaxService, ITokenManagerService tokenManager)
        {
            _salesTaxService = salesTaxService;
            _tokenManager = tokenManager;
        }

        /// <summary>
        /// Calculate sales tax for a transaction
        /// </summary>
        [HttpPost("calculate")]
        public async Task<IActionResult> CalculateSaleTransactionTax([FromBody] CalculateSaleTransactionTaxRequest request)
        {
            try
            {
                if (request.Transaction == null || !request.Transaction.Lines.Any())
                {
                    return BadRequest(new ApiResponse<TaxCalculation>
                    {
                        Success = false,
                        ErrorMessage = "Transaction with at least one line item is required"
                    });
                }

                var token = await _tokenManager.GetCurrentTokenAsync();
                var taxCalculation = await _salesTaxService.CalculateSaleTransactionTaxAsync(token, request);
                
                return Ok(new ApiResponse<TaxCalculation>
                {
                    Success = true,
                    Data = taxCalculation
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<TaxCalculation>
                {
                    Success = false,
                    ErrorMessage = ex.Message
                });
            }
        }

        /// <summary>
        /// Get tax rates for a specific address
        /// </summary>
        [HttpPost("rates")]
        public async Task<IActionResult> GetTaxRates([FromBody] TaxRateQueryRequest request)
        {
            try
            {
                if (request.Address == null || string.IsNullOrEmpty(request.Address.State))
                {
                    return BadRequest(new ApiResponse<List<TaxRate>>
                    {
                        Success = false,
                        ErrorMessage = "Address with state is required"
                    });
                }

                var token = await _tokenManager.GetCurrentTokenAsync();
                var taxRates = await _salesTaxService.GetTaxRatesAsync(token, request);
                
                return Ok(new ApiResponse<List<TaxRate>>
                {
                    Success = true,
                    Data = taxRates
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<List<TaxRate>>
                {
                    Success = false,
                    ErrorMessage = ex.Message
                });
            }
        }

        /// <summary>
        /// Get tax jurisdictions for an address
        /// </summary>
        [HttpPost("jurisdictions")]
        public async Task<IActionResult> GetTaxJurisdictions([FromBody] AddressInput address)
        {
            try
            {
                if (address == null || string.IsNullOrEmpty(address.State))
                {
                    return BadRequest(new ApiResponse<List<TaxJurisdiction>>
                    {
                        Success = false,
                        ErrorMessage = "Address with state is required"
                    });
                }

                var token = await _tokenManager.GetCurrentTokenAsync();
                var jurisdictions = await _salesTaxService.GetTaxJurisdictionsAsync(token, address);
                
                return Ok(new ApiResponse<List<TaxJurisdiction>>
                {
                    Success = true,
                    Data = jurisdictions
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<List<TaxJurisdiction>>
                {
                    Success = false,
                    ErrorMessage = ex.Message
                });
            }
        }



        /// <summary>
        /// Calculate tax for a simple transaction (simplified endpoint)
        /// </summary>
        [HttpPost("calculate/simple")]
        public async Task<IActionResult> CalculateSimpleTax(
            [FromQuery] decimal amount,
            [FromQuery] string? description,
            [FromQuery] string customerState,
            [FromQuery] string customerCity,
            [FromQuery] string customerPostalCode,
            [FromQuery] string? customerId,
            [FromQuery] string? shipFromLine1,
            [FromQuery] string? shipFromCity,
            [FromQuery] string? shipFromState,
            [FromQuery] string? shipFromPostalCode,
            [FromQuery] DateTime? transactionDate = null)
        {
            try
            {
                if (amount <= 0)
                {
                    return BadRequest(new ApiResponse<TaxCalculation>
                    {
                        Success = false,
                        ErrorMessage = "Amount must be greater than 0"
                    });
                }

                if (string.IsNullOrEmpty(customerState))
                {
                    return BadRequest(new ApiResponse<TaxCalculation>
                    {
                        Success = false,
                        ErrorMessage = "Customer state is required"
                    });
                }

                var request = new CalculateSaleTransactionTaxRequest
                {
                    Transaction = new SaleTransactionInput
                    {
                        TransactionDate = transactionDate ?? DateTime.Now,
                        CustomerId = customerId,
                        Lines = new List<SaleTransactionLineInput>
                        {
                            new SaleTransactionLineInput
                            {
                                Amount = amount,
                                Description = description ?? "Sale item"
                            }
                        },
                        CustomerAddress = new AddressInput
                        {
                            Line1 = "Customer Address",
                            City = customerCity,
                            State = customerState,
                            PostalCode = customerPostalCode,
                            Country = "US"
                        },
                        ShipFromAddress = !string.IsNullOrEmpty(shipFromCity) && !string.IsNullOrEmpty(shipFromState) ? new AddressInput
                        {
                            Line1 = shipFromLine1 ?? "Business Address",
                            City = shipFromCity,
                            State = shipFromState,
                            PostalCode = shipFromPostalCode ?? throw new ArgumentException("shipFromPostalCode is required when shipFromCity and shipFromState are provided"),
                            Country = "US"
                        } : null
                    }
                };

                var token = await _tokenManager.GetCurrentTokenAsync();
                var taxCalculation = await _salesTaxService.CalculateSaleTransactionTaxAsync(token, request);
                
                return Ok(new ApiResponse<TaxCalculation>
                {
                    Success = true,
                    Data = taxCalculation
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<TaxCalculation>
                {
                    Success = false,
                    ErrorMessage = ex.Message
                });
            }
        }
    }
}
