using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Tran.Data;
using Tran.Core.Services;
using Tran.Api.Services;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// ════════════════════════════════════════════
// 1. Database Configuration
// ════════════════════════════════════════════
var dbProvider = builder.Configuration.GetValue<string>("DatabaseProvider") ?? "Sqlite";
var connectionString = dbProvider == "SqlServer"
    ? builder.Configuration.GetConnectionString("DefaultConnection")
    : builder.Configuration.GetConnectionString("SqliteConnection");

builder.Services.AddDbContext<TranDbContext>(options =>
{
    if (dbProvider == "SqlServer")
    {
        options.UseSqlServer(connectionString);
    }
    else
    {
        options.UseSqlite(connectionString);
    }
});

// ════════════════════════════════════════════
// 2. Redis Cache Configuration
// ════════════════════════════════════════════
var redisConnection = builder.Configuration.GetValue<string>("Redis:ConnectionString");
if (!string.IsNullOrEmpty(redisConnection))
{
    try
    {
        var redis = ConnectionMultiplexer.Connect(redisConnection);
        builder.Services.AddSingleton<IConnectionMultiplexer>(redis);
        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnection;
            options.InstanceName = builder.Configuration.GetValue<string>("Redis:InstanceName") ?? "TranERP_";
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Redis connection failed: {ex.Message}. Continuing without cache.");
    }
}

// Cache Service (Redis or InMemory fallback)
if (!string.IsNullOrEmpty(redisConnection))
{
    builder.Services.AddSingleton<ICacheService, RedisCacheService>();
}
else
{
    builder.Services.AddSingleton<ICacheService, InMemoryCacheService>();
}

// ════════════════════════════════════════════
// 3. Azure Blob Storage Configuration
// ════════════════════════════════════════════
builder.Services.AddSingleton<IBlobStorageService, BlobStorageService>();

// ════════════════════════════════════════════
// 4. Application Services
// ════════════════════════════════════════════
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

// ════════════════════════════════════════════
// 5. JWT Authentication
// ════════════════════════════════════════════
var jwtKey = builder.Configuration.GetValue<string>("Jwt:Key") ?? throw new InvalidOperationException("JWT Key not configured");
var jwtIssuer = builder.Configuration.GetValue<string>("Jwt:Issuer") ?? "TranERP";
var jwtAudience = builder.Configuration.GetValue<string>("Jwt:Audience") ?? "TranERP-Client";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

// ════════════════════════════════════════════
// 6. CORS Configuration
// ════════════════════════════════════════════
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() 
    ?? new[] { "http://localhost:5173", "http://localhost:3000" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// ════════════════════════════════════════════
// 7. Controllers & Swagger
// ════════════════════════════════════════════
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Tran ERP API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new()
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new()
    {
        {
            new()
            {
                Reference = new()
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// ════════════════════════════════════════════
// 8. Database Initialization
// ════════════════════════════════════════════
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<TranDbContext>();
    
    if (dbProvider == "Sqlite")
    {
        context.Database.EnsureCreated();
        DatabaseInitializer.Initialize(context);
    }
    else
    {
        // Azure SQL - Apply migrations
        context.Database.Migrate();
    }
}

// ════════════════════════════════════════════
// 9. Middleware Pipeline
// ════════════════════════════════════════════
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Tran ERP API v1"));
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
