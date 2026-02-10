using Microsoft.EntityFrameworkCore;

namespace Tran.Data;

/// <summary>
/// DbContext 팩토리 - 연결 문자열 중앙 관리
/// </summary>
public static class DbContextFactory
{
    private static string _connectionString = "Data Source=tran.db";

    public static void Configure(string connectionString)
    {
        _connectionString = connectionString;
    }

    public static TranDbContext Create()
    {
        var options = new DbContextOptionsBuilder<TranDbContext>()
            .UseSqlite(_connectionString)
            .Options;
        return new TranDbContext(options);
    }
}
