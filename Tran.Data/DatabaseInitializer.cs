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

        // 샘플 데이터는 App.xaml.cs의 CreateSampleData에서 추가
        // (외래 키 제약 조건 순서를 지키기 위해)
    }

    /// <summary>
    /// 데이터베이스 삭제 (테스트용)
    /// </summary>
    public static void Drop(TranDbContext context)
    {
        context.Database.EnsureDeleted();
    }
}
