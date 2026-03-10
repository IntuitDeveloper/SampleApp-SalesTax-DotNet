using Microsoft.AspNetCore.Mvc;
using QuickBooks.SalesTax.API.Models;
using QuickBooks.SalesTax.API.Services;
using Newtonsoft.Json;

namespace QuickBooks.SalesTax.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class QuickBooksController : ControllerBase
    {
        private readonly QuickBooksConfig _config;
        private readonly ITokenManagerService _tokenManager;
        private readonly ILogger<QuickBooksController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public QuickBooksController(
            QuickBooksConfig config, 
            ITokenManagerService tokenManager, 
            ILogger<QuickBooksController> logger,
            IHttpClientFactory httpClientFactory)
        {
            _config = config;
            _tokenManager = tokenManager;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// Get invoices from QuickBooks using REST API with pagination
        /// </summary>
        [HttpGet("invoices")]
        public async Task<IActionResult> GetInvoices([FromQuery] string? realmId = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var token = await _tokenManager.GetCurrentTokenAsync();
                
                if (token == null)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        ErrorMessage = "No valid token available. Please authenticate first."
                    });
                }

                var baseUri = _config.BaseUrl;

                using var httpClient = _httpClientFactory.CreateClient();
                httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.AccessToken);
                httpClient.DefaultRequestHeaders.Accept.Add(
                    new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                // First, get total count of invoices
                var countQuery = "SELECT COUNT(*) FROM Invoice";
                var countUrl = $"{baseUri}/v3/company/{token.RealmId}/query?query={Uri.EscapeDataString(countQuery)}";
                
                var countResponse = await httpClient.GetAsync(countUrl);
                var countContent = await countResponse.Content.ReadAsStringAsync();
                var totalCount = 0;

                if (countResponse.IsSuccessStatusCode)
                {
                    var countResult = JsonConvert.DeserializeObject<InvoiceCountResponse>(countContent);
                    totalCount = countResult?.QueryResponse?.TotalCount ?? 0;
                }

                // Calculate pagination
                var startPosition = ((page - 1) * pageSize) + 1; // QuickBooks uses 1-based index
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                // Fetch paginated invoices
                var query = $"SELECT * FROM Invoice STARTPOSITION {startPosition} MAXRESULTS {pageSize}";
                var url = $"{baseUri}/v3/company/{token.RealmId}/query?query={Uri.EscapeDataString(query)}";
                
                _logger.LogInformation("Fetching invoices from: {Url}", url);

                var response = await httpClient.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var invoiceResponse = JsonConvert.DeserializeObject<InvoiceQueryResponse>(content);
                    var invoices = invoiceResponse?.QueryResponse?.Invoice ?? new List<Invoice>();

                    _logger.LogInformation("Successfully fetched {Count} invoices (page {Page} of {TotalPages})", invoices.Count, page, totalPages);

                    return Ok(new ApiResponse<PaginatedInvoiceResult>
                    {
                        Success = true,
                        Data = new PaginatedInvoiceResult
                        {
                            Invoices = invoices,
                            CurrentPage = page,
                            PageSize = pageSize,
                            TotalCount = totalCount,
                            TotalPages = totalPages
                        }
                    });
                }
                else
                {
                    _logger.LogError("Failed to fetch invoices: {StatusCode} - {Content}", response.StatusCode, content);
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        ErrorMessage = $"Failed to fetch invoices: {response.StatusCode}"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching invoices");
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    ErrorMessage = ex.Message
                });
            }
        }

        /// <summary>
        /// Get company info from QuickBooks (for Ship From address)
        /// </summary>
        [HttpGet("companyinfo")]
        public async Task<IActionResult> GetCompanyInfo()
        {
            try
            {
                var token = await _tokenManager.GetCurrentTokenAsync();
                
                if (token == null)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        ErrorMessage = "No valid token available. Please authenticate first."
                    });
                }

                var baseUri = _config.BaseUrl;

                using var httpClient = _httpClientFactory.CreateClient();
                httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.AccessToken);
                httpClient.DefaultRequestHeaders.Accept.Add(
                    new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                var url = $"{baseUri}/v3/company/{token.RealmId}/companyinfo/{token.RealmId}";
                
                _logger.LogInformation("Fetching company info from: {Url}", url);

                var response = await httpClient.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var companyResponse = JsonConvert.DeserializeObject<CompanyInfoResponse>(content);

                    return Ok(new ApiResponse<CompanyInfo>
                    {
                        Success = true,
                        Data = companyResponse?.CompanyInfo
                    });
                }
                else
                {
                    _logger.LogError("Failed to fetch company info: {StatusCode} - {Content}", response.StatusCode, content);
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        ErrorMessage = $"Failed to fetch company info: {response.StatusCode}"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching company info");
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    ErrorMessage = ex.Message
                });
            }
        }

        /// <summary>
        /// Get customers from QuickBooks using REST API
        /// </summary>
        [HttpGet("customers")]
        public async Task<IActionResult> GetCustomers([FromQuery] string? realmId = null)
        {
            try
            {
                var token = await _tokenManager.GetCurrentTokenAsync();
                
                if (token == null)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        ErrorMessage = "No valid token available. Please authenticate first."
                    });
                }

                var baseUri = _config.BaseUrl;

                using var httpClient = _httpClientFactory.CreateClient();
                httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.AccessToken);
                httpClient.DefaultRequestHeaders.Accept.Add(
                    new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                var query = "SELECT * FROM Customer WHERE Active = true MAXRESULTS 100";
                var url = $"{baseUri}/v3/company/{token.RealmId}/query?query={Uri.EscapeDataString(query)}";
                
                _logger.LogInformation("Fetching customers from: {Url}", url);

                var response = await httpClient.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var customerResponse = JsonConvert.DeserializeObject<CustomerQueryResponse>(content);
                    var customers = customerResponse?.QueryResponse?.Customer ?? new List<CustomerListItem>();

                    _logger.LogInformation("Successfully fetched {Count} customers", customers.Count);

                    return Ok(new ApiResponse<List<CustomerListItem>>
                    {
                        Success = true,
                        Data = customers
                    });
                }
                else
                {
                    _logger.LogError("Failed to fetch customers: {StatusCode} - {Content}", response.StatusCode, content);
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        ErrorMessage = $"Failed to fetch customers: {response.StatusCode}"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching customers");
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    ErrorMessage = ex.Message
                });
            }
        }

        /// <summary>
        /// Get items from QuickBooks using REST API
        /// </summary>
        [HttpGet("items")]
        public async Task<IActionResult> GetItems([FromQuery] string? realmId = null)
        {
            try
            {
                var token = await _tokenManager.GetCurrentTokenAsync();
                
                if (token == null)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        ErrorMessage = "No valid token available. Please authenticate first."
                    });
                }

                var baseUri = _config.BaseUrl;

                using var httpClient = _httpClientFactory.CreateClient();
                httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.AccessToken);
                httpClient.DefaultRequestHeaders.Accept.Add(
                    new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                var query = "SELECT * FROM Item WHERE Active = true MAXRESULTS 100";
                var url = $"{baseUri}/v3/company/{token.RealmId}/query?query={Uri.EscapeDataString(query)}";
                
                _logger.LogInformation("Fetching items from: {Url}", url);

                var response = await httpClient.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var itemResponse = JsonConvert.DeserializeObject<ItemQueryResponse>(content);
                    var items = itemResponse?.QueryResponse?.Item ?? new List<Item>();

                    _logger.LogInformation("Successfully fetched {Count} items", items.Count);

                    return Ok(new ApiResponse<List<Item>>
                    {
                        Success = true,
                        Data = items
                    });
                }
                else
                {
                    _logger.LogError("Failed to fetch items: {StatusCode} - {Content}", response.StatusCode, content);
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        ErrorMessage = $"Failed to fetch items: {response.StatusCode}"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching items");
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    ErrorMessage = ex.Message
                });
            }
        }

        /// <summary>
        /// Get a specific customer by ID with full details including address
        /// </summary>
        [HttpGet("customers/{customerId}")]
        public async Task<IActionResult> GetCustomerById(string customerId, [FromQuery] string? realmId = null)
        {
            try
            {
                var token = await _tokenManager.GetCurrentTokenAsync();
                
                if (token == null)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        ErrorMessage = "No valid token available. Please authenticate first."
                    });
                }

                var baseUri = _config.BaseUrl;

                using var httpClient = _httpClientFactory.CreateClient();
                httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.AccessToken);
                httpClient.DefaultRequestHeaders.Accept.Add(
                    new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                var url = $"{baseUri}/v3/company/{token.RealmId}/customer/{customerId}";
                
                _logger.LogInformation("Fetching customer {CustomerId} from: {Url}", customerId, url);

                var response = await httpClient.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var customerResponse = JsonConvert.DeserializeObject<CustomerSingleResponse>(content);

                    return Ok(new ApiResponse<CustomerDetail>
                    {
                        Success = true,
                        Data = customerResponse?.Customer
                    });
                }
                else
                {
                    _logger.LogError("Failed to fetch customer: {StatusCode} - {Content}", response.StatusCode, content);
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        ErrorMessage = $"Failed to fetch customer: {response.StatusCode}"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching customer {CustomerId}", customerId);
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    ErrorMessage = ex.Message
                });
            }
        }

        /// <summary>
        /// Get a specific invoice by ID
        /// </summary>
        [HttpGet("invoices/{invoiceId}")]
        public async Task<IActionResult> GetInvoiceById(string invoiceId)
        {
            try
            {
                var token = await _tokenManager.GetCurrentTokenAsync();
                
                if (token == null)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        ErrorMessage = "No valid token available. Please authenticate first."
                    });
                }

                var baseUri = _config.BaseUrl;

                using var httpClient = _httpClientFactory.CreateClient();
                httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.AccessToken);
                httpClient.DefaultRequestHeaders.Accept.Add(
                    new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                var url = $"{baseUri}/v3/company/{token.RealmId}/invoice/{invoiceId}";
                
                _logger.LogInformation("Fetching invoice {InvoiceId} from: {Url}", invoiceId, url);

                var response = await httpClient.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var invoiceResponse = JsonConvert.DeserializeObject<InvoiceSingleResponse>(content);

                    return Ok(new ApiResponse<Invoice>
                    {
                        Success = true,
                        Data = invoiceResponse?.Invoice
                    });
                }
                else
                {
                    _logger.LogError("Failed to fetch invoice: {StatusCode} - {Content}", response.StatusCode, content);
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        ErrorMessage = $"Failed to fetch invoice: {response.StatusCode}"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching invoice {InvoiceId}", invoiceId);
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    ErrorMessage = ex.Message
                });
            }
        }
    }

    // Invoice Models for QuickBooks REST API
    public class InvoiceQueryResponse
    {
        [JsonProperty("QueryResponse")]
        public InvoiceQueryData? QueryResponse { get; set; }
    }

    public class InvoiceQueryData
    {
        [JsonProperty("Invoice")]
        public List<Invoice>? Invoice { get; set; }

        [JsonProperty("maxResults")]
        public int MaxResults { get; set; }

        [JsonProperty("startPosition")]
        public int StartPosition { get; set; }

        [JsonProperty("totalCount")]
        public int TotalCount { get; set; }
    }

    public class InvoiceCountResponse
    {
        [JsonProperty("QueryResponse")]
        public InvoiceCountData? QueryResponse { get; set; }
    }

    public class InvoiceCountData
    {
        [JsonProperty("totalCount")]
        public int TotalCount { get; set; }
    }

    public class PaginatedInvoiceResult
    {
        [JsonProperty("invoices")]
        public List<Invoice> Invoices { get; set; } = new();

        [JsonProperty("currentPage")]
        public int CurrentPage { get; set; }

        [JsonProperty("pageSize")]
        public int PageSize { get; set; }

        [JsonProperty("totalCount")]
        public int TotalCount { get; set; }

        [JsonProperty("totalPages")]
        public int TotalPages { get; set; }
    }

    public class InvoiceSingleResponse
    {
        [JsonProperty("Invoice")]
        public Invoice? Invoice { get; set; }
    }

    public class Invoice
    {
        [JsonProperty("Id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("DocNumber")]
        public string DocNumber { get; set; } = string.Empty;

        [JsonProperty("TxnDate")]
        public string TxnDate { get; set; } = string.Empty;

        [JsonProperty("CustomerRef")]
        public CustomerReference CustomerRef { get; set; } = new();

        [JsonProperty("BillAddr")]
        public Address BillAddr { get; set; } = new();

        [JsonProperty("ShipAddr")]
        public Address? ShipAddr { get; set; }

        [JsonProperty("TotalAmt")]
        public decimal TotalAmt { get; set; }

        [JsonProperty("Balance")]
        public decimal Balance { get; set; }

        [JsonProperty("Line")]
        public List<InvoiceLine> Line { get; set; } = new();

        [JsonProperty("DueDate")]
        public string? DueDate { get; set; }

        [JsonProperty("TxnTaxDetail")]
        public TaxDetail? TxnTaxDetail { get; set; }
    }

    public class CustomerReference
    {
        [JsonProperty("value")]
        public string Value { get; set; } = string.Empty;

        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;
    }

    public class Address
    {
        [JsonProperty("Line1")]
        public string Line1 { get; set; } = string.Empty;

        [JsonProperty("City")]
        public string City { get; set; } = string.Empty;

        [JsonProperty("CountrySubDivisionCode")]
        public string CountrySubDivisionCode { get; set; } = string.Empty;

        [JsonProperty("PostalCode")]
        public string PostalCode { get; set; } = string.Empty;

        [JsonProperty("Country")]
        public string? Country { get; set; }
    }

    public class InvoiceLine
    {
        [JsonProperty("Id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("LineNum")]
        public int LineNum { get; set; }

        [JsonProperty("Description")]
        public string? Description { get; set; }

        [JsonProperty("Amount")]
        public decimal Amount { get; set; }

        [JsonProperty("DetailType")]
        public string DetailType { get; set; } = string.Empty;

        [JsonProperty("SalesItemLineDetail")]
        public SalesItemLineDetail? SalesItemLineDetail { get; set; }
    }

    public class SalesItemLineDetail
    {
        [JsonProperty("ItemRef")]
        public ItemReference? ItemRef { get; set; }

        [JsonProperty("Qty")]
        public decimal Qty { get; set; }

        [JsonProperty("UnitPrice")]
        public decimal UnitPrice { get; set; }

        [JsonProperty("TaxCodeRef")]
        public TaxCodeReference? TaxCodeRef { get; set; }
    }

    public class ItemReference
    {
        [JsonProperty("value")]
        public string Value { get; set; } = string.Empty;

        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;
    }

    public class TaxCodeReference
    {
        [JsonProperty("value")]
        public string Value { get; set; } = string.Empty;
    }

    public class TaxDetail
    {
        [JsonProperty("TotalTax")]
        public decimal TotalTax { get; set; }

        [JsonProperty("TaxLine")]
        public List<TaxLine>? TaxLine { get; set; }
    }

    public class TaxLine
    {
        [JsonProperty("Amount")]
        public decimal Amount { get; set; }

        [JsonProperty("DetailType")]
        public string DetailType { get; set; } = string.Empty;
    }

    // Item Models
    public class ItemQueryResponse
    {
        [JsonProperty("QueryResponse")]
        public ItemQueryData? QueryResponse { get; set; }
    }

    public class ItemQueryData
    {
        [JsonProperty("Item")]
        public List<Item>? Item { get; set; }
    }

    public class Item
    {
        [JsonProperty("Id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("Name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("Description")]
        public string? Description { get; set; }

        [JsonProperty("UnitPrice")]
        public decimal UnitPrice { get; set; }

        [JsonProperty("Type")]
        public string Type { get; set; } = string.Empty;

        [JsonProperty("Active")]
        public bool Active { get; set; }

        [JsonProperty("Taxable")]
        public bool Taxable { get; set; }
    }

    // Company Info Models
    public class CompanyInfoResponse
    {
        [JsonProperty("CompanyInfo")]
        public CompanyInfo? CompanyInfo { get; set; }
    }

    public class CompanyInfo
    {
        [JsonProperty("Id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("CompanyName")]
        public string CompanyName { get; set; } = string.Empty;

        [JsonProperty("LegalName")]
        public string? LegalName { get; set; }

        [JsonProperty("CompanyAddr")]
        public Address? CompanyAddr { get; set; }

        [JsonProperty("LegalAddr")]
        public Address? LegalAddr { get; set; }
    }

    // Customer List Models
    public class CustomerQueryResponse
    {
        [JsonProperty("QueryResponse")]
        public CustomerQueryData? QueryResponse { get; set; }
    }

    public class CustomerQueryData
    {
        [JsonProperty("Customer")]
        public List<CustomerListItem>? Customer { get; set; }
    }

    public class CustomerListItem
    {
        [JsonProperty("Id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("DisplayName")]
        public string DisplayName { get; set; } = string.Empty;

        [JsonProperty("CompanyName")]
        public string? CompanyName { get; set; }

        [JsonProperty("GivenName")]
        public string? GivenName { get; set; }

        [JsonProperty("FamilyName")]
        public string? FamilyName { get; set; }
    }

    // Customer Detail Models
    public class CustomerSingleResponse
    {
        [JsonProperty("Customer")]
        public CustomerDetail? Customer { get; set; }
    }

    public class CustomerDetail
    {
        [JsonProperty("Id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("DisplayName")]
        public string DisplayName { get; set; } = string.Empty;

        [JsonProperty("CompanyName")]
        public string? CompanyName { get; set; }

        [JsonProperty("BillAddr")]
        public Address? BillAddr { get; set; }

        [JsonProperty("ShipAddr")]
        public Address? ShipAddr { get; set; }

        [JsonProperty("Active")]
        public bool Active { get; set; }
    }
}
