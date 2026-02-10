using Microsoft.EntityFrameworkCore;
using Tran.Core.Models;

namespace Tran.Data;

/// <summary>
/// Tran 데이터베이스 컨텍스트
/// 로컬 SQLite 기반 오프라인 우선 설계
/// </summary>
public class TranDbContext : DbContext
{
    public TranDbContext(DbContextOptions<TranDbContext> options)
        : base(options)
    {
    }

    // 엔티티 세트
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentItem> DocumentItems => Set<DocumentItem>();
    public DbSet<DocumentStateLog> DocumentStateLogs => Set<DocumentStateLog>();
    public DbSet<RevisionRequest> RevisionRequests => Set<RevisionRequest>();
    public DbSet<Settlement> Settlements => Set<Settlement>();
    public DbSet<DocumentTemplate> DocumentTemplates => Set<DocumentTemplate>();

    // ERP 확장 엔티티 세트
    public DbSet<Product> Products { get; set; } = null!;
    public DbSet<Order> Orders { get; set; } = null!;
    public DbSet<OrderItem> OrderItems { get; set; } = null!;
    public DbSet<Purchase> Purchases { get; set; } = null!;
    public DbSet<PurchaseItem> PurchaseItems { get; set; } = null!;
    public DbSet<Sale> Sales { get; set; } = null!;
    public DbSet<SaleItem> SaleItems { get; set; } = null!;
    public DbSet<Quotation> Quotations { get; set; } = null!;
    public DbSet<QuotationItem> QuotationItems { get; set; } = null!;
    public DbSet<Inventory> Inventories { get; set; } = null!;
    public DbSet<InventoryTransaction> InventoryTransactions { get; set; } = null!;
    public DbSet<CompanyPrice> CompanyPrices { get; set; } = null!;
    public DbSet<PriceHistory> PriceHistories { get; set; } = null!;
    public DbSet<CompanyProduct> CompanyProducts { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Companies
        modelBuilder.Entity<Company>(entity =>
        {
            entity.HasKey(e => e.CompanyId);
            entity.Property(e => e.CompanyName).IsRequired();
            entity.Property(e => e.BusinessNumber).IsRequired();
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            // BusinessNumber 중복 체크를 위한 인덱스
            entity.HasIndex(e => e.BusinessNumber)
                .HasDatabaseName("idx_companies_business_number")
                .IsUnique();

            // Status는 IsActive에서 파생되므로 DB 매핑 제외
            entity.Ignore(e => e.Status);

            // 활성 거래처 조회를 위한 인덱스
            entity.HasIndex(e => e.IsActive)
                .HasDatabaseName("idx_companies_is_active");
        });

        // Users
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId);
            entity.Property(e => e.UserName).IsRequired();
        });

        // Documents - 가장 중요한 엔티티
        modelBuilder.Entity<Document>(entity =>
        {
            entity.HasKey(e => e.DocumentId);
            entity.Property(e => e.DocumentId).IsRequired();
            entity.Property(e => e.FromCompanyId).IsRequired();
            entity.Property(e => e.ToCompanyId).IsRequired();
            entity.Property(e => e.State).IsRequired();
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");

            // Optimistic Locking: StateVersion을 동시성 토큰으로 설정
            entity.Property(e => e.StateVersion).IsConcurrencyToken();

            // 인덱스 권장
            entity.HasIndex(e => e.State).HasDatabaseName("idx_documents_state");
            entity.HasIndex(e => new { e.FromCompanyId, e.ToCompanyId })
                .HasDatabaseName("idx_documents_company");
        });

        // DocumentItems
        modelBuilder.Entity<DocumentItem>(entity =>
        {
            entity.HasKey(e => e.ItemId);
            entity.Property(e => e.DocumentId).IsRequired();
            entity.Property(e => e.ItemName).IsRequired();
            entity.Property(e => e.Quantity).HasColumnType("decimal(18,2)");
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)");
            entity.Property(e => e.LineAmount).HasColumnType("decimal(18,2)");

            // FK: Document → DocumentItem
            entity.HasOne<Document>()
                .WithMany(d => d.Items)
                .HasForeignKey(e => e.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            // ExtraDataJson - SQLite는 TEXT 타입 자동 사용
            entity.Property(e => e.ExtraDataJson);
        });

        // DocumentStateLogs - 분쟁 시 최종 증빙
        modelBuilder.Entity<DocumentStateLog>(entity =>
        {
            entity.HasKey(e => e.LogId);
            entity.Property(e => e.DocumentId).IsRequired();
            entity.Property(e => e.ChangedBy).IsRequired();

            // FK: Document → DocumentStateLog (Restrict: 감사 로그 보존 필수)
            entity.HasOne<Document>()
                .WithMany()
                .HasForeignKey(e => e.DocumentId)
                .OnDelete(DeleteBehavior.Restrict);

            // 인덱스
            entity.HasIndex(e => e.DocumentId).HasDatabaseName("idx_logs_document");
        });

        // RevisionRequests
        modelBuilder.Entity<RevisionRequest>(entity =>
        {
            entity.HasKey(e => e.RequestId);
            entity.Property(e => e.DocumentId).IsRequired();
            entity.Property(e => e.RequestReason).IsRequired();

            // FK: Document → RevisionRequest (Restrict: 수정 요청 기록 보존)
            entity.HasOne<Document>()
                .WithMany()
                .HasForeignKey(e => e.DocumentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Settlements
        modelBuilder.Entity<Settlement>(entity =>
        {
            entity.HasKey(e => e.SettlementId);
            entity.Property(e => e.CompanyId).IsRequired();
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");
        });

        // DocumentTemplate 설정
        modelBuilder.Entity<DocumentTemplate>(entity =>
        {
            entity.HasKey(e => e.TemplateId);

            entity.HasOne(e => e.Company)
                .WithMany()
                .HasForeignKey(e => e.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.TemplateName)
                .IsRequired()
                .HasMaxLength(200);

            // SchemaJson - SQLite는 TEXT 타입 자동 사용
            entity.Property(e => e.SchemaJson)
                .IsRequired();

            // LayoutJson - SQLite는 TEXT 타입 자동 사용
            entity.Property(e => e.LayoutJson)
                .IsRequired();

            entity.HasIndex(e => new { e.CompanyId, e.TemplateType, e.IsActive })
                .HasDatabaseName("idx_document_templates_company_type_active");
        });

        // ============================
        // ERP 확장 엔티티 설정
        // ============================

        // Product (품목 마스터)
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.ProductId);

            entity.HasIndex(e => e.ProductCode)
                .IsUnique()
                .HasFilter("ProductCode IS NOT NULL")
                .HasDatabaseName("idx_products_product_code");

            entity.HasIndex(e => e.IsActive)
                .HasDatabaseName("idx_products_is_active");
        });

        // Order (발주)
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.OrderId);

            entity.HasOne(e => e.Company)
                .WithMany()
                .HasForeignKey(e => e.CompanyId);

            entity.HasMany(e => e.Items)
                .WithOne(e => e.Order)
                .HasForeignKey(e => e.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.State)
                .HasDatabaseName("idx_orders_state");

            entity.HasIndex(e => e.CompanyId)
                .HasDatabaseName("idx_orders_company");

            entity.Property(e => e.State)
                .HasConversion<int>();
        });

        // OrderItem (발주 품목)
        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.OrderItemId);

            entity.HasOne(e => e.Product)
                .WithMany()
                .HasForeignKey(e => e.ProductId);
        });

        // Purchase (매입)
        modelBuilder.Entity<Purchase>(entity =>
        {
            entity.HasKey(e => e.PurchaseId);

            entity.HasOne(e => e.Order)
                .WithMany()
                .HasForeignKey(e => e.OrderId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Company)
                .WithMany()
                .HasForeignKey(e => e.CompanyId);

            entity.HasMany(e => e.Items)
                .WithOne(e => e.Purchase)
                .HasForeignKey(e => e.PurchaseId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.State)
                .HasDatabaseName("idx_purchases_state");

            entity.HasIndex(e => e.CompanyId)
                .HasDatabaseName("idx_purchases_company");

            entity.Property(e => e.State)
                .HasConversion<int>();
        });

        // PurchaseItem (매입 품목)
        modelBuilder.Entity<PurchaseItem>(entity =>
        {
            entity.HasKey(e => e.PurchaseItemId);

            entity.HasOne(e => e.Product)
                .WithMany()
                .HasForeignKey(e => e.ProductId);
        });

        // Sale (판매)
        modelBuilder.Entity<Sale>(entity =>
        {
            entity.HasKey(e => e.SaleId);

            entity.HasOne(e => e.Company)
                .WithMany()
                .HasForeignKey(e => e.CompanyId);

            entity.HasMany(e => e.Items)
                .WithOne(e => e.Sale)
                .HasForeignKey(e => e.SaleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.State)
                .HasDatabaseName("idx_sales_state");

            entity.HasIndex(e => e.CompanyId)
                .HasDatabaseName("idx_sales_company");

            entity.Property(e => e.State)
                .HasConversion<int>();
        });

        // SaleItem (판매 품목)
        modelBuilder.Entity<SaleItem>(entity =>
        {
            entity.HasKey(e => e.SaleItemId);

            entity.HasOne(e => e.Product)
                .WithMany()
                .HasForeignKey(e => e.ProductId);
        });

        // Quotation (견적)
        modelBuilder.Entity<Quotation>(entity =>
        {
            entity.HasKey(e => e.QuotationId);

            entity.HasOne(e => e.Company)
                .WithMany()
                .HasForeignKey(e => e.CompanyId);

            entity.HasMany(e => e.Items)
                .WithOne(e => e.Quotation)
                .HasForeignKey(e => e.QuotationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.State)
                .HasDatabaseName("idx_quotations_state");

            entity.HasIndex(e => e.CompanyId)
                .HasDatabaseName("idx_quotations_company");

            entity.Property(e => e.State)
                .HasConversion<int>();
        });

        // QuotationItem (견적 품목)
        modelBuilder.Entity<QuotationItem>(entity =>
        {
            entity.HasKey(e => e.QuotationItemId);

            entity.HasOne(e => e.Product)
                .WithMany()
                .HasForeignKey(e => e.ProductId);
        });

        // Inventory (재고)
        modelBuilder.Entity<Inventory>(entity =>
        {
            entity.HasKey(e => e.InventoryId);

            entity.HasOne(e => e.Product)
                .WithMany()
                .HasForeignKey(e => e.ProductId);

            entity.HasIndex(e => e.ProductId)
                .IsUnique()
                .HasDatabaseName("idx_inventories_product");

            entity.Ignore(e => e.AvailableQuantity);
        });

        // InventoryTransaction (재고 이력)
        modelBuilder.Entity<InventoryTransaction>(entity =>
        {
            entity.HasKey(e => e.TransactionId);

            entity.HasOne(e => e.Product)
                .WithMany()
                .HasForeignKey(e => e.ProductId);

            entity.HasIndex(e => e.ProductId)
                .HasDatabaseName("idx_inventory_transactions_product");

            entity.HasIndex(e => e.CreatedAt)
                .HasDatabaseName("idx_inventory_transactions_created_at");

            entity.Property(e => e.Type)
                .HasConversion<int>();
        });

        // CompanyPrice (거래처별 단가)
        modelBuilder.Entity<CompanyPrice>(entity =>
        {
            entity.HasKey(e => e.CompanyPriceId);

            entity.HasOne(e => e.Company)
                .WithMany()
                .HasForeignKey(e => e.CompanyId);

            entity.HasOne(e => e.Product)
                .WithMany()
                .HasForeignKey(e => e.ProductId);

            entity.HasIndex(e => new { e.CompanyId, e.ProductId })
                .IsUnique()
                .HasDatabaseName("idx_company_prices_company_product");
        });

        // PriceHistory (단가 변경 이력)
        modelBuilder.Entity<PriceHistory>(entity =>
        {
            entity.HasKey(e => e.PriceHistoryId);

            entity.HasIndex(e => new { e.CompanyId, e.ProductId })
                .HasDatabaseName("idx_price_histories_company_product");
        });

        // CompanyProduct (거래처별 품목)
        modelBuilder.Entity<CompanyProduct>(entity =>
        {
            entity.HasKey(e => e.CompanyProductId);

            entity.HasOne(e => e.Company)
                .WithMany()
                .HasForeignKey(e => e.CompanyId);

            entity.HasOne(e => e.Product)
                .WithMany()
                .HasForeignKey(e => e.ProductId);

            entity.HasIndex(e => new { e.CompanyId, e.ProductId })
                .IsUnique()
                .HasDatabaseName("idx_company_products_company_product");
        });
    }
}
