using System.Configuration;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
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

        // 전역 폰트 설정
        var fontFamily = new FontFamily("Segoe UI Variable, Segoe UI, sans-serif");
        TextElement.FontFamilyProperty.OverrideMetadata(
            typeof(TextElement),
            new FrameworkPropertyMetadata(fontFamily));
        TextBlock.FontFamilyProperty.OverrideMetadata(
            typeof(TextBlock),
            new FrameworkPropertyMetadata(fontFamily));

        // 데이터베이스 초기화 및 샘플 데이터 생성
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        using var context = DbContextFactory.Create();

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
        // ════════════════════════════════════════════
        // 의료기기 업종 거래처 10곳
        // ════════════════════════════════════════════

        // 우리 회사 (의료기기 유통)
        var myCompany = new Company
        {
            CompanyId = "COMP001",
            CompanyName = "트란메디컬",
            BusinessNumber = "123-45-67890",
            Representative = "김태윤",
            Contact = "02-555-0100",
            Address = "서울특별시 강남구 테헤란로 123",
            ContactName = "김태윤",
            ContactEmail = "ty.kim@tranmedical.co.kr",
            ContactPhone = "010-1234-5678",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // 판매 거래처 (병원/의원/약국)
        var hospital1 = new Company
        {
            CompanyId = "COMP002",
            CompanyName = "서울대학교병원",
            BusinessNumber = "201-82-00001",
            Representative = "박진호",
            Contact = "02-2072-0000",
            Address = "서울특별시 종로구 대학로 101",
            ContactName = "이수민",
            ContactEmail = "sumin.lee@snuh.org",
            ContactPhone = "010-2345-6789",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var hospital2 = new Company
        {
            CompanyId = "COMP003",
            CompanyName = "세브란스병원",
            BusinessNumber = "201-82-00002",
            Representative = "정현우",
            Contact = "02-2228-0000",
            Address = "서울특별시 서대문구 연세로 50-1",
            ContactName = "박지혜",
            ContactEmail = "jh.park@severance.org",
            ContactPhone = "010-3456-7890",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var hospital3 = new Company
        {
            CompanyId = "COMP004",
            CompanyName = "삼성서울병원",
            BusinessNumber = "201-82-00003",
            Representative = "최동현",
            Contact = "02-3410-0000",
            Address = "서울특별시 강남구 일원로 81",
            ContactName = "김영수",
            ContactEmail = "ys.kim@samsung-hosp.com",
            ContactPhone = "010-4567-8901",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var clinic1 = new Company
        {
            CompanyId = "COMP005",
            CompanyName = "강남미래의원",
            BusinessNumber = "214-91-10001",
            Representative = "이미래",
            Contact = "02-555-2200",
            Address = "서울특별시 강남구 봉은사로 210",
            ContactName = "이미래",
            ContactEmail = "mirae@miraemc.kr",
            ContactPhone = "010-5678-9012",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var clinic2 = new Company
        {
            CompanyId = "COMP006",
            CompanyName = "서초정형외과",
            BusinessNumber = "214-91-10002",
            Representative = "한정훈",
            Contact = "02-588-3300",
            Address = "서울특별시 서초구 서초대로 330",
            ContactName = "한정훈",
            ContactEmail = "jh.han@seochoortho.kr",
            ContactPhone = "010-6789-0123",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var clinic3 = new Company
        {
            CompanyId = "COMP007",
            CompanyName = "연세가정의학과",
            BusinessNumber = "214-91-10003",
            Representative = "윤서영",
            Contact = "02-332-4400",
            Address = "서울특별시 마포구 월드컵북로 50",
            ContactName = "윤서영",
            ContactEmail = "sy.yoon@yonsefm.kr",
            ContactPhone = "010-7890-1234",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // 매입 거래처 (공급업체)
        var supplier1 = new Company
        {
            CompanyId = "COMP008",
            CompanyName = "메디텍코리아",
            BusinessNumber = "119-86-50001",
            Representative = "장호진",
            Contact = "031-777-0100",
            Address = "경기도 성남시 분당구 판교로 256",
            ContactName = "장호진",
            ContactEmail = "hj.jang@meditech.co.kr",
            ContactPhone = "010-8901-2345",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var supplier2 = new Company
        {
            CompanyId = "COMP009",
            CompanyName = "한국의료기기",
            BusinessNumber = "119-86-50002",
            Representative = "오성태",
            Contact = "031-888-0200",
            Address = "경기도 용인시 기흥구 동백중앙로 100",
            ContactName = "오성태",
            ContactEmail = "st.oh@kmeddevice.co.kr",
            ContactPhone = "010-9012-3456",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var supplier3 = new Company
        {
            CompanyId = "COMP010",
            CompanyName = "서울약품",
            BusinessNumber = "119-86-50003",
            Representative = "남기수",
            Contact = "02-666-0300",
            Address = "서울특별시 금천구 가산디지털1로 50",
            ContactName = "남기수",
            ContactEmail = "ks.nam@seoulpharma.co.kr",
            ContactPhone = "010-0123-4567",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        context.Companies.AddRange(myCompany, hospital1, hospital2, hospital3,
            clinic1, clinic2, clinic3, supplier1, supplier2, supplier3);

        // 사용자
        var user = new User
        {
            UserId = "USER001",
            CompanyId = myCompany.CompanyId,
            UserName = "관리자",
            Role = UserRole.Admin
        };
        context.Users.Add(user);

        // 샘플 거래명세표
        var doc1 = new Document
        {
            DocumentId = "DOC-2026-0001",
            FromCompanyId = myCompany.CompanyId,
            ToCompanyId = hospital1.CompanyId,
            State = DocumentState.Draft,
            TotalAmount = 1500000,
            CreatedBy = user.UserId,
            TransactionDate = DateTime.Today
        };

        var doc2 = new Document
        {
            DocumentId = "DOC-2026-0002",
            FromCompanyId = myCompany.CompanyId,
            ToCompanyId = hospital2.CompanyId,
            State = DocumentState.Sent,
            TotalAmount = 2300000,
            CreatedBy = user.UserId,
            TransactionDate = DateTime.Today.AddDays(-1),
            SentAt = DateTime.UtcNow.AddHours(-2)
        };

        var doc3 = new Document
        {
            DocumentId = "DOC-2026-0003",
            FromCompanyId = myCompany.CompanyId,
            ToCompanyId = hospital3.CompanyId,
            State = DocumentState.Confirmed,
            TotalAmount = 5800000,
            CreatedBy = user.UserId,
            TransactionDate = DateTime.Today.AddDays(-5),
            SentAt = DateTime.UtcNow.AddDays(-4),
            ConfirmedAt = DateTime.UtcNow.AddDays(-3)
        };

        context.Documents.AddRange(doc1, doc2, doc3);

        var item1 = new DocumentItem
        {
            ItemId = Guid.NewGuid().ToString(),
            DocumentId = doc1.DocumentId,
            ItemName = "혈압계",
            Quantity = 10,
            UnitPrice = 150000,
            OptionText = "자동 팔뚝형"
        };
        item1.CalculateLineAmount();
        context.DocumentItems.Add(item1);

        // 기본 템플릿
        var defaultTemplate = new DocumentTemplate
        {
            TemplateId = "TPL-DEFAULT-001",
            CompanyId = myCompany.CompanyId,
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

        context.SaveChanges();

        // ERP 샘플 데이터 추가
        DatabaseInitializer.CreateErpSampleData(context);
    }
}
