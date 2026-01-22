using System.Configuration;
using System.Data;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Tran.Data;
using Tran.Core.Models;

namespace Tran.Desktop;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 데이터베이스 초기화 및 샘플 데이터 생성
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        var options = new DbContextOptionsBuilder<TranDbContext>()
            .UseSqlite("Data Source=tran.db")
            .Options;

        using var context = new TranDbContext(options);

        // 데이터베이스 생성
        DatabaseInitializer.Initialize(context);

        // 샘플 데이터가 없으면 추가
        if (!context.Documents.Any())
        {
            CreateSampleData(context);
        }
    }

    private void CreateSampleData(TranDbContext context)
    {
        // 샘플 회사 생성
        var company1 = new Company
        {
            CompanyId = "COMP001",
            CompanyName = "서울상사",
            BusinessNumber = "123-45-67890",
            Representative = "김철수",
            Contact = "02-1234-5678",
            Address = "서울특별시 강남구 테헤란로 123",
            IsActive = true,
            Status = CompanyStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        var company2 = new Company
        {
            CompanyId = "COMP002",
            CompanyName = "부산무역",
            BusinessNumber = "234-56-78901",
            Representative = "이영희",
            Contact = "051-9876-5432",
            Address = "부산광역시 해운대구 센텀로 456",
            IsActive = true,
            Status = CompanyStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        context.Companies.AddRange(company1, company2);

        // 샘플 사용자
        var user = new User
        {
            UserId = "USER001",
            CompanyId = company1.CompanyId,
            UserName = "관리자",
            Role = UserRole.Admin
        };

        context.Users.Add(user);

        // 샘플 거래명세표 1 - 작성중
        var doc1 = new Document
        {
            DocumentId = "DOC-2026-0001",
            FromCompanyId = company1.CompanyId,
            ToCompanyId = company2.CompanyId,
            State = DocumentState.Draft,
            TotalAmount = 1500000,
            CreatedBy = user.UserId,
            TransactionDate = DateTime.Today
        };

        // 샘플 거래명세표 2 - 전송됨
        var doc2 = new Document
        {
            DocumentId = "DOC-2026-0002",
            FromCompanyId = company1.CompanyId,
            ToCompanyId = company2.CompanyId,
            State = DocumentState.Sent,
            TotalAmount = 2300000,
            CreatedBy = user.UserId,
            TransactionDate = DateTime.Today.AddDays(-1),
            SentAt = DateTime.UtcNow.AddHours(-2)
        };

        // 샘플 거래명세표 3 - 확정됨
        var doc3 = new Document
        {
            DocumentId = "DOC-2026-0003",
            FromCompanyId = company1.CompanyId,
            ToCompanyId = company2.CompanyId,
            State = DocumentState.Confirmed,
            TotalAmount = 5800000,
            CreatedBy = user.UserId,
            TransactionDate = DateTime.Today.AddDays(-5),
            SentAt = DateTime.UtcNow.AddDays(-4),
            ConfirmedAt = DateTime.UtcNow.AddDays(-3)
        };

        context.Documents.AddRange(doc1, doc2, doc3);

        // 샘플 품목 (DOC-2026-0001용)
        var item1 = new DocumentItem
        {
            ItemId = Guid.NewGuid().ToString(),
            DocumentId = doc1.DocumentId,
            ItemName = "노트북",
            Quantity = 10,
            UnitPrice = 150000,
            OptionText = "15인치, 블랙"
        };
        item1.CalculateLineAmount();

        context.DocumentItems.Add(item1);

        // 샘플 템플릿 추가 (Companies를 먼저 추가한 후에)
        var defaultTemplate = new DocumentTemplate
        {
            TemplateId = "TPL-DEFAULT-001",
            CompanyId = company1.CompanyId,
            TemplateName = "기본 거래명세표",
            TemplateType = TemplateType.Statement,
            SchemaJson = @"{
                ""fields"": [
                    {""key"": ""item_name"", ""label"": ""상품명"", ""type"": ""text"", ""required"": true},
                    {""key"": ""quantity"", ""label"": ""수량"", ""type"": ""number"", ""required"": true},
                    {""key"": ""unit_price"", ""label"": ""단가"", ""type"": ""currency"", ""required"": true},
                    {""key"": ""option_text"", ""label"": ""옵션"", ""type"": ""text"", ""required"": false}
                ]
            }",
            LayoutJson = @"{
                ""columns"": [
                    {""field"": ""item_name"", ""width"": ""40%""},
                    {""field"": ""quantity"", ""width"": ""15%""},
                    {""field"": ""unit_price"", ""width"": ""20%""},
                    {""field"": ""option_text"", ""width"": ""25%""}
                ]
            }",
            IsActive = true
        };

        context.DocumentTemplates.Add(defaultTemplate);

        // 변경사항 저장
        context.SaveChanges();
    }
}

