using Microsoft.EntityFrameworkCore;
using Tran.Data;
using Tran.Core.Services;
using Tran.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add Blazor Server services
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// SQLite + EF Core (DbContextFactory for Blazor Server)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=tran.db";

builder.Services.AddDbContextFactory<TranDbContext>(options =>
    options.UseSqlite(connectionString));

// Also register TranDbContext directly for scoped services
builder.Services.AddDbContext<TranDbContext>(options =>
    options.UseSqlite(connectionString), ServiceLifetime.Scoped);

// Register application services (Scoped - one per circuit/session)
builder.Services.AddScoped<IOrderService, Tran.Data.Services.OrderService>();
builder.Services.AddScoped<IProductService, Tran.Data.Services.ProductService>();
builder.Services.AddScoped<IPurchaseService, Tran.Data.Services.PurchaseService>();
builder.Services.AddScoped<ISaleService, Tran.Data.Services.SaleService>();
builder.Services.AddScoped<IInventoryService, Tran.Data.Services.InventoryService>();
builder.Services.AddScoped<IQuotationService, Tran.Data.Services.QuotationService>();
builder.Services.AddScoped<IQuotationTemplateService, Tran.Data.Services.QuotationTemplateService>();
builder.Services.AddScoped<IDocumentQueryService, Tran.Data.Services.DocumentQueryService>();
builder.Services.AddScoped<IPricePolicyService, Tran.Data.Services.PricePolicyService>();
builder.Services.AddScoped<ISalesStatisticsService, Tran.Data.Services.SalesStatisticsService>();
builder.Services.AddScoped<IAutoOrderService, Tran.Data.Services.AutoOrderService>();
builder.Services.AddScoped<IDataImportService, Tran.Data.Services.DataImportService>();
builder.Services.AddSingleton<IStateTransitionService, StateTransitionService>();
builder.Services.AddScoped<AppStateService>();

var app = builder.Build();

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<TranDbContext>();
    DatabaseInitializer.Initialize(context);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<Tran.Web.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();
