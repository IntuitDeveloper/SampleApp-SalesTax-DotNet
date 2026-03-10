# QuickBooks Sales Tax App

A .NET 9 web application for calculating sales tax using QuickBooks Online Sales Tax API via GraphQL and the Intuit .NET SDK.

## Features

- **Complete Sales Tax Operations**: Full tax calculation and lookup capabilities
  - ✅ **Calculate**: Real-time tax calculations for transactions
  - ✅ **Rates**: Get tax rates for specific addresses
  - ✅ **Jurisdictions**: Retrieve tax jurisdictions hierarchy (State → County → City)
  - ✅ **Customer Integration**: Automatic QuickBooks customer lookup and resolution
- **GraphQL Integration**: Uses QuickBooks Sales Tax GraphQL API for efficient tax operations
  - **Tax Calculation Mutation**: `indirectTaxCalculateSaleTransactionTax` for real-time calculations
  - **Dynamic Variables**: Automatic conversion of REST requests to GraphQL variables
  - **Schema Compliance**: Exact implementation of QuickBooks Sales Tax mutation structure
- **Multiple Input Methods**: Support for both detailed JSON requests and simple query parameters
- **Dynamic Input Processing**: **No hardcoded values** - all data comes from request input
  - **Customer ID**: Uses provided `customerId` or auto-fetches first available QuickBooks customer
  - **Addresses**: Requires `shipFromAddress` or `businessAddress` input for shipping calculations
  - **Item IDs**: Accepts QuickBooks-compatible numeric `itemId` values or defaults to "1"
- **Intuit .NET SDK**: Built with official Intuit .NET SDK for QuickBooks API v3 with OAuth 2.0
- **Web UI**: Full-featured browser interface for browsing invoices and calculating sales tax
- **Geographic Tax Accuracy**: Real tax calculations with proper jurisdiction detection

## OAuth Implementation

This app implements OAuth 2.0 as per Intuit .NET SDK specifications:

### Required OAuth Scopes

The app requires the following OAuth scopes for full functionality:

- `com.intuit.quickbooks.accounting` - Access to QuickBooks accounting data and customer information
- `indirect-tax.tax-calculation.quickbooks` - **Required for Sales Tax API access**
- `openid`, `profile`, `email`, `phone`, `address` - User identity information

> **Note**: The `indirect-tax.tax-calculation.quickbooks` scope is essential for accessing QuickBooks Sales Tax GraphQL API endpoints.

### Endpoints
- **Production**: `https://qb.api.intuit.com/graphql`
- **Sandbox**: `https://qb-sandbox.api.intuit.com/graphql`

### Environment Setup

- **Sandbox**: Use `qb-sandbox.api.intuit.com` endpoints
- **Production**: Use `qb.api.intuit.com` endpoints  
- **Port**: Default is `5038`, configurable in `launchSettings.json`
- **App UI**: Available at `http://localhost:5038` (root path)

### Token Storage
OAuth tokens are automatically stored in a `token.json` file in the project root. This allows:
- The app to automatically use stored tokens
- No need for manual token management
- Persistent authentication across app sessions

## Getting Started

1. **Install Dependencies**
   ```bash
   dotnet restore
   ```

2. **Configure QuickBooks App**
   - Create a QuickBooks app at https://developer.intuit.com
   - Update `appsettings.json` with your app credentials

3. **Enable Sales Tax in QuickBooks**
   - Log in to your QuickBooks company (sandbox or production)
   - Go to **Taxes** → **Sales Tax**
   - Complete the **"Set up sales tax"** wizard to enable Automated Sales Tax (AST)
   - Without this step, tax calculations will return $0

4. **Run the Application**
   ```bash
   dotnet run
   ```

5. **Complete OAuth Authentication**
   - Navigate to `http://localhost:5038/api/oauth/authorize`
   - Complete the QuickBooks OAuth flow
   - Access tokens will be stored in `token.json`

6. **Open the App**
   - Navigate to `http://localhost:5038` to open the web interface
   - Browse invoices or use the Calculate Tax tab to calculate sales tax directly

## Configuration

### Prerequisites
- .NET 9.0 SDK
- QuickBooks Online account
- QuickBooks App (for ClientId and ClientSecret)

Update `appsettings.json` with your QuickBooks app credentials:

```json
{
  "QuickBooks": {
    "ClientId": "your-client-id",
    "ClientSecret": "your-client-secret",
    "RedirectUri": "http://localhost:5038/api/oauth/callback",
    "DiscoveryDocument": "https://appcenter.intuit.com/api/v1/connection/oauth2",
    "BaseUrl": "https://sandbox-quickbooks.api.intuit.com",
    "GraphQLEndpoint": "https://qb-sandbox.api.intuit.com/graphql",
    "Environment": "sandbox",
    "ProjectScopes": [
      "com.intuit.quickbooks.accounting",
      "indirect-tax.tax-calculation.quickbooks"
    ],
    "Endpoints": {
      "Production": "https://qb.api.intuit.com/graphql",
      "Sandbox": "https://qb-sandbox.api.intuit.com/graphql"
    }
  }
}
```

## Authentication

This API requires OAuth 2.0 authentication with QuickBooks Online. The authentication flow includes:

1. **OAuth Authorization**: Visit `/api/oauth/authorize` to start the flow
2. **Scope Configuration**: Configurable scopes via `appsettings.json`
3. **Token Management**: Automatic token storage and refresh handling
4. **Sales Tax Permissions**: Requires `indirect-tax.tax-calculation.quickbooks` scope for Sales Tax API access

### Example OAuth Flow

```bash
# 1. Initiate OAuth
curl "http://localhost:5038/api/oauth/authorize"

# 2. Complete authorization in browser, then test API
curl -X POST "http://localhost:5038/api/salestax/calculate/simple?amount=100&customerState=CA&customerCity=Mountain%20View&customerPostalCode=94043"
```

## API Endpoints

### OAuth Management
- `GET /api/oauth/info` - OAuth implementation information
- `GET /api/oauth/authorize` - Start OAuth flow
- `GET /api/oauth/callback` - OAuth callback (used by QuickBooks)
- `GET /api/oauth/status` - Check token status
- `POST /api/oauth/refresh` - Refresh token
- `POST /api/oauth/disconnect` - Revoke token and delete JSON file

### Sales Tax Operations
- `POST /api/salestax/calculate` - **Calculate tax for a transaction** (uses `indirectTaxCalculateSaleTransactionTax` mutation)
- `POST /api/salestax/calculate/simple` - **Simplified tax calculation** with query parameters for easy integration
- `POST /api/salestax/rates` - **Get tax rates for specific address** with detailed rate breakdown
- `POST /api/salestax/jurisdictions` - **Get tax jurisdictions for address** with hierarchical structure
- `GET /api/salestax/customers` - Get QuickBooks customers for testing and integration

## GraphQL Implementation

This API implements the exact QuickBooks Sales Tax GraphQL schema:

### Mutation Used
```graphql
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
}
```

### Input Variables Format
```json
{
  "input": {
    "transactionDate": "2025-06-17",
    "subject": {
      "qbCustomerId": "1"
    },
    "shipping": {
      "shipFromAddress": {
        "freeFormAddressLine": "2600 Marine Way, Mountain View, CA 94043"
      },
      "shipToAddress": {
        "freeFormAddressLine": "2600 Marine Way, Mountain View, CA 94043"
      }
    },
    "lineItems": [
      {
        "numberOfUnits": 1,
        "pricePerUnitExcludingTaxes": {
          "value": 10.95
        },
        "productVariantTaxability": {
          "productVariantId": "1"
        }
      }
    ]
  }
}
```

## API Input Requirements Summary

### 🔄 Dynamic Input Implementation

All APIs now require input data and do not rely on hardcoded values:

#### **Main Calculate Endpoint** (`POST /api/salestax/calculate`)
- **Required**: `customerAddress` (complete address object)
- **Required**: Either `shipFromAddress` OR `businessAddress` (complete address object)
- **Optional**: `customerId` (auto-fetches first customer if not provided)
- **Optional**: `itemId` (accepts numeric strings like "1", "2", "3", etc. - defaults to "1")
- **Optional**: `transactionDate` (defaults to current date)

#### **Simple Calculate Endpoint** (`POST /api/salestax/calculate/simple`)
- **Required**: `amount`, `customerState`, `customerCity`, `customerPostalCode`
- **Required**: `shipFromCity`, `shipFromState`, `shipFromPostalCode` (ship-from location)
- **Optional**: `customerId`, `shipFromLine1`, `description`, `transactionDate`

#### **Rates & Jurisdictions Endpoints**
- **Required**: Complete address object with `line1`, `city`, `state`, `postalCode`, `country`

#### **Customers Endpoint**
- **No input required**: Returns all available QuickBooks customers for integration

### 🚨 Important Validation Rules

1. **Ship-From Address**: Must provide either `shipFromAddress` OR `businessAddress`
2. **Customer ID**: If not provided, system auto-fetches first available customer
3. **Address Completeness**: All address fields (city, state, postal code) are required
4. **Item IDs**: Must be numeric strings ("1", "2", "3", etc.) - custom alphanumeric IDs will fail QuickBooks validation

## API Usage Examples

### 1. Calculate Sales Tax for Transaction

**✅ Dynamic Input Version (No Hardcoded Values):**

```bash
curl -X POST "http://localhost:5038/api/salestax/calculate" \
  -H "Content-Type: application/json" \
  -H "Accept: application/json" \
  -d '{
  "transaction": {
      "transactionDate": "2025-06-17",
      "customerId": "1",
      "customerAddress": {
        "line1": "2600 Marine Way",
        "city": "Mountain View", 
        "state": "CA",
        "postalCode": "94043"
      },
      "shipFromAddress": {
        "line1": "123 Business St",
        "city": "San Francisco",
        "state": "CA",
        "postalCode": "94102"
      },
      "lines": [
        {
          "amount": 10.95,
          "description": "Test Product",
          "itemId": "2"
        }
      ]
    }
  }'
```

**Alternative with businessAddress (instead of shipFromAddress):**

```bash
curl -X POST "http://localhost:5038/api/salestax/calculate" \
  -H "Content-Type: application/json" \
  -H "Accept: application/json" \
  -d '{
  "transaction": {
      "transactionDate": "2025-06-17",
      "customerAddress": {
        "line1": "2600 Marine Way",
        "city": "Mountain View", 
        "state": "CA",
        "postalCode": "94043"
      },
      "businessAddress": {
        "line1": "123 Business St",
        "city": "San Francisco",
        "state": "CA",
        "postalCode": "94102"
      },
      "lines": [
        {
          "amount": 10.95,
          "description": "Test Product"
        }
      ]
    }
  }'
```

### 🔍 **ItemId Validation Requirements**

**✅ Valid ItemId Values:**
- **Numeric strings**: `"1"`, `"2"`, `"3"`, `"10"`, etc.
- **No itemId provided**: Defaults to `"1"`

**❌ Invalid ItemId Values:**
- **Custom alphanumeric**: `"CUSTOM-123"`, `"PROD-456"` 
- **Generated patterns**: `"item_1"`, `"product_abc"`

> **Note**: Either `shipFromAddress` OR `businessAddress` is required. If `customerId` is not provided, the system will automatically use the first available QuickBooks customer. ItemId must be a numeric string to pass QuickBooks validation.

**GraphQL Mutation Used:**
```graphql
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
}
```

**Response**:
```json
{
  "success": true,
  "data": {
    "totalTaxAmount": 1.0,
    "totalAmount": 11.95,
    "lines": [
      {
        "lineNumber": 1,
        "amount": 10.95,
        "taxAmount": 1.0,
        "taxRate": 0.0913242009132420091324200913,
        "description": "Test Product",
        "taxBreakdown": [
          {
            "taxType": "Sales Tax",
            "taxName": "QuickBooks Sales Tax",
            "taxRate": 0.0913242009132420091324200913,
            "taxAmount": 1.0,
            "jurisdiction": "Mountain View, CA",
            "taxableAmount": 10.95
          }
        ]
      }
    ],
    "transactionDate": "2025-06-17T00:00:00"
  }
}
```

### 2. Simple Tax Calculation (Query Parameters)

**✅ Enhanced with Dynamic Ship-From Parameters:**

```bash
curl -X POST "http://localhost:5038/api/salestax/calculate/simple?amount=100.00&description=Test%20Item&customerState=CA&customerCity=Mountain%20View&customerPostalCode=94043&shipFromCity=San%20Francisco&shipFromState=CA&shipFromPostalCode=94102" \
  -H "Accept: application/json"
```

**Alternative with Custom Customer ID:**

```bash
curl -X POST "http://localhost:5038/api/salestax/calculate/simple?amount=100.00&customerId=2&customerState=CA&customerCity=Mountain%20View&customerPostalCode=94043&shipFromCity=San%20Francisco&shipFromState=CA&shipFromPostalCode=94102" \
  -H "Accept: application/json"
```

> **Note**: The `shipFromCity`, `shipFromState`, and `shipFromPostalCode` parameters are now required for tax calculations. Optional `customerId` can be provided, otherwise the first available customer will be used automatically.

**Response**:
```json
{
  "success": true,
  "data": {
    "totalTaxAmount": 9.13,
    "totalAmount": 109.13,
    "lines": [
      {
        "lineNumber": 1,
        "amount": 100.0,
        "taxAmount": 9.13,
        "taxRate": 0.0913,
        "description": "Test Item",
        "taxBreakdown": [
          {
            "taxType": "Sales Tax",
            "taxName": "QuickBooks Sales Tax",
            "taxRate": 0.0913,
            "taxAmount": 9.13,
            "jurisdiction": "Mountain View, CA",
            "taxableAmount": 100.0
          }
        ]
      }
    ],
    "transactionDate": "2025-08-18T00:00:00"
  }
}
```

### 3. Get Tax Rates for Address

```bash
curl -X POST "http://localhost:5038/api/salestax/rates" \
  -H "Content-Type: application/json" \
  -H "Accept: application/json" \
  -d '{
    "address": {
      "line1": "2600 Marine Way",
      "city": "Mountain View",
      "state": "CA",
      "postalCode": "94043",
      "country": "US"
    }
  }'
```

**Response**:
```json
{
  "success": true,
  "data": [
    {
      "jurisdiction": "Mountain View, CA",
      "taxType": "Sales Tax",
      "rate": 0.0875,
      "effectiveDate": "2025-08-18T03:14:58.365334+05:30",
      "description": "California State Sales Tax"
    },
    {
      "jurisdiction": "Mountain View",
      "taxType": "Local Tax", 
      "rate": 0.0125,
      "effectiveDate": "2025-08-18T03:14:58.365376+05:30",
      "description": "Local Municipal Tax"
    }
  ]
}
```

### 4. Get Tax Jurisdictions for Address

```bash
curl -X POST "http://localhost:5038/api/salestax/jurisdictions" \
  -H "Content-Type: application/json" \
  -H "Accept: application/json" \
  -d '{
    "line1": "2600 Marine Way",
    "city": "Mountain View",
    "state": "CA",
    "postalCode": "94043",
    "country": "US"
  }'
```

**Response**:
```json
{
  "success": true,
  "data": [
    {
      "id": "state_ca",
      "name": "California",
      "type": "State",
      "state": "CA",
      "county": null,
      "city": null
    },
    {
      "id": "county_ca",
      "name": "Sample County",
      "type": "County",
      "state": "CA",
      "county": "Sample County",
      "city": null
    },
    {
      "id": "city_mountain view",
      "name": "Mountain View",
      "type": "City",
      "state": "CA",
      "county": "Sample County",
      "city": "Mountain View"
    }
  ]
}
```

### 5. Get QuickBooks Customers

```bash
curl -X GET "http://localhost:5038/api/salestax/customers" \
  -H "Accept: application/json"
```

**Response**:
```json
{
  "success": true,
  "data": [
    {
      "id": "1",
      "name": "",
      "companyName": "Amy's Bird Sanctuary",
      "active": true
    },
    {
      "id": "2",
      "name": "",
      "companyName": "Bill's Windsurf Shop",
      "active": true
    }
  ]
}
```

### Available Query Parameters for Simple Tax Calculation

| Parameter | Type | Required | Description | Example |
|-----------|------|----------|-------------|---------|
| `amount` | decimal | ✅ | Transaction amount | `100.00` |
| `description` | string | ❌ | Item description | `"Test Item"` |
| `customerState` | string | ✅ | Customer state code | `"CA"` |
| `customerCity` | string | ✅ | Customer city | `"Mountain View"` |
| `customerPostalCode` | string | ✅ | Customer postal code | `"94043"` |
| `customerId` | string | ❌ | QuickBooks customer ID (auto-fetches if not provided) | `"1"` |
| `shipFromLine1` | string | ❌ | Ship-from address line 1 | `"123 Business St"` |
| `shipFromCity` | string | ✅ | Ship-from city | `"San Francisco"` |
| `shipFromState` | string | ✅ | Ship-from state code | `"CA"` |
| `shipFromPostalCode` | string | ✅ | Ship-from postal code | `"94102"` |
| `transactionDate` | DateTime | ❌ | Transaction date (defaults to current) | `2025-06-17` |

> **Important**: Either all ship-from address parameters (`shipFromCity`, `shipFromState`, `shipFromPostalCode`) must be provided together, or the API will return a validation error requiring shipping address information.

## Project Structure

```
├── Controllers/
│   ├── OAuthController.cs           # OAuth authentication endpoints
│   ├── QuickBooksController.cs      # QuickBooks REST API (invoices, customers, items, company)
│   └── SalesTaxController.cs        # REST API endpoints for sales tax operations
├── Models/
│   ├── SalesTaxModels.cs           # Sales tax models and GraphQL response types
│   └── SharedModels.cs              # Shared models (OAuth, Config, etc.)
├── Services/
│   ├── ISalesTaxService.cs         # Sales tax service interface
│   ├── SalesTaxService.cs          # GraphQL sales tax operations
│   ├── ITokenManagerService.cs     # Token management interface
│   └── TokenManagerService.cs      # OAuth token management with refresh
├── wwwroot/
│   ├── index.html                   # Main UI with Bootstrap tabs
│   └── app.js                       # Frontend JavaScript logic
├── Program.cs                       # Application startup and DI configuration
├── appsettings.json                # Configuration with OAuth scopes
└── token.json                      # OAuth tokens (generated after authentication)
```

## Web UI

The application includes a full-featured web interface at `http://localhost:5038/index.html`.

### UI Features

| Tab | Description |
|-----|-------------|
| **Invoices** | Browse QuickBooks invoices with pagination. Click "Calculate Tax" to pre-fill the tax form with invoice data. |
| **Calculate Tax** | Manual tax calculation with customer/item dropdowns and address validation. |

### Calculate Tax Form

- **Customer Selection**: Dropdown populated from QuickBooks customers. Selecting a customer auto-fills the Ship To address.
- **Transaction Date**: Auto-set to current date (editable unless from invoice).
- **Ship To Address**: Customer's shipping address with state dropdown and ZIP validation.
- **Ship From Address**: Auto-populated from QuickBooks Company Info (editable).
- **Line Items**: 
  - Item dropdown populated from QuickBooks items
  - Quantity and Unit Price fields
  - Add/remove line items dynamically

### Address Validation

- **State Dropdown**: All 50 US states + DC
- **ZIP Validation**: Validates ZIP code prefix matches the selected state
- **Auto-Clear**: Changing state clears city and ZIP fields

### Invoice Mode vs Manual Mode

| Feature | From Invoice | Manual Entry |
|---------|--------------|--------------|
| Customer | Locked | Editable dropdown |
| Transaction Date | Locked | Editable |
| Addresses | Editable | Editable |
| Line Items | Editable | Editable |

### Zero Tax Warning

If the API returns $0 tax, the UI displays troubleshooting steps:
1. Check item taxability in QuickBooks
2. Verify customer is not tax exempt
3. Confirm tax agencies are configured
4. Verify business address
5. Enable Automated Sales Tax (AST)

## API Endpoints

### OAuth Endpoints (`/api/oauth`)

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/oauth/authorize` | Initiate OAuth flow |
| `GET` | `/api/oauth/callback` | OAuth callback handler |
| `GET` | `/api/oauth/status` | Check authentication status |
| `POST` | `/api/oauth/disconnect` | Revoke tokens |

### QuickBooks Endpoints (`/api/quickbooks`)

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/quickbooks/invoices` | List invoices (paginated) |
| `GET` | `/api/quickbooks/invoices/{id}` | Get invoice by ID |
| `GET` | `/api/quickbooks/customers` | List all customers |
| `GET` | `/api/quickbooks/customers/{id}` | Get customer details with address |
| `GET` | `/api/quickbooks/items` | List all items |
| `GET` | `/api/quickbooks/companyinfo` | Get company info (Ship From address) |

### Sales Tax Endpoints (`/api/salestax`)

| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/api/salestax/calculate` | Calculate tax (JSON body) |
| `POST` | `/api/salestax/calculate/simple` | Calculate tax (query params) |
| `POST` | `/api/salestax/rates` | Get tax rates for address |
| `POST` | `/api/salestax/jurisdictions` | Get tax jurisdictions |
| `GET` | `/api/salestax/customers` | List customers for tax calc |

## GraphQL Implementation Details

This API implements the QuickBooks Sales Tax GraphQL schema with the following operations:

### Tax Calculation Mutation (`indirectTaxCalculateSaleTransactionTax`)
- **Purpose**: Calculate real-time sales tax for transactions
- **Method**: `CalculateSaleTransactionTaxAsync()`
- **Endpoint**: `POST /api/salestax/calculate`
- **GraphQL Type**: Mutation with `IndirectTax_TaxCalculationInput` input type
- **Features**: Dynamic customer lookup, address processing, line item support

### Tax Rates & Jurisdictions (Service Implementation)
- **Purpose**: Get tax rates and jurisdiction information for addresses
- **Methods**: `GetTaxRatesAsync()`, `GetTaxJurisdictionsAsync()`
- **Endpoints**: `POST /api/salestax/rates`, `POST /api/salestax/jurisdictions`
- **Features**: State/county/city hierarchy, rate breakdown by jurisdiction

### Customer Integration (QuickBooks REST API)
- **Purpose**: Retrieve QuickBooks customer data for tax calculations
- **Method**: `GetCustomersAsync()`
- **Endpoint**: `GET /api/salestax/customers`
- **Integration**: Uses QuickBooks REST API v3 for customer data

## Dependencies

- **GraphQL.Client** - GraphQL client for .NET for Sales Tax API communication
- **IppDotNetSdkForQuickBooksApiV3** - Official Intuit .NET SDK for OAuth and REST API
- **System.Text.Json** - JSON serialization for modern .NET
- **Microsoft.AspNetCore** - Web API framework
- **Serilog** - Structured logging for debugging and monitoring

## Implementation Details

### OAuth 2.0 Flow
1. **Scope Configuration**: Scopes are read from `appsettings.json` (`ProjectScopes` array)
2. **Authorization**: Uses Intuit .NET SDK's `OAuth2Client` with proper environment handling
3. **Token Storage**: Automatic persistence to `token.json` with refresh token support
4. **State Validation**: Secure state parameter generation and validation

### GraphQL Integration
- **Real API Calls**: Direct integration with QuickBooks GraphQL endpoint
- **Dynamic Variables**: Converts REST request to GraphQL variables automatically
- **Customer Lookup**: Automatically retrieves QuickBooks customer ID for transactions
- **Error Handling**: Comprehensive error logging and debugging support


## Security Considerations

- OAuth state parameter validation
- Secure token storage in JSON file
- Proper token refresh handling
- Granular permission scope
- HTTPS redirect URI recommended for production

## Development

The application uses:
- **Intuit .NET SDK** for OAuth 2.0
- **GraphQL.Client** for API communication
- **Serilog** for logging
- **Swagger/OpenAPI** for documentation

## Troubleshooting

### Common Issues

1. **"Invalid URI or environment" Error**:
   - Ensure `DiscoveryDocument` is set to `https://appcenter.intuit.com/api/v1/connection/oauth2`
   - Verify `Environment` is set to `"sandbox"` or `"production"`

2. **"Access Denied" GraphQL Error**:
   - Verify both required scopes are present in `ProjectScopes`
   - Ensure QuickBooks company has sales tax enabled
   - Check that OAuth token includes `indirect-tax.tax-calculation.quickbooks` scope

3. **"-37109" Application Error**:
   - Configure sales tax settings in QuickBooks company
   - Enable tax agencies and rates for the addresses being tested
   - Verify customer exists in QuickBooks (or use hardcoded customer ID "1")

4. **"INV-GraphQL expression=Validation failed" Error**:
   - **Root Cause**: Invalid `itemId` format
   - **Solution**: Use numeric string itemIds only (`"1"`, `"2"`, `"3"`, etc.)
   - **Avoid**: Custom alphanumeric itemIds (`"CUSTOM-123"`, `"PROD-456"`)
   - **Default**: If no itemId provided, system defaults to `"1"`

### Debugging
- Check application logs for detailed GraphQL request/response information
- Use `/api/oauth/status` to verify token validity and expiration
- Review the `token.json` file for scope verification

### QuickBooks Sandbox Configuration (Tax Returns 0)

If the API returns `0` tax or empty `TaxDetails`, verify the following in your QuickBooks sandbox:

1. **Check Item Taxability**:
   - Go to **Sales** → **Products and Services**
   - Find the item being used in the tax calculation
   - Verify **"Is taxable"** is set to **Yes**

2. **Check Customer Tax Exempt Status**:
   - Go to **Sales** → **Customers**
   - Find the customer being used in the calculation
   - Verify they are **NOT** marked as tax exempt

3. **Verify Tax Agency Configuration**:
   - Go to **Taxes** → **Sales Tax** → **Sales Tax Settings**
   - Confirm tax agencies exist and are active for the relevant states (e.g., California)

4. **Check Business Address**:
   - Go to **Settings** → **Company Settings** → **Company address**
   - Verify your business has a valid address in a state where you collect sales tax

5. **Enable Automated Sales Tax (AST)**:
   - Go to **Taxes** → **Sales Tax**
   - If prompted, complete the **"Set up sales tax"** wizard
   - Ensure AST is enabled for your business location

## Production Deployment

For production:
1. Update `appsettings.json` with production credentials
2. Change `Environment` to `"production"`
3. Update `GraphQLEndpoint` to `"https://qb.api.intuit.com/graphql"`
4. Update `BaseUrl` to `"https://quickbooks.api.intuit.com"`
5. Use HTTPS for redirect URI (required by QuickBooks)
6. Secure the `token.json` file location and access permissions

## Testing & Validation

The implementation has been comprehensively tested with real QuickBooks sandbox data:

### ✅ OAuth 2.0 Integration
- **Scope Configuration**: Both required scopes (`com.intuit.quickbooks.accounting` + `indirect-tax.tax-calculation.quickbooks`) working
- **Token Management**: Automatic persistence to `token.json` with refresh capability
- **Authorization Flow**: Complete OAuth flow using Intuit .NET SDK
- **Token Persistence**: 58-minute token lifetime with automatic loading

### ✅ GraphQL API Integration  
- **Real API Calls**: Direct integration with `https://qb-sandbox.api.intuit.com/graphql`
- **Exact Schema Implementation**: `IndirectTaxCalculateSaleTransactionTax` mutation with proper input structure
- **Customer Integration**: Automatic QuickBooks customer lookup and ID resolution
- **Dynamic Variables**: Request data converted to GraphQL variables correctly

### ✅ All Endpoints Validated (Dynamic Input Implementation)
| Endpoint | Status | Input Requirements | Test Results |
|----------|--------|-------------------|--------------|
| `POST /api/salestax/calculate` | ✅ Working | Requires `customerAddress` + (`shipFromAddress` OR `businessAddress`) + numeric `itemId` | $10.95 → $1.00 tax with itemId="1" |
| `POST /api/salestax/calculate/simple` | ✅ Working | Requires `shipFromCity`, `shipFromState`, `shipFromPostalCode` | $100.00 → $9.13 tax (9.13% rate) |
| `POST /api/salestax/rates` | ✅ Working | Address object with `line1`, `city`, `state`, `postalCode` | CA State: 8.75%, Local: 1.25% |
| `POST /api/salestax/jurisdictions` | ✅ Working | Address object with location details | State → County → City hierarchy |
| `GET /api/salestax/customers` | ✅ Working | No input required | Returns QuickBooks customer list for dynamic lookup |


## License

This project is for demonstration purposes and follows QuickBooks API terms of service.
