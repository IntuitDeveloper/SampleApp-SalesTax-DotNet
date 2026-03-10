using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using QuickBooks.SalesTax.API.Models;

namespace QuickBooks.SalesTax.API.Services
{
    public class SalesTaxService : ISalesTaxService
    {
        private readonly QuickBooksConfig _config;
        private readonly IHttpClientFactory _httpClientFactory;

        public SalesTaxService(QuickBooksConfig config, IHttpClientFactory httpClientFactory)
        {
            _config = config;
            _httpClientFactory = httpClientFactory;
        }

        private GraphQLHttpClient GetGraphQLClient(OAuthToken token)
        {
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token.AccessToken}");
            httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            
            var options = new GraphQLHttpClientOptions
            {
                EndPoint = new Uri(_config.GraphQLEndpoint)
            };
            
            var graphQLClient = new GraphQLHttpClient(
                options, 
                new NewtonsoftJsonSerializer(),
                httpClient);
            
            return graphQLClient;
        }

        private async Task<T?> ExecuteMutationAsync<T>(OAuthToken token, GraphQLRequest mutation) where T : class
        {
            // Log GraphQL Request
            Console.WriteLine("\n========== GRAPHQL REQUEST ==========");
            Console.WriteLine($"Query:\n{mutation.Query}");
            Console.WriteLine($"\nVariables:\n{System.Text.Json.JsonSerializer.Serialize(mutation.Variables, new System.Text.Json.JsonSerializerOptions { WriteIndented = true })}");
            Console.WriteLine("======================================\n");
            
            try
            {
                using var client = GetGraphQLClient(token);
                var response = await client.SendQueryAsync<T>(mutation);

                // Log GraphQL Response
                Console.WriteLine("\n========== GRAPHQL RESPONSE ==========");
                Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(response.Data, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
                Console.WriteLine("=======================================\n");

                if (response.Errors?.Any() == true)
                {
                    var errorMessages = string.Join(", ", response.Errors.Select(e => e.Message));
                    Console.WriteLine($"GraphQL Errors: {errorMessages}");
                    throw new Exception($"GraphQL errors: {errorMessages}");
                }

                return response.Data;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n========== GRAPHQL ERROR ==========");
                Console.WriteLine($"Exception: {ex.Message}");
                Console.WriteLine("====================================\n");
                
                throw new Exception($"Error executing GraphQL mutation: {ex.Message}", ex);
            }
        }

        public async Task<TaxCalculation?> CalculateSaleTransactionTaxAsync(OAuthToken token, CalculateSaleTransactionTaxRequest request)
        {
            Console.WriteLine("=== Starting Tax Calculation ===");
            Console.WriteLine($"Token RealmId: {token.RealmId}");
            Console.WriteLine($"Token Environment: {token.Environment}");

            // Use customer ID from request or fetch first available customer
            string customerId;
            if (!string.IsNullOrEmpty(request.Transaction.CustomerId))
            {
                customerId = request.Transaction.CustomerId;
                Console.WriteLine($"Using provided customer ID: {customerId}");
            }
            else
            {
                Console.WriteLine("No customer ID provided, fetching first available customer...");
                var customers = await GetCustomersAsync(token);
                customerId = customers.FirstOrDefault()?.Id ?? throw new Exception("No customers found in QuickBooks. Please provide a customerId in the request.");
                Console.WriteLine($"Using first available customer ID: {customerId}");
            }

            // Use the exact GraphQL query format from QBO documentation
            var mutation = new GraphQLRequest
            {
                Query = @"
                    mutation IndirectTaxCalculateSaleTransactionTax($input: IndirectTax_TaxCalculationInput!) {
                        indirectTaxCalculateSaleTransactionTax(input: $input) {
                            taxCalculation {
                                transactionDate
                                taxTotals {
                                    totalTaxAmountExcludingShipping {
                                        value
                                    }
                                }
                                subject {
                                    customer {
                                        id
                                    }
                                }
                                shipping {
                                    shipToAddress {
                                        rawAddress {
                                            ... on IndirectTax_FreeFormAddress {
                                                freeformAddressLine
                                            }
                                        }
                                    }
                                    shipFromAddress {
                                        rawAddress {
                                            ... on IndirectTax_FreeFormAddress {
                                                freeformAddressLine
                                            }
                                        }
                                    }
                                }
                                lineItems {
                                    edges {
                                        node {
                                            numberOfUnits
                                            pricePerUnitExcludingTaxes {
                                                value
                                            }
                                        }
                                    }
                                    nodes {
                                        productVariantTaxability {
                                            product {
                                                id
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }",
                Variables = new
                {
                    input = new
                    {
                        transactionDate = request.Transaction.TransactionDate.ToString("yyyy-MM-dd"),
                        subject = new
                        {
                            qbCustomerId = customerId
                        },
                        shipping = new
                        {
                            shipFromAddress = request.Transaction.ShipFromAddress != null ? new
                            {
                                freeFormAddressLine = $"{request.Transaction.ShipFromAddress.Line1}, {request.Transaction.ShipFromAddress.City}, {request.Transaction.ShipFromAddress.State} {request.Transaction.ShipFromAddress.PostalCode}"
                            } : (request.Transaction.BusinessAddress != null ? new
                            {
                                freeFormAddressLine = $"{request.Transaction.BusinessAddress.Line1}, {request.Transaction.BusinessAddress.City}, {request.Transaction.BusinessAddress.State} {request.Transaction.BusinessAddress.PostalCode}"
                            } : throw new Exception("Either shipFromAddress or businessAddress must be provided in the request.")),
                            shipToAddress = request.Transaction.CustomerAddress != null ? new
                            {
                                freeFormAddressLine = $"{request.Transaction.CustomerAddress.Line1}, {request.Transaction.CustomerAddress.City}, {request.Transaction.CustomerAddress.State} {request.Transaction.CustomerAddress.PostalCode}"
                            } : throw new Exception("CustomerAddress must be provided in the request.")
                        },
                        lineItems = request.Transaction.Lines.Select((line, index) => 
                        {
                            // Use productVariantId as string per user's working example
                            var itemId = !string.IsNullOrEmpty(line.ItemId) ? line.ItemId : "1";
                            var qty = line.Qty > 0 ? line.Qty : 1;
                            var unitPrice = line.UnitPrice > 0 ? line.UnitPrice : (qty > 0 ? line.Amount / qty : line.Amount);
                            return new
                            {
                                numberOfUnits = (int)qty,
                                pricePerUnitExcludingTaxes = new { value = unitPrice },
                                productVariantTaxability = new
                                {
                                    productVariantId = itemId
                                }
                            };
                        }).ToArray()
                    }
                }
            };

            var response = await ExecuteMutationAsync<IndirectTaxCalculationResponse>(token, mutation);
            
            // Convert the response format to the existing TaxCalculation format
            if (response?.IndirectTaxCalculateSaleTransactionTax?.TaxCalculation != null)
            {
                var calc = response.IndirectTaxCalculateSaleTransactionTax.TaxCalculation;
                return new TaxCalculation
                {
                    TransactionDate = DateTime.Parse(calc.TransactionDate),
                    TotalTaxAmount = calc.TaxTotals?.TotalTaxAmountExcludingShipping?.Value ?? 0,
                    TotalAmount = (calc.LineItems?.Edges?.Sum(e => e.Node?.PricePerUnitExcludingTaxes?.Value ?? 0) ?? 0) + 
                                 (calc.TaxTotals?.TotalTaxAmountExcludingShipping?.Value ?? 0),
                    Lines = calc.LineItems?.Edges?.Select((edge, index) => new TaxCalculationLine
                    {
                        LineNumber = index + 1,
                        Amount = edge.Node?.PricePerUnitExcludingTaxes?.Value ?? 0,
                        TaxAmount = (calc.TaxTotals?.TotalTaxAmountExcludingShipping?.Value ?? 0) / calc.LineItems.Edges.Count, // Distribute tax evenly
                        TaxRate = calc.LineItems?.Edges?.Any() == true ? 
                            (calc.TaxTotals?.TotalTaxAmountExcludingShipping?.Value ?? 0) / (calc.LineItems?.Edges?.Sum(e => e.Node?.PricePerUnitExcludingTaxes?.Value ?? 0) ?? 1) : 0,
                        Description = request.Transaction.Lines.ElementAtOrDefault(index)?.Description ?? "Item",
                        TaxBreakdown = new List<TaxBreakdownItem>
                        {
                            new TaxBreakdownItem
                            {
                                TaxType = "Sales Tax",
                                TaxName = "QuickBooks Sales Tax",
                                TaxRate = calc.LineItems?.Edges?.Any() == true ? 
                                    (calc.TaxTotals?.TotalTaxAmountExcludingShipping?.Value ?? 0) / (calc.LineItems?.Edges?.Sum(e => e.Node?.PricePerUnitExcludingTaxes?.Value ?? 0) ?? 1) : 0,
                                TaxAmount = (calc.TaxTotals?.TotalTaxAmountExcludingShipping?.Value ?? 0) / calc.LineItems.Edges.Count,
                                TaxableAmount = edge.Node?.PricePerUnitExcludingTaxes?.Value ?? 0,
                                Jurisdiction = $"{request.Transaction.CustomerAddress?.City}, {request.Transaction.CustomerAddress?.State}"
                            }
                        }
                    }).ToList() ?? new List<TaxCalculationLine>()
                };
            }
            
            return null;
        }

        public async Task<List<TaxRate>> GetTaxRatesAsync(OAuthToken token, TaxRateQueryRequest request)
        {
            // For now, return a simplified response since the exact schema for tax rates may be different
            // This is a placeholder implementation that returns sample data
            await Task.Delay(100); // Simulate API call
            
            return new List<TaxRate>
            {
                new TaxRate
                {
                    Jurisdiction = $"{request.Address.City}, {request.Address.State}",
                    TaxType = "Sales Tax",
                    Rate = 0.0875m, // Sample CA sales tax rate
                    EffectiveDate = DateTime.Now,
                    Description = "California State Sales Tax"
                },
                new TaxRate
                {
                    Jurisdiction = request.Address.City,
                    TaxType = "Local Tax", 
                    Rate = 0.0125m, // Sample local tax rate
                    EffectiveDate = DateTime.Now,
                    Description = "Local Municipal Tax"
                }
            };
        }

        public async Task<List<TaxJurisdiction>> GetTaxJurisdictionsAsync(OAuthToken token, AddressInput address)
        {
            // For now, return a simplified response since the exact schema for tax jurisdictions may be different
            // This is a placeholder implementation that returns sample data
            await Task.Delay(100); // Simulate API call
            
            return new List<TaxJurisdiction>
            {
                new TaxJurisdiction
                {
                    Id = "state_ca",
                    Name = "California",
                    Type = "State",
                    State = address.State,
                    County = null,
                    City = null
                },
                new TaxJurisdiction
                {
                    Id = $"county_{address.State.ToLower()}",
                    Name = "Sample County",
                    Type = "County",
                    State = address.State,
                    County = "Sample County",
                    City = null
                },
                new TaxJurisdiction
                {
                    Id = $"city_{address.City.ToLower()}",
                    Name = address.City,
                    Type = "City",
                    State = address.State,
                    County = "Sample County", 
                    City = address.City
                }
            };
        }

        public async Task<List<QBCustomer>> GetCustomersAsync(OAuthToken token)
        {
            try
            {
                // Use QuickBooks REST API to get customers
                var baseUri = _config.BaseUrl;
                
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.AccessToken);
                httpClient.DefaultRequestHeaders.Accept.Add(
                    new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                var url = $"{baseUri}/v3/company/{token.RealmId}/query?query=SELECT * FROM Customer maxresults 20";
                
                var response = await httpClient.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var customerResponse = System.Text.Json.JsonSerializer.Deserialize<CustomerQueryResponse>(content);
                    return customerResponse?.QueryResponse?.Customer ?? new List<QBCustomer>();
                }
                else
                {
                    Console.WriteLine($"Failed to get customers: {response.StatusCode} - {content}");
                    return new List<QBCustomer>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting customers: {ex.Message}");
                return new List<QBCustomer>();
            }
        }
    }
}
