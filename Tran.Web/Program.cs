using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Tran.Data;
using Tran.Core.Services;
using Tran.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add Blazor Server services
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Authentication & Authorization
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

// SQLite + EF Core (DbContextFactory only - Blazor Server best practice)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=tran.db";

builder.Services.AddDbContextFactory<TranDbContext>(options =>
    options.UseSqlite(connectionString));

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
    var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<TranDbContext>>();
    using var context = await factory.CreateDbContextAsync();
    DatabaseInitializer.Initialize(context);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Security headers
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Append("Permissions-Policy", "camera=(), microphone=(), geolocation=()");
    await next();
});

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapRazorComponents<Tran.Web.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();
