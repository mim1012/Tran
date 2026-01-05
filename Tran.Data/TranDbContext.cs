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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Companies
        modelBuilder.Entity<Company>(entity =>
        {
            entity.HasKey(e => e.CompanyId);
            entity.Property(e => e.CompanyName).IsRequired();
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

            entity.Property(e => e.ExtraDataJson)
                .HasColumnType("nvarchar(max)");
        });

        // DocumentStateLogs - 분쟁 시 최종 증빙
        modelBuilder.Entity<DocumentStateLog>(entity =>
        {
            entity.HasKey(e => e.LogId);
            entity.Property(e => e.DocumentId).IsRequired();
            entity.Property(e => e.ChangedBy).IsRequired();

            // 인덱스
            entity.HasIndex(e => e.DocumentId).HasDatabaseName("idx_logs_document");
        });

        // RevisionRequests
        modelBuilder.Entity<RevisionRequest>(entity =>
        {
            entity.HasKey(e => e.RequestId);
            entity.Property(e => e.DocumentId).IsRequired();
            entity.Property(e => e.RequestReason).IsRequired();
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

            entity.Property(e => e.SchemaJson)
                .IsRequired()
                .HasColumnType("nvarchar(max)");

            entity.Property(e => e.LayoutJson)
                .IsRequired()
                .HasColumnType("nvarchar(max)");

            entity.HasIndex(e => new { e.CompanyId, e.TemplateType, e.IsActive })
                .HasDatabaseName("idx_document_templates_company_type_active");
        });
    }
}
