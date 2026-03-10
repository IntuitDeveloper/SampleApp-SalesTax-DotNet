using Newtonsoft.Json;

namespace QuickBooks.SalesTax.API.Models
{
    // Tax Calculation Models
    public class CalculateSaleTransactionTaxRequest
    {
        public SaleTransactionInput Transaction { get; set; } = new();
    }

    public class SaleTransactionInput
    {
        [JsonProperty("transactionDate")]
        public DateTime TransactionDate { get; set; }
        
        [JsonProperty("lines")]
        public List<SaleTransactionLineInput> Lines { get; set; } = new();
        
        [JsonProperty("customerAddress")]
        public AddressInput? CustomerAddress { get; set; }
        
        [JsonProperty("businessAddress")]
        public AddressInput? BusinessAddress { get; set; }
        
        [JsonProperty("customerId")]
        public string? CustomerId { get; set; }
        
        [JsonProperty("shipFromAddress")]
        public AddressInput? ShipFromAddress { get; set; }
    }

    public class SaleTransactionLineInput
    {
        [JsonProperty("amount")]
        public decimal Amount { get; set; }
        
        [JsonProperty("qty")]
        public decimal Qty { get; set; } = 1;
        
        [JsonProperty("unitPrice")]
        public decimal UnitPrice { get; set; }
        
        [JsonProperty("description")]
        public string Description { get; set; } = string.Empty;
        
        [JsonProperty("taxCode")]
        public string? TaxCode { get; set; }
        
        [JsonProperty("itemId")]
        public string? ItemId { get; set; }
    }

    public class AddressInput
    {
        [JsonProperty("line1")]
        public string Line1 { get; set; } = string.Empty;
        
        [JsonProperty("city")]
        public string City { get; set; } = string.Empty;
        
        [JsonProperty("state")]
        public string State { get; set; } = string.Empty;
        
        [JsonProperty("postalCode")]
        public string PostalCode { get; set; } = string.Empty;
        
        [JsonProperty("country")]
        public string Country { get; set; } = "US";
    }

    public class CalculateSaleTransactionTaxResponse
    {
        [JsonProperty("taxCalculation")]
        public TaxCalculationResult? TaxCalculation { get; set; }
    }

    public class TaxCalculationResult
    {
        [JsonProperty("calculateSaleTransactionTax")]
        public TaxCalculation? CalculateSaleTransactionTax { get; set; }
        
        [JsonProperty("userErrors")]
        public List<UserError>? UserErrors { get; set; }
    }

    public class TaxCalculation
    {
        [JsonProperty("totalTaxAmount")]
        public decimal TotalTaxAmount { get; set; }
        
        [JsonProperty("totalAmount")]
        public decimal TotalAmount { get; set; }
        
        [JsonProperty("lines")]
        public List<TaxCalculationLine> Lines { get; set; } = new();
        
        [JsonProperty("transactionDate")]
        public DateTime TransactionDate { get; set; }
    }

    public class TaxCalculationLine
    {
        [JsonProperty("lineNumber")]
        public int LineNumber { get; set; }
        
        [JsonProperty("amount")]
        public decimal Amount { get; set; }
        
        [JsonProperty("taxAmount")]
        public decimal TaxAmount { get; set; }
        
        [JsonProperty("taxRate")]
        public decimal TaxRate { get; set; }
        
        [JsonProperty("description")]
        public string Description { get; set; } = string.Empty;
        
        [JsonProperty("taxBreakdown")]
        public List<TaxBreakdownItem> TaxBreakdown { get; set; } = new();
    }

    public class TaxBreakdownItem
    {
        [JsonProperty("taxType")]
        public string TaxType { get; set; } = string.Empty;
        
        [JsonProperty("taxName")]
        public string TaxName { get; set; } = string.Empty;
        
        [JsonProperty("taxRate")]
        public decimal TaxRate { get; set; }
        
        [JsonProperty("taxAmount")]
        public decimal TaxAmount { get; set; }
        
        [JsonProperty("jurisdiction")]
        public string? Jurisdiction { get; set; }
        
        [JsonProperty("taxableAmount")]
        public decimal TaxableAmount { get; set; }
    }

    public class UserError
    {
        [JsonProperty("field")]
        public List<string>? Field { get; set; }
        
        [JsonProperty("message")]
        public string Message { get; set; } = string.Empty;
        
        [JsonProperty("code")]
        public string? Code { get; set; }
    }

    // GraphQL Response Models
    public class GraphQLResponse<T>
    {
        [JsonProperty("data")]
        public T? Data { get; set; }
        
        [JsonProperty("errors")]
        public List<GraphQLError>? Errors { get; set; }
    }

    public class GraphQLError
    {
        [JsonProperty("message")]
        public string Message { get; set; } = string.Empty;
        
        [JsonProperty("locations")]
        public List<GraphQLLocation>? Locations { get; set; }
        
        [JsonProperty("path")]
        public List<object>? Path { get; set; }
    }

    public class GraphQLLocation
    {
        [JsonProperty("line")]
        public int Line { get; set; }
        
        [JsonProperty("column")]
        public int Column { get; set; }
    }

    // Tax Rate Query Models
    public class TaxRateQueryRequest
    {
        public AddressInput Address { get; set; } = new();
        public DateTime? EffectiveDate { get; set; }
    }

    public class TaxRateQueryResponse
    {
        [JsonProperty("taxRates")]
        public List<TaxRate> TaxRates { get; set; } = new();
    }

    public class TaxRate
    {
        [JsonProperty("jurisdiction")]
        public string Jurisdiction { get; set; } = string.Empty;
        
        [JsonProperty("taxType")]
        public string TaxType { get; set; } = string.Empty;
        
        [JsonProperty("rate")]
        public decimal Rate { get; set; }
        
        [JsonProperty("effectiveDate")]
        public DateTime EffectiveDate { get; set; }
        
        [JsonProperty("description")]
        public string? Description { get; set; }
    }

    // Tax Jurisdiction Models
    public class TaxJurisdiction
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;
        
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;
        
        [JsonProperty("type")]
        public string Type { get; set; } = string.Empty;
        
        [JsonProperty("state")]
        public string? State { get; set; }
        
        [JsonProperty("county")]
        public string? County { get; set; }
        
        [JsonProperty("city")]
        public string? City { get; set; }
    }

    // Response models for the exact GraphQL schema provided
    public class IndirectTaxCalculationResponse
    {
        [JsonProperty("indirectTaxCalculateSaleTransactionTax")]
        public IndirectTaxCalculationPayload IndirectTaxCalculateSaleTransactionTax { get; set; } = new IndirectTaxCalculationPayload();
    }

    public class IndirectTaxCalculationPayload
    {
        [JsonProperty("taxCalculation")]
        public IndirectTaxCalculation TaxCalculation { get; set; } = new IndirectTaxCalculation();
    }

    public class IndirectTaxCalculation
    {
        [JsonProperty("transactionDate")]
        public string TransactionDate { get; set; } = string.Empty;
        
        [JsonProperty("taxTotals")]
        public IndirectTaxTotals TaxTotals { get; set; } = new IndirectTaxTotals();
        
        [JsonProperty("subject")]
        public IndirectTaxSubject Subject { get; set; } = new IndirectTaxSubject();
        
        [JsonProperty("shipping")]
        public IndirectTaxShipping Shipping { get; set; } = new IndirectTaxShipping();
        
        [JsonProperty("lineItems")]
        public IndirectTaxLineItems LineItems { get; set; } = new IndirectTaxLineItems();
    }

    public class IndirectTaxTotals
    {
        [JsonProperty("totalTaxAmountExcludingShipping")]
        public ValueContainer TotalTaxAmountExcludingShipping { get; set; } = new ValueContainer();
    }

    public class IndirectTaxSubject
    {
        [JsonProperty("customer")]
        public IndirectTaxCustomer Customer { get; set; } = new IndirectTaxCustomer();
        
        [JsonProperty("qbCustomerId")]
        public string QbCustomerId { get; set; } = string.Empty;
    }

    public class IndirectTaxCustomer
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;
    }

    public class IndirectTaxShipping
    {
        [JsonProperty("shipToAddress")]
        public IndirectTaxAddress ShipToAddress { get; set; } = new IndirectTaxAddress();
        
        [JsonProperty("shipFromAddress")]
        public IndirectTaxAddress ShipFromAddress { get; set; } = new IndirectTaxAddress();
        
        [JsonProperty("taxAmount")]
        public ValueContainer TaxAmount { get; set; } = new ValueContainer();
        
        [JsonProperty("shippingFee")]
        public ValueContainer ShippingFee { get; set; } = new ValueContainer();
    }

    public class IndirectTaxAddress
    {
        [JsonProperty("streetAddressLine1")]
        public string StreetAddressLine1 { get; set; } = string.Empty;
        
        [JsonProperty("rawAddress")]
        public IndirectTaxRawAddress? RawAddress { get; set; }
    }

    public class IndirectTaxRawAddress
    {
        [JsonProperty("freeformAddressLine")]
        public string FreeformAddressLine { get; set; } = string.Empty;
        
        [JsonProperty("freeFormAddressLine")]
        public string FreeFormAddressLine { get; set; } = string.Empty;
    }

    public class IndirectTaxLineItems
    {
        [JsonProperty("edges")]
        public List<IndirectTaxLineItemEdge> Edges { get; set; } = new List<IndirectTaxLineItemEdge>();
        
        [JsonProperty("nodes")]
        public List<IndirectTaxLineItemNode> Nodes { get; set; } = new List<IndirectTaxLineItemNode>();
    }

    public class IndirectTaxLineItemEdge
    {
        [JsonProperty("node")]
        public IndirectTaxLineItemEdgeNode Node { get; set; } = new IndirectTaxLineItemEdgeNode();
    }

    public class IndirectTaxLineItemEdgeNode
    {
        [JsonProperty("numberOfUnits")]
        public int NumberOfUnits { get; set; }
        
        [JsonProperty("pricePerUnitExcludingTaxes")]
        public ValueContainer PricePerUnitExcludingTaxes { get; set; } = new ValueContainer();
    }

    public class IndirectTaxLineItemNode
    {
        [JsonProperty("numberOfUnits")]
        public int NumberOfUnits { get; set; }
        
        [JsonProperty("totalPriceExcludingTaxes")]
        public ValueContainer TotalPriceExcludingTaxes { get; set; } = new ValueContainer();
        
        [JsonProperty("taxAmount")]
        public ValueContainer TaxAmount { get; set; } = new ValueContainer();
        
        [JsonProperty("productVariantTaxability")]
        public IndirectTaxProductVariantTaxability ProductVariantTaxability { get; set; } = new IndirectTaxProductVariantTaxability();
        
        [JsonProperty("taxDetails")]
        public List<IndirectTaxDetail> TaxDetails { get; set; } = new List<IndirectTaxDetail>();
    }
    
    public class IndirectTaxDetail
    {
        [JsonProperty("taxAmount")]
        public ValueContainer TaxAmount { get; set; } = new ValueContainer();
        
        [JsonProperty("taxableAmount")]
        public ValueContainer TaxableAmount { get; set; } = new ValueContainer();
        
        [JsonProperty("taxRate")]
        public IndirectTaxRateInfo TaxRate { get; set; } = new IndirectTaxRateInfo();
    }
    
    public class IndirectTaxRateInfo
    {
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;
    }

    public class IndirectTaxProductVariantTaxability
    {
        [JsonProperty("classificationCode")]
        public string ClassificationCode { get; set; } = string.Empty;
        
        [JsonProperty("product")]
        public IndirectTaxProduct? Product { get; set; }
        
        [JsonProperty("productVariantId")]
        public string ProductVariantId { get; set; } = string.Empty;
    }

    public class IndirectTaxProduct
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;
    }

    public class ValueContainer
    {
        [JsonProperty("value")]
        public decimal Value { get; set; }
    }

    // QuickBooks Customer models
    public class QBCustomer
    {
        [JsonProperty("Id")]
        public string Id { get; set; } = string.Empty;
        
        [JsonProperty("Name")]
        public string Name { get; set; } = string.Empty;
        
        [JsonProperty("CompanyName")]
        public string? CompanyName { get; set; }
        
        [JsonProperty("Active")]
        public bool Active { get; set; } = true;
    }

    public class CustomerQueryResponse
    {
        [JsonProperty("QueryResponse")]
        public CustomerQueryData? QueryResponse { get; set; }
    }

    public class CustomerQueryData
    {
        [JsonProperty("Customer")]
        public List<QBCustomer>? Customer { get; set; }
        
        [JsonProperty("maxResults")]
        public int MaxResults { get; set; }
        
        [JsonProperty("startPosition")]
        public int StartPosition { get; set; }
    }
}
