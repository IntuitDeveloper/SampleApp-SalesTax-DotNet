// API Base URL
const API_BASE_URL = 'http://localhost:5038/api';

// US States for validation
const US_STATES = [
    { code: 'AL', name: 'Alabama' }, { code: 'AK', name: 'Alaska' }, { code: 'AZ', name: 'Arizona' },
    { code: 'AR', name: 'Arkansas' }, { code: 'CA', name: 'California' }, { code: 'CO', name: 'Colorado' },
    { code: 'CT', name: 'Connecticut' }, { code: 'DE', name: 'Delaware' }, { code: 'FL', name: 'Florida' },
    { code: 'GA', name: 'Georgia' }, { code: 'HI', name: 'Hawaii' }, { code: 'ID', name: 'Idaho' },
    { code: 'IL', name: 'Illinois' }, { code: 'IN', name: 'Indiana' }, { code: 'IA', name: 'Iowa' },
    { code: 'KS', name: 'Kansas' }, { code: 'KY', name: 'Kentucky' }, { code: 'LA', name: 'Louisiana' },
    { code: 'ME', name: 'Maine' }, { code: 'MD', name: 'Maryland' }, { code: 'MA', name: 'Massachusetts' },
    { code: 'MI', name: 'Michigan' }, { code: 'MN', name: 'Minnesota' }, { code: 'MS', name: 'Mississippi' },
    { code: 'MO', name: 'Missouri' }, { code: 'MT', name: 'Montana' }, { code: 'NE', name: 'Nebraska' },
    { code: 'NV', name: 'Nevada' }, { code: 'NH', name: 'New Hampshire' }, { code: 'NJ', name: 'New Jersey' },
    { code: 'NM', name: 'New Mexico' }, { code: 'NY', name: 'New York' }, { code: 'NC', name: 'North Carolina' },
    { code: 'ND', name: 'North Dakota' }, { code: 'OH', name: 'Ohio' }, { code: 'OK', name: 'Oklahoma' },
    { code: 'OR', name: 'Oregon' }, { code: 'PA', name: 'Pennsylvania' }, { code: 'RI', name: 'Rhode Island' },
    { code: 'SC', name: 'South Carolina' }, { code: 'SD', name: 'South Dakota' }, { code: 'TN', name: 'Tennessee' },
    { code: 'TX', name: 'Texas' }, { code: 'UT', name: 'Utah' }, { code: 'VT', name: 'Vermont' },
    { code: 'VA', name: 'Virginia' }, { code: 'WA', name: 'Washington' }, { code: 'WV', name: 'West Virginia' },
    { code: 'WI', name: 'Wisconsin' }, { code: 'WY', name: 'Wyoming' }, { code: 'DC', name: 'District of Columbia' }
];

// State to ZIP code ranges (first 3 digits)
const STATE_ZIP_RANGES = {
    'AL': [[350, 369]], 'AK': [[995, 999]], 'AZ': [[850, 865]], 'AR': [[716, 729]],
    'CA': [[900, 961]], 'CO': [[800, 816]], 'CT': [[60, 69]], 'DE': [[197, 199]],
    'FL': [[320, 349]], 'GA': [[300, 319], [398, 399]], 'HI': [[967, 968]], 'ID': [[832, 838]],
    'IL': [[600, 629]], 'IN': [[460, 479]], 'IA': [[500, 528]], 'KS': [[660, 679]],
    'KY': [[400, 427]], 'LA': [[700, 714]], 'ME': [[39, 49]], 'MD': [[206, 219]],
    'MA': [[10, 27], [55, 55]], 'MI': [[480, 499]], 'MN': [[550, 567]], 'MS': [[386, 397]],
    'MO': [[630, 658]], 'MT': [[590, 599]], 'NE': [[680, 693]], 'NV': [[889, 898]],
    'NH': [[30, 38]], 'NJ': [[70, 89]], 'NM': [[870, 884]], 'NY': [[100, 149]],
    'NC': [[270, 289]], 'ND': [[580, 588]], 'OH': [[430, 459]], 'OK': [[730, 749]],
    'OR': [[970, 979]], 'PA': [[150, 196]], 'RI': [[28, 29]], 'SC': [[290, 299]],
    'SD': [[570, 577]], 'TN': [[370, 385]], 'TX': [[750, 799], [885, 885]], 'UT': [[840, 847]],
    'VT': [[50, 59]], 'VA': [[220, 246]], 'WA': [[980, 994]], 'WV': [[247, 268]],
    'WI': [[530, 549]], 'WY': [[820, 831]], 'DC': [[200, 205]]
};

// Global state
let currentToken = null;
let invoices = [];
let lineItemCounter = 0;
let customers = [];
let items = [];
let companyInfo = null;
let isFromInvoice = false;

// Pagination state
let currentPage = 1;
let totalPages = 1;
let totalCount = 0;
const pageSize = 10;

// Initialize on page load
document.addEventListener('DOMContentLoaded', function() {
    checkOAuthStatus();
    setupEventListeners();
    setDefaultDate();
});

function setupEventListeners() {
    // Tax calculation form submission
    const taxForm = document.getElementById('taxCalculationForm');
    if (taxForm) {
        taxForm.addEventListener('submit', function(e) {
            e.preventDefault();
            calculateSalesTax();
        });
    }

    // Auto-fetch data when tabs are clicked
    const invoicesTab = document.getElementById('invoices-tab');
    if (invoicesTab) {
        invoicesTab.addEventListener('shown.bs.tab', function() {
            if (invoices.length === 0) {
                fetchInvoices();
            }
        });
    }

    // Fetch customers, items, and company info when Calculate Tax tab is shown
    const calculateTab = document.getElementById('calculate-tab');
    if (calculateTab) {
        calculateTab.addEventListener('shown.bs.tab', function() {
            if (customers.length === 0) {
                fetchCustomersForDropdown();
            }
            if (items.length === 0) {
                fetchItemsForDropdown();
            }
            if (!companyInfo) {
                fetchCompanyInfo();
            }
        });
    }
}

function setDefaultDate() {
    const dateInput = document.getElementById('transactionDate');
    if (dateInput) {
        const today = new Date().toISOString().split('T')[0];
        dateInput.value = today;
    }
    // Populate state dropdowns
    populateStateDropdowns();
}

function populateStateDropdowns() {
    const stateSelects = ['customerState', 'shipFromState'];
    stateSelects.forEach(selectId => {
        const select = document.getElementById(selectId);
        if (select && select.options.length <= 1) {
            US_STATES.forEach(state => {
                const option = document.createElement('option');
                option.value = state.code;
                option.textContent = `${state.code} - ${state.name}`;
                select.appendChild(option);
            });
            // Add change event listener
            select.addEventListener('change', function() {
                onStateChange(selectId);
            });
        }
        // Disable city/zip initially if state has no value
        updateCityZipDisabledState(selectId);
    });
    
    // Add zip validation on blur
    ['customerPostalCode', 'shipFromPostalCode'].forEach(zipId => {
        const zipInput = document.getElementById(zipId);
        if (zipInput) {
            zipInput.addEventListener('blur', function() {
                validateZipForState(zipId);
            });
        }
    });
}

function updateCityZipDisabledState(stateSelectId) {
    const isCustomer = stateSelectId === 'customerState';
    const cityId = isCustomer ? 'customerCity' : 'shipFromCity';
    const zipId = isCustomer ? 'customerPostalCode' : 'shipFromPostalCode';
    const stateCode = document.getElementById(stateSelectId).value;
    const disabled = !stateCode;
    document.getElementById(cityId).disabled = disabled;
    document.getElementById(zipId).disabled = disabled;
}

function onStateChange(stateSelectId) {
    // Determine which address section we're in
    const isCustomer = stateSelectId === 'customerState';
    const cityId = isCustomer ? 'customerCity' : 'shipFromCity';
    const zipId = isCustomer ? 'customerPostalCode' : 'shipFromPostalCode';
    
    // Clear city and zip when state changes
    document.getElementById(cityId).value = '';
    document.getElementById(zipId).value = '';
    document.getElementById(zipId).setCustomValidity('');
    
    // Enable/disable based on whether state is now selected
    updateCityZipDisabledState(stateSelectId);
}

function validateZipForState(zipInputId) {
    const isCustomer = zipInputId === 'customerPostalCode';
    const stateId = isCustomer ? 'customerState' : 'shipFromState';
    
    const stateCode = document.getElementById(stateId).value;
    const zipInput = document.getElementById(zipInputId);
    const zip = zipInput.value.trim();
    
    if (!stateCode || !zip) {
        zipInput.setCustomValidity('');
        return true;
    }
    
    // Get first 3 digits of zip
    const zipPrefix = parseInt(zip.substring(0, 3), 10);
    if (isNaN(zipPrefix)) {
        zipInput.setCustomValidity('Invalid ZIP code format');
        showAlert('Invalid ZIP code format', 'warning');
        return false;
    }
    
    const ranges = STATE_ZIP_RANGES[stateCode];
    if (!ranges) {
        zipInput.setCustomValidity('');
        return true;
    }
    
    // Check if zip prefix falls within any valid range for the state
    const isValid = ranges.some(([min, max]) => zipPrefix >= min && zipPrefix <= max);
    
    if (!isValid) {
        const stateName = US_STATES.find(s => s.code === stateCode)?.name || stateCode;
        zipInput.setCustomValidity(`ZIP code ${zip} is not valid for ${stateName}`);
        showAlert(`ZIP code ${zip} is not valid for ${stateName}`, 'warning');
        return false;
    }
    
    zipInput.setCustomValidity('');
    return true;
}

// ==================== OAuth Functions ====================

async function checkOAuthStatus() {
    try {
        const response = await fetch(`${API_BASE_URL}/oauth/status`);
        const result = await response.json();
        
        if (result.success && result.data.isAuthenticated) {
            updateConnectionStatus(true, result.data);
            showMainContent();
        } else {
            updateConnectionStatus(false);
        }
    } catch (error) {
        console.error('Error checking OAuth status:', error);
        updateConnectionStatus(false);
    }
}

function updateConnectionStatus(isConnected, tokenData = null) {
    const statusElement = document.getElementById('connectionStatus');
    const tokenInfoElement = document.getElementById('tokenInfo');
    const btnConnect = document.getElementById('btnConnect');
    const btnDisconnect = document.getElementById('btnDisconnect');
    const btnRefresh = document.getElementById('btnRefresh');
    
    if (isConnected) {
        statusElement.innerHTML = `
            <span class="status-badge status-connected">
                <i class="bi bi-check-circle"></i> Connected to QuickBooks
            </span>
        `;
        
        if (tokenData) {
            document.getElementById('realmId').textContent = tokenData.realmId || '-';
            document.getElementById('expiresAt').textContent = tokenData.expiresAt ? new Date(tokenData.expiresAt).toLocaleString() : '-';
            document.getElementById('minutesLeft').textContent = tokenData.minutesUntilExpiry || '-';
            tokenInfoElement.style.display = 'block';
        }
        
        btnConnect.style.display = 'none';
        btnDisconnect.style.display = 'inline-block';
        btnRefresh.style.display = 'inline-block';
    } else {
        statusElement.innerHTML = `
            <span class="status-badge status-disconnected">
                <i class="bi bi-x-circle"></i> Not Connected
            </span>
        `;
        tokenInfoElement.style.display = 'none';
        btnConnect.style.display = 'inline-block';
        btnDisconnect.style.display = 'none';
        btnRefresh.style.display = 'none';
        hideMainContent();
    }
}

async function connectToQuickBooks() {
    showLoading();
    try {
        const response = await fetch(`${API_BASE_URL}/oauth/authorize`);
        const result = await response.json();
        
        if (result.success && result.data.authorizationUrl) {
            // Open authorization URL in a new window
            const authWindow = window.open(result.data.authorizationUrl, 'QuickBooks Authorization', 'width=800,height=600');
            
            // Poll for token status
            const pollInterval = setInterval(async () => {
                const statusResponse = await fetch(`${API_BASE_URL}/oauth/status`);
                const statusResult = await statusResponse.json();
                
                if (statusResult.success && statusResult.data.isAuthenticated) {
                    clearInterval(pollInterval);
                    if (authWindow && !authWindow.closed) {
                        authWindow.close();
                    }
                    hideLoading();
                    showAlert('Successfully connected to QuickBooks!', 'success');
                    checkOAuthStatus();
                }
            }, 2000);
            
            // Stop polling after 5 minutes
            setTimeout(() => {
                clearInterval(pollInterval);
                hideLoading();
            }, 300000);
        } else {
            hideLoading();
            showAlert('Failed to initiate OAuth flow', 'danger');
        }
    } catch (error) {
        hideLoading();
        console.error('Error connecting to QuickBooks:', error);
        showAlert('Error connecting to QuickBooks: ' + error.message, 'danger');
    }
}

async function disconnectFromQuickBooks() {
    if (!confirm('Are you sure you want to disconnect from QuickBooks? This will delete the token.json file.')) {
        return;
    }
    
    showLoading();
    try {
        const response = await fetch(`${API_BASE_URL}/oauth/disconnect`, {
            method: 'POST'
        });
        const result = await response.json();
        
        hideLoading();
        if (result.success) {
            showAlert('Successfully disconnected from QuickBooks and deleted token.json', 'success');
            updateConnectionStatus(false);
            hideMainContent();
        } else {
            showAlert('Failed to disconnect: ' + result.errorMessage, 'danger');
        }
    } catch (error) {
        hideLoading();
        console.error('Error disconnecting:', error);
        showAlert('Error disconnecting: ' + error.message, 'danger');
    }
}

async function refreshToken() {
    showLoading();
    try {
        const response = await fetch(`${API_BASE_URL}/oauth/refresh`, {
            method: 'POST'
        });
        const result = await response.json();
        
        hideLoading();
        if (result.success) {
            showAlert('Token refreshed successfully', 'success');
            checkOAuthStatus();
        } else {
            showAlert('Failed to refresh token: ' + result.errorMessage, 'danger');
        }
    } catch (error) {
        hideLoading();
        console.error('Error refreshing token:', error);
        showAlert('Error refreshing token: ' + error.message, 'danger');
    }
}

// ==================== Invoice Functions ====================

async function fetchInvoices(page = 1) {
    showLoading();
    try {
        // Get token first
        const tokenResponse = await fetch(`${API_BASE_URL}/oauth/status`);
        const tokenResult = await tokenResponse.json();
        
        if (!tokenResult.success || !tokenResult.data.isAuthenticated) {
            hideLoading();
            showAlert('Please connect to QuickBooks first', 'warning');
            return;
        }
        
        const realmId = tokenResult.data.realmId;
        
        // Fetch invoices using QuickBooks REST API with pagination
        const invoicesResponse = await fetch(`${API_BASE_URL}/quickbooks/invoices?realmId=${realmId}&page=${page}&pageSize=${pageSize}`);
        
        // If the endpoint doesn't exist, show mock data
        if (invoicesResponse.status === 404) {
            hideLoading();
            showMockInvoices();
            showAlert('Using mock invoice data (REST API endpoint not implemented yet)', 'info');
            return;
        }
        
        const invoicesResult = await invoicesResponse.json();
        
        hideLoading();
        if (invoicesResult.success) {
            // Update pagination state
            currentPage = invoicesResult.data.currentPage;
            totalPages = invoicesResult.data.totalPages;
            totalCount = invoicesResult.data.totalCount;
            invoices = invoicesResult.data.invoices;
            
            displayInvoices(invoices);
        } else {
            showAlert('Failed to fetch invoices: ' + invoicesResult.errorMessage, 'danger');
        }
    } catch (error) {
        hideLoading();
        console.error('Error fetching invoices:', error);
        // Show mock data on error
        showMockInvoices();
        showAlert('Using mock invoice data for demonstration', 'info');
    }
}

function goToPage(page) {
    if (page >= 1 && page <= totalPages) {
        fetchInvoices(page);
        window.scrollTo({ top: 0, behavior: 'smooth' });
    }
}

function showMockInvoices() {
    // Mock invoice data for demonstration
    invoices = [
        {
            id: '145',
            docNumber: 'INV-1034',
            txnDate: '2025-06-17',
            customerRef: { value: '1', name: "Amy's Bird Sanctuary" },
            shipAddr: {
                line1: '2600 Marine Way',
                city: 'Mountain View',
                countrySubDivisionCode: 'CA',
                postalCode: '94043'
            },
            totalAmt: 100.00,
            balance: 100.00,
            line: [
                {
                    id: '1',
                    description: 'Design Services',
                    amount: 85.00,
                    salesItemLineDetail: {
                        qty: 5,
                        unitPrice: 17.00
                    }
                },
                {
                    id: '2',
                    description: 'Consulting',
                    amount: 15.00,
                    salesItemLineDetail: {
                        qty: 1,
                        unitPrice: 15.00
                    }
                }
            ]
        },
        {
            id: '146',
            docNumber: 'INV-1035',
            txnDate: '2025-06-18',
            customerRef: { value: '2', name: "Bill's Windsurf Shop" },
            shipAddr: {
                line1: '123 Ocean Ave',
                city: 'San Francisco',
                countrySubDivisionCode: 'CA',
                postalCode: '94102'
            },
            totalAmt: 250.00,
            balance: 250.00,
            line: [
                {
                    id: '1',
                    description: 'Product Sale',
                    amount: 250.00,
                    salesItemLineDetail: {
                        qty: 10,
                        unitPrice: 25.00
                    }
                }
            ]
        }
    ];
    
    displayInvoices(invoices);
}

function displayInvoices(invoiceList) {
    const container = document.getElementById('invoicesContainer');
    
    if (!invoiceList || invoiceList.length === 0) {
        container.innerHTML = `
            <div class="empty-state">
                <i class="bi bi-inbox"></i>
                <h5>No Invoices Found</h5>
                <p>No invoices available in QuickBooks</p>
            </div>
        `;
        return;
    }
    
    let html = '';
    invoiceList.forEach(invoice => {
        html += `
            <div class="invoice-card">
                <div class="invoice-header">
                    <div>
                        <div class="invoice-number">#${invoice.docNumber}</div>
                        <small class="text-muted">
                            <i class="bi bi-calendar"></i> ${new Date(invoice.txnDate).toLocaleDateString()}
                        </small>
                    </div>
                    <div class="text-end">
                        <div class="invoice-amount">$${invoice.totalAmt.toFixed(2)}</div>
                        <small class="text-muted">Balance: $${invoice.balance.toFixed(2)}</small>
                    </div>
                </div>
                
                <div class="customer-info">
                    <div class="row">
                        <div class="col-md-6">
                            <strong><i class="bi bi-person"></i> Customer:</strong><br>
                            ${invoice.customerRef.name}
                        </div>
                        <div class="col-md-6">
                            <strong><i class="bi bi-geo-alt"></i> Ship To:</strong><br>
                            ${formatAddress(invoice.shipAddr)}
                        </div>
                    </div>
                </div>
                
                <div class="action-buttons">
                    <button class="btn btn-sm btn-primary" onclick="viewInvoiceDetail('${invoice.id}')">
                        <i class="bi bi-eye"></i> View Details
                    </button>
                    <button class="btn btn-sm btn-success" onclick="getRatesForInvoice('${invoice.id}')">
                        <i class="bi bi-percent"></i> View Rates
                    </button>
                    <button class="btn btn-sm btn-info" onclick="getJurisdictionsForInvoice('${invoice.id}')">
                        <i class="bi bi-geo-alt"></i> View Jurisdictions
                    </button>
                    <button class="btn btn-sm btn-warning" onclick="calculateTaxForInvoice('${invoice.id}')">
                        <i class="bi bi-calculator"></i> Calculate Tax
                    </button>
                </div>
            </div>
        `;
    });
    
    // Add pagination controls
    html += renderPagination();
    
    container.innerHTML = html;
}

function renderPagination() {
    if (totalPages <= 1) return '';
    
    let paginationHtml = `
        <nav aria-label="Invoice pagination" class="mt-4">
            <ul class="pagination justify-content-center">
                <li class="page-item ${currentPage === 1 ? 'disabled' : ''}">
                    <a class="page-link" href="#" onclick="goToPage(1); return false;" aria-label="First">
                        <i class="bi bi-chevron-double-left"></i>
                    </a>
                </li>
                <li class="page-item ${currentPage === 1 ? 'disabled' : ''}">
                    <a class="page-link" href="#" onclick="goToPage(${currentPage - 1}); return false;" aria-label="Previous">
                        <i class="bi bi-chevron-left"></i>
                    </a>
                </li>
    `;
    
    // Calculate page range to display
    const maxVisiblePages = 5;
    let startPage = Math.max(1, currentPage - Math.floor(maxVisiblePages / 2));
    let endPage = Math.min(totalPages, startPage + maxVisiblePages - 1);
    
    // Adjust start if we're near the end
    if (endPage - startPage + 1 < maxVisiblePages) {
        startPage = Math.max(1, endPage - maxVisiblePages + 1);
    }
    
    // Add ellipsis at start if needed
    if (startPage > 1) {
        paginationHtml += `
            <li class="page-item">
                <a class="page-link" href="#" onclick="goToPage(1); return false;">1</a>
            </li>
        `;
        if (startPage > 2) {
            paginationHtml += `<li class="page-item disabled"><span class="page-link">...</span></li>`;
        }
    }
    
    // Add page numbers
    for (let i = startPage; i <= endPage; i++) {
        paginationHtml += `
            <li class="page-item ${i === currentPage ? 'active' : ''}">
                <a class="page-link" href="#" onclick="goToPage(${i}); return false;">${i}</a>
            </li>
        `;
    }
    
    // Add ellipsis at end if needed
    if (endPage < totalPages) {
        if (endPage < totalPages - 1) {
            paginationHtml += `<li class="page-item disabled"><span class="page-link">...</span></li>`;
        }
        paginationHtml += `
            <li class="page-item">
                <a class="page-link" href="#" onclick="goToPage(${totalPages}); return false;">${totalPages}</a>
            </li>
        `;
    }
    
    paginationHtml += `
                <li class="page-item ${currentPage === totalPages ? 'disabled' : ''}">
                    <a class="page-link" href="#" onclick="goToPage(${currentPage + 1}); return false;" aria-label="Next">
                        <i class="bi bi-chevron-right"></i>
                    </a>
                </li>
                <li class="page-item ${currentPage === totalPages ? 'disabled' : ''}">
                    <a class="page-link" href="#" onclick="goToPage(${totalPages}); return false;" aria-label="Last">
                        <i class="bi bi-chevron-double-right"></i>
                    </a>
                </li>
            </ul>
            <div class="text-center text-muted">
                <small>Showing page ${currentPage} of ${totalPages} (${totalCount} total invoices)</small>
            </div>
        </nav>
    `;
    
    return paginationHtml;
}

function viewInvoiceDetail(invoiceId) {
    const invoice = invoices.find(inv => inv.id === invoiceId);
    if (!invoice) return;
    
    let lineItemsHtml = '';
    invoice.line.forEach((line, index) => {
        if (line.description) {
            lineItemsHtml += `
                <tr>
                    <td>${index + 1}</td>
                    <td>${line.description}</td>
                    <td>${line.salesItemLineDetail?.qty || 1}</td>
                    <td>$${line.salesItemLineDetail?.unitPrice?.toFixed(2) || '0.00'}</td>
                    <td>$${line.amount.toFixed(2)}</td>
                </tr>
            `;
        }
    });
    
    const content = `
        <div class="row">
            <div class="col-md-6">
                <h5>Invoice Information</h5>
                <table class="table">
                    <tr><th>Invoice Number:</th><td>${invoice.docNumber}</td></tr>
                    <tr><th>Date:</th><td>${new Date(invoice.txnDate).toLocaleDateString()}</td></tr>
                    <tr><th>Customer:</th><td>${invoice.customerRef.name}</td></tr>
                    <tr><th>Customer ID:</th><td>${invoice.customerRef.value}</td></tr>
                </table>
            </div>
            <div class="col-md-6">
                <h5>Shipping Address</h5>
                <address>
                    ${formatAddress(invoice.shipAddr)}
                </address>
                <h5 class="mt-3">Amount Details</h5>
                <table class="table">
                    <tr><th>Total Amount:</th><td class="text-success fw-bold">$${invoice.totalAmt.toFixed(2)}</td></tr>
                    <tr><th>Balance Due:</th><td class="text-danger fw-bold">$${invoice.balance.toFixed(2)}</td></tr>
                </table>
            </div>
        </div>
        
        <h5 class="mt-4">Line Items</h5>
        <table class="table table-striped">
            <thead>
                <tr>
                    <th>#</th>
                    <th>Description</th>
                    <th>Qty</th>
                    <th>Unit Price</th>
                    <th>Amount</th>
                </tr>
            </thead>
            <tbody>
                ${lineItemsHtml}
            </tbody>
        </table>
    `;
    
    document.getElementById('invoiceDetailContent').innerHTML = content;
    new bootstrap.Modal(document.getElementById('invoiceDetailModal')).show();
}

async function getRatesForInvoice(invoiceId) {
    const invoice = invoices.find(inv => inv.id === invoiceId);
    if (!invoice) return;
    
    showLoading();
    try {
        const response = await fetch(`${API_BASE_URL}/salestax/rates`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                address: {
                    line1: invoice.shipAddr?.line1 || '',
                    city: invoice.shipAddr?.city || '',
                    state: invoice.shipAddr?.countrySubDivisionCode || '',
                    postalCode: invoice.shipAddr?.postalCode || '',
                    country: 'US'
                }
            })
        });
        
        const result = await response.json();
        hideLoading();
        
        if (result.success) {
            displayRates(result.data, invoice);
        } else {
            showAlert('Failed to fetch rates: ' + result.errorMessage, 'danger');
        }
    } catch (error) {
        hideLoading();
        console.error('Error fetching rates:', error);
        showAlert('Error fetching rates: ' + error.message, 'danger');
    }
}

function displayRates(rates, invoice) {
    let html = `
        <div class="mb-3">
            <h6>Ship To Address:</h6>
            <p>${formatAddress(invoice.shipAddr)}</p>
        </div>
        <table class="table table-striped">
            <thead>
                <tr>
                    <th>Jurisdiction</th>
                    <th>Tax Type</th>
                    <th>Rate</th>
                    <th>Description</th>
                </tr>
            </thead>
            <tbody>
    `;
    
    rates.forEach(rate => {
        html += `
            <tr>
                <td>${rate.jurisdiction}</td>
                <td><span class="badge bg-primary">${rate.taxType}</span></td>
                <td class="fw-bold">${(rate.rate * 100).toFixed(2)}%</td>
                <td>${rate.description || '-'}</td>
            </tr>
        `;
    });
    
    html += `
            </tbody>
        </table>
        <div class="alert alert-info">
            <strong>Total Combined Rate:</strong> ${(rates.reduce((sum, r) => sum + r.rate, 0) * 100).toFixed(2)}%
        </div>
    `;
    
    document.getElementById('ratesContent').innerHTML = html;
    new bootstrap.Modal(document.getElementById('ratesModal')).show();
}

async function getJurisdictionsForInvoice(invoiceId) {
    const invoice = invoices.find(inv => inv.id === invoiceId);
    if (!invoice) return;
    
    showLoading();
    try {
        const response = await fetch(`${API_BASE_URL}/salestax/jurisdictions`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                line1: invoice.shipAddr?.line1 || '',
                city: invoice.shipAddr?.city || '',
                state: invoice.shipAddr?.countrySubDivisionCode || '',
                postalCode: invoice.shipAddr?.postalCode || '',
                country: 'US'
            })
        });
        
        const result = await response.json();
        hideLoading();
        
        if (result.success) {
            displayJurisdictions(result.data, invoice);
        } else {
            showAlert('Failed to fetch jurisdictions: ' + result.errorMessage, 'danger');
        }
    } catch (error) {
        hideLoading();
        console.error('Error fetching jurisdictions:', error);
        showAlert('Error fetching jurisdictions: ' + error.message, 'danger');
    }
}

function displayJurisdictions(jurisdictions, invoice) {
    let html = `
        <div class="mb-3">
            <h6>Ship To Address:</h6>
            <p>${formatAddress(invoice.shipAddr)}</p>
        </div>
        <div class="row">
    `;
    
    jurisdictions.filter(j => j.type !== 'County').forEach(jurisdiction => {
        const iconMap = {
            'State': 'bi-flag',
            'City': 'bi-geo-alt-fill'
        };
        
        html += `
            <div class="col-md-4 mb-3">
                <div class="card">
                    <div class="card-body text-center">
                        <i class="bi ${iconMap[jurisdiction.type] || 'bi-geo'} fs-1 text-primary"></i>
                        <h5 class="mt-2">${jurisdiction.name}</h5>
                        <span class="badge bg-secondary">${jurisdiction.type}</span>
                        <hr>
                        <small class="text-muted">
                            ${jurisdiction.state ? `State: ${jurisdiction.state}<br>` : ''}
                            ${jurisdiction.city ? `City: ${jurisdiction.city}` : ''}
                        </small>
                    </div>
                </div>
            </div>
        `;
    });
    
    html += '</div>';
    
    document.getElementById('jurisdictionsContent').innerHTML = html;
    new bootstrap.Modal(document.getElementById('jurisdictionsModal')).show();
}

async function calculateTaxForInvoice(invoiceId) {
    const invoice = invoices.find(inv => inv.id === invoiceId);
    if (!invoice) return;
    
    // Mark as from invoice - customer and date will be non-editable
    isFromInvoice = true;
    
    // Ensure customers and items are loaded
    if (customers.length === 0) {
        await fetchCustomersForDropdown();
    }
    if (items.length === 0) {
        await fetchItemsForDropdown();
    }
    
    // Pre-fill transaction date (non-editable when from invoice)
    const transactionDateField = document.getElementById('transactionDate');
    transactionDateField.value = invoice.txnDate;
    transactionDateField.disabled = true;
    
    // Set customer dropdown (non-editable when from invoice)
    const customerSelect = document.getElementById('customerSelect');
    customerSelect.value = invoice.customerRef.value;
    customerSelect.disabled = true;
    
    document.getElementById('customerId').value = invoice.customerRef.value;
    
    // Set customer address (editable)
    const customerLine1Field = document.getElementById('customerLine1');
    customerLine1Field.value = invoice.shipAddr?.line1 || '';
    customerLine1Field.disabled = false;
    
    const customerCityField = document.getElementById('customerCity');
    customerCityField.value = invoice.shipAddr?.city || '';
    customerCityField.disabled = false;
    
    const customerStateField = document.getElementById('customerState');
    customerStateField.value = invoice.shipAddr?.countrySubDivisionCode || '';
    customerStateField.disabled = false;
    updateCityZipDisabledState('customerState');
    
    const customerPostalCodeField = document.getElementById('customerPostalCode');
    customerPostalCodeField.value = invoice.shipAddr?.postalCode || '';
    customerPostalCodeField.disabled = false;
    
    // Set ship from address from company info (editable)
    if (companyInfo) {
        const addr = companyInfo.companyAddr || companyInfo.legalAddr;
        if (addr) {
            document.getElementById('shipFromLine1').value = addr.line1 || '';
            document.getElementById('shipFromCity').value = addr.city || '';
            document.getElementById('shipFromState').value = addr.countrySubDivisionCode || '';
            document.getElementById('shipFromPostalCode').value = addr.postalCode || '';
            updateCityZipDisabledState('shipFromState');
        }
    } else {
        // Fetch company info if not loaded
        fetchCompanyInfo();
    }
    
    // Clear and add line items
    const lineItemsContainer = document.getElementById('lineItemsContainer');
    lineItemsContainer.innerHTML = '';
    lineItemCounter = 0;
    
    invoice.line.forEach((line, index) => {
        if (line.salesItemLineDetail) {
            addLineItem();
            const lineElement = lineItemsContainer.querySelector(`[data-line-index="${lineItemCounter}"]`);
            if (lineElement) {
                const qty = line.salesItemLineDetail?.qty || 1;
                const unitPrice = line.salesItemLineDetail?.unitPrice || line.amount;
                const itemId = line.salesItemLineDetail?.itemRef?.value || '1';
                
                lineElement.querySelector('[name="lineQty[]"]').value = qty;
                lineElement.querySelector('[name="lineUnitPrice[]"]').value = unitPrice;
                lineElement.querySelector('[name="lineItemId[]"]').value = itemId;
                
                // Try to set the item dropdown if we have items
                const itemSelect = lineElement.querySelector('.item-select');
                if (itemSelect && itemId) {
                    itemSelect.value = itemId;
                }
            }
        }
    });
    
    // Switch to calculate tab
    const calculateTabElement = document.getElementById('calculate-tab');
    const calculateTab = new bootstrap.Tab(calculateTabElement);
    calculateTab.show();
    
    showAlert('Invoice data loaded. Customer and date are locked.', 'info');
}

// ==================== Customer & Item Dropdown Functions ====================

async function fetchCompanyInfo() {
    try {
        const response = await fetch(`${API_BASE_URL}/quickbooks/companyinfo`);
        const result = await response.json();
        
        if (result.success && result.data) {
            companyInfo = result.data;
            populateShipFromAddress();
        }
    } catch (error) {
        console.error('Error fetching company info:', error);
    }
}

function populateShipFromAddress() {
    if (!companyInfo) return;
    
    // Use CompanyAddr or LegalAddr
    const addr = companyInfo.companyAddr || companyInfo.legalAddr;
    if (addr) {
        const shipFromLine1 = document.getElementById('shipFromLine1');
        const shipFromCity = document.getElementById('shipFromCity');
        const shipFromState = document.getElementById('shipFromState');
        const shipFromPostalCode = document.getElementById('shipFromPostalCode');
        
        // Only populate if fields are empty (don't overwrite user edits)
        if (shipFromLine1 && !shipFromLine1.value) {
            shipFromLine1.value = addr.line1 || '';
        }
        if (shipFromCity && !shipFromCity.value) {
            shipFromCity.value = addr.city || '';
        }
        if (shipFromState && !shipFromState.value) {
            shipFromState.value = addr.countrySubDivisionCode || '';
        }
        if (shipFromPostalCode && !shipFromPostalCode.value) {
            shipFromPostalCode.value = addr.postalCode || '';
        }
        updateCityZipDisabledState('shipFromState');
    }
}

async function fetchCustomersForDropdown() {
    try {
        const response = await fetch(`${API_BASE_URL}/quickbooks/customers`);
        const result = await response.json();
        
        if (result.success && result.data) {
            customers = result.data;
            populateCustomerDropdown();
        }
    } catch (error) {
        console.error('Error fetching customers:', error);
    }
}

async function fetchItemsForDropdown() {
    try {
        const tokenResponse = await fetch(`${API_BASE_URL}/oauth/status`);
        const tokenResult = await tokenResponse.json();
        
        if (!tokenResult.success || !tokenResult.data.isAuthenticated) {
            return;
        }
        
        const realmId = tokenResult.data.realmId;
        const response = await fetch(`${API_BASE_URL}/quickbooks/items?realmId=${realmId}`);
        const result = await response.json();
        
        if (result.success && result.data) {
            items = result.data;
            populateItemDropdowns();
        }
    } catch (error) {
        console.error('Error fetching items:', error);
    }
}

function populateCustomerDropdown() {
    const select = document.getElementById('customerSelect');
    if (!select) return;
    
    // Keep the default option
    select.innerHTML = '<option value="">-- Select a Customer --</option>';
    
    customers.forEach(customer => {
        const displayName = customer.displayName || customer.companyName || `Customer ${customer.id}`;
        const option = document.createElement('option');
        option.value = customer.id;
        option.textContent = displayName;
        option.dataset.customer = JSON.stringify(customer);
        select.appendChild(option);
    });
}

function populateItemDropdowns() {
    const selects = document.querySelectorAll('.item-select');
    selects.forEach(select => {
        const currentValue = select.value;
        // Keep the default option
        select.innerHTML = '<option value="">-- Select Item --</option>';
        
        items.forEach(item => {
            const option = document.createElement('option');
            option.value = item.id;
            option.textContent = item.name;
            option.dataset.item = JSON.stringify(item);
            select.appendChild(option);
        });
        
        // Restore selection if it existed
        if (currentValue) {
            select.value = currentValue;
        }
    });
}

async function onCustomerSelect() {
    const select = document.getElementById('customerSelect');
    const customerId = select.value;
    
    document.getElementById('customerId').value = customerId;
    
    if (!customerId) {
        // Clear address fields
        document.getElementById('customerLine1').value = '';
        document.getElementById('customerCity').value = '';
        document.getElementById('customerState').value = '';
        document.getElementById('customerPostalCode').value = '';
        return;
    }
    
    // Get customer details to populate address
    try {
        const tokenResponse = await fetch(`${API_BASE_URL}/oauth/status`);
        const tokenResult = await tokenResponse.json();
        
        if (!tokenResult.success || !tokenResult.data.isAuthenticated) {
            return;
        }
        
        const realmId = tokenResult.data.realmId;
        const response = await fetch(`${API_BASE_URL}/quickbooks/customers/${customerId}?realmId=${realmId}`);
        const result = await response.json();
        
        if (result.success && result.data) {
            const customer = result.data;
            // Use ShipAddr or BillAddr
            const addr = customer.shipAddr || customer.billAddr;
            if (addr) {
                document.getElementById('customerLine1').value = addr.line1 || '';
                document.getElementById('customerCity').value = addr.city || '';
                document.getElementById('customerState').value = addr.countrySubDivisionCode || '';
                document.getElementById('customerPostalCode').value = addr.postalCode || '';
                updateCityZipDisabledState('customerState');
            }
        }
    } catch (error) {
        console.error('Error fetching customer details:', error);
    }
}

function onItemSelect(selectElement, lineIndex) {
    const itemId = selectElement.value;
    const lineItem = document.querySelector(`[data-line-index="${lineIndex}"]`);
    
    if (!lineItem) return;
    
    const itemIdInput = lineItem.querySelector('[name="lineItemId[]"]');
    const unitPriceInput = lineItem.querySelector('[name="lineUnitPrice[]"]');
    
    if (!itemId) {
        itemIdInput.value = '1';
        return;
    }
    
    // Find the item in our items array
    const item = items.find(i => i.id === itemId);
    if (item) {
        itemIdInput.value = item.id;
        if (item.unitPrice) {
            unitPriceInput.value = item.unitPrice;
        }
    }
}

// ==================== Tax Calculation Functions ====================

function addLineItem() {
    const container = document.getElementById('lineItemsContainer');
    lineItemCounter++;
    
    // Build item options HTML
    let itemOptionsHtml = '<option value="">-- Select Item --</option>';
    items.forEach(item => {
        itemOptionsHtml += `<option value="${item.id}" data-item='${JSON.stringify(item)}'>${item.name}</option>`;
    });
    
    const lineItemHtml = `
        <div class="line-item" data-line-index="${lineItemCounter}">
            <div class="row align-items-center">
                <div class="col-md-5">
                    <label class="form-label fw-bold small">Item</label>
                    <select class="form-select form-select-sm item-select" name="lineItemSelect[]" onchange="onItemSelect(this, ${lineItemCounter})">
                        ${itemOptionsHtml}
                    </select>
                    <input type="hidden" name="lineItemId[]" value="1">
                </div>
                <div class="col-md-3">
                    <label class="form-label fw-bold small">Qty</label>
                    <input type="number" class="form-control form-control-sm" name="lineQty[]" value="1" min="1" required>
                </div>
                <div class="col-md-3">
                    <label class="form-label fw-bold small">Unit Price</label>
                    <input type="number" class="form-control form-control-sm" name="lineUnitPrice[]" step="0.01" required>
                </div>
                <div class="col-md-1">
                    <label class="form-label small">&nbsp;</label>
                    <button type="button" class="btn btn-sm btn-danger" onclick="removeLineItem(${lineItemCounter})">
                        <i class="bi bi-trash"></i>
                    </button>
                </div>
            </div>
        </div>
    `;
    
    container.insertAdjacentHTML('beforeend', lineItemHtml);
}

function removeLineItem(index) {
    const lineItem = document.querySelector(`[data-line-index="${index}"]`);
    if (lineItem) {
        lineItem.remove();
    }
}

async function calculateSalesTax() {
    const form = document.getElementById('taxCalculationForm');
    if (!form.checkValidity()) {
        form.reportValidity();
        return;
    }
    
    // Gather form data
    const transactionDate = document.getElementById('transactionDate').value;
    const customerId = document.getElementById('customerId').value || null;
    
    const customerAddress = {
        line1: document.getElementById('customerLine1').value,
        city: document.getElementById('customerCity').value,
        state: document.getElementById('customerState').value,
        postalCode: document.getElementById('customerPostalCode').value,
        country: 'US'
    };
    
    const shipFromAddress = {
        line1: document.getElementById('shipFromLine1').value,
        city: document.getElementById('shipFromCity').value,
        state: document.getElementById('shipFromState').value,
        postalCode: document.getElementById('shipFromPostalCode').value,
        country: 'US'
    };
    
    // Gather line items (qty * unitPrice = amount)
    const quantities = document.getElementsByName('lineQty[]');
    const unitPrices = document.getElementsByName('lineUnitPrice[]');
    const itemIds = document.getElementsByName('lineItemId[]');
    
    const lines = [];
    for (let i = 0; i < quantities.length; i++) {
        const qty = parseFloat(quantities[i].value) || 1;
        const unitPrice = parseFloat(unitPrices[i].value) || 0;
        lines.push({
            qty: qty,
            unitPrice: unitPrice,
            amount: qty * unitPrice,
            itemId: itemIds[i].value || '1'
        });
    }
    
    const requestData = {
        transaction: {
            transactionDate: transactionDate,
            customerId: customerId,
            customerAddress: customerAddress,
            shipFromAddress: shipFromAddress,
            lines: lines
        }
    };
    
    showLoading();
    try {
        const response = await fetch(`${API_BASE_URL}/salestax/calculate`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(requestData)
        });
        
        const result = await response.json();
        hideLoading();
        
        if (result.success) {
            displayTaxCalculationResult(result.data);
            
            // Check if tax is zero and show debugging info
            if (result.data.totalTaxAmount === 0) {
                showZeroTaxWarning();
            } else {
                showAlert('Tax calculation completed successfully', 'success');
            }
        } else {
            showAlert('Failed to calculate tax: ' + result.errorMessage, 'danger');
        }
    } catch (error) {
        hideLoading();
        console.error('Error calculating tax:', error);
        showAlert('Error calculating tax: ' + error.message, 'danger');
    }
    
    // Reset isFromInvoice flag after calculation
    isFromInvoice = false;
    document.getElementById('transactionDate').disabled = false;
    document.getElementById('customerSelect').disabled = false;
}

function showZeroTaxWarning() {
    const warningHtml = `
        <div class="alert alert-warning mt-3">
            <h5><i class="bi bi-exclamation-triangle"></i> Tax Returned $0.00</h5>
            <p>If the tax calculation returned zero, please verify the following in your QuickBooks sandbox:</p>
            <ol>
                <li><strong>Check Item Taxability:</strong> Go to Sales → Products and Services → Verify "Is taxable" is set to Yes</li>
                <li><strong>Check Customer Tax Exempt Status:</strong> Go to Sales → Customers → Verify customer is NOT marked as tax exempt</li>
                <li><strong>Verify Tax Agency Configuration:</strong> Go to Taxes → Sales Tax → Sales Tax Settings → Confirm tax agencies are active</li>
                <li><strong>Check Business Address:</strong> Go to Settings → Company Settings → Verify valid address in a sales tax state</li>
                <li><strong>Enable Automated Sales Tax (AST):</strong> Go to Taxes → Sales Tax → Complete "Set up sales tax" wizard</li>
            </ol>
        </div>
    `;
    
    const resultContainer = document.getElementById('taxCalculationResult');
    resultContainer.insertAdjacentHTML('beforeend', warningHtml);
    showAlert('Tax calculation returned $0.00 - See troubleshooting tips below', 'warning');
}

function displayTaxCalculationResult(data) {
    const resultContainer = document.getElementById('taxCalculationResult');
    
    let linesHtml = '';
    data.lines.forEach(line => {
        let breakdownHtml = '';
        if (line.taxBreakdown && line.taxBreakdown.length > 0) {
            breakdownHtml = '<div class="mt-2"><small class="fw-bold">Tax Breakdown:</small><ul class="small mb-0">';
            line.taxBreakdown.forEach(breakdown => {
                breakdownHtml += `
                    <li>
                        ${breakdown.taxName} (${breakdown.jurisdiction}): 
                        ${(breakdown.taxRate * 100).toFixed(4)}% = $${breakdown.taxAmount.toFixed(2)}
                    </li>
                `;
            });
            breakdownHtml += '</ul></div>';
        }
        
        linesHtml += `
            <div class="line-item">
                <div class="row">
                    <div class="col-md-6">
                        <strong>Line ${line.lineNumber}:</strong> ${line.description}
                    </div>
                    <div class="col-md-2 text-end">
                        <strong>Amount:</strong><br>$${line.amount.toFixed(2)}
                    </div>
                    <div class="col-md-2 text-end">
                        <strong>Tax Rate:</strong><br>${(line.taxRate * 100).toFixed(4)}%
                    </div>
                    <div class="col-md-2 text-end">
                        <strong>Tax:</strong><br>$${line.taxAmount.toFixed(2)}
                    </div>
                </div>
                ${breakdownHtml}
            </div>
        `;
    });
    
    const html = `
        <div class="tax-calculation-result">
            <h4 class="text-success mb-4">
                <i class="bi bi-check-circle-fill"></i> Tax Calculation Results
            </h4>
            
            <div class="row mb-4">
                <div class="col-md-4">
                    <div class="card bg-white">
                        <div class="card-body text-center">
                            <h6 class="text-muted">Transaction Date</h6>
                            <h4>${new Date(data.transactionDate).toLocaleDateString()}</h4>
                        </div>
                    </div>
                </div>
                <div class="col-md-4">
                    <div class="card bg-info text-white">
                        <div class="card-body text-center">
                            <h6>Total Tax Amount</h6>
                            <h3 class="mb-0">$${data.totalTaxAmount.toFixed(2)}</h3>
                        </div>
                    </div>
                </div>
                <div class="col-md-4">
                    <div class="card bg-success text-white">
                        <div class="card-body text-center">
                            <h6>Total Amount (with Tax)</h6>
                            <h3 class="mb-0">$${data.totalAmount.toFixed(2)}</h3>
                        </div>
                    </div>
                </div>
            </div>
            
            <h5 class="mb-3">Line Items Breakdown</h5>
            ${linesHtml}
        </div>
    `;
    
    resultContainer.innerHTML = html;
    resultContainer.style.display = 'block';
    
    // Scroll to results
    resultContainer.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
}

// ==================== UI Helper Functions ====================

function formatAddress(addr) {
    if (!addr) return 'No address available';
    const parts = [
        addr.line1,
        addr.city,
        addr.countrySubDivisionCode,
        addr.postalCode
    ].filter(p => p && p.trim());
    return parts.length > 0 ? parts.join(', ') : 'No address available';
}

function showMainContent() {
    document.getElementById('mainContent').style.display = 'block';
    // Auto-fetch invoices since it's the default active tab
    if (invoices.length === 0) {
        fetchInvoices();
    }
}

function hideMainContent() {
    document.getElementById('mainContent').style.display = 'none';
}

function showLoading() {
    document.getElementById('loadingOverlay').classList.add('active');
}

function hideLoading() {
    document.getElementById('loadingOverlay').classList.remove('active');
}

function showAlert(message, type = 'info') {
    const alertContainer = document.getElementById('alertContainer');
    const alertId = 'alert-' + Date.now();
    
    const alertHtml = `
        <div class="alert alert-${type} alert-dismissible fade show" role="alert" id="${alertId}">
            <i class="bi bi-${type === 'success' ? 'check-circle' : type === 'danger' ? 'exclamation-triangle' : 'info-circle'}"></i>
            ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        </div>
    `;
    
    alertContainer.insertAdjacentHTML('beforeend', alertHtml);
    
    // Auto-dismiss after 5 seconds
    setTimeout(() => {
        const alert = document.getElementById(alertId);
        if (alert) {
            const bsAlert = bootstrap.Alert.getOrCreateInstance(alert);
            bsAlert.close();
        }
    }, 5000);
}
