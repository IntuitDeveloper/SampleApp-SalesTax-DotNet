using QuickBooks.SalesTax.API.Models;
using QuickBooks.SalesTax.API.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver();
    });
builder.Services.AddHttpClient();

// Add session support for OAuth state validation
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Configure QuickBooks settings
var quickBooksConfig = new QuickBooksConfig();
builder.Configuration.GetSection("QuickBooks").Bind(quickBooksConfig);
builder.Services.AddSingleton(quickBooksConfig);

// Register services
builder.Services.AddScoped<ISalesTaxService, SalesTaxService>();
builder.Services.AddScoped<ITokenManagerService, TokenManagerService>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { 
        Title = "QuickBooks Sales Tax API", 
        Version = "v1",
        Description = "API for calculating sales tax using QuickBooks GraphQL and Intuit .NET SDK. Use Swagger UI for all interactions - OAuth tokens are automatically stored in JSON file."
    });
});

// Add CORS for Swagger UI
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSwagger", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseDefaultFiles();
app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "QuickBooks Sales Tax API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowSwagger");
app.UseSession();
app.UseAuthorization();
app.MapControllers();

app.Run();
