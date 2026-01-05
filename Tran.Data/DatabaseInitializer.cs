using Microsoft.EntityFrameworkCore;
using Tran.Core.Models;

namespace Tran.Data;

/// <summary>
/// 데이터베이스 초기화 유틸리티
/// SQLite 데이터베이스를 생성하고 스키마를 구성합니다
/// </summary>
public static class DatabaseInitializer
{
    /// <summary>
    /// 데이터베이스 초기화 (없으면 생성, 마이그레이션 적용)
    /// </summary>
    public static void Initialize(TranDbContext context)
    {
        // 데이터베이스가 없으면 생성하고 스키마 적용
        context.Database.EnsureCreated();

        // 샘플 템플릿 데이터가 없으면 생성
        if (!context.DocumentTemplates.Any())
        {
            var defaultTemplate = new DocumentTemplate
            {
                TemplateId = "TPL-DEFAULT-001",
                CompanyId = "COMP001", // 기존 샘플 회사 ID 사용
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
        }
    }

    /// <summary>
    /// 데이터베이스 삭제 (테스트용)
    /// </summary>
    public static void Drop(TranDbContext context)
    {
        context.Database.EnsureDeleted();
    }
}
