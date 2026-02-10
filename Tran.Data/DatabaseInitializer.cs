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
        // DB가 없으면 생성, 있으면 그대로 유지
        context.Database.EnsureCreated();

        // 샘플 데이터는 App.xaml.cs의 CreateSampleData에서 추가
        // (외래 키 제약 조건 순서를 지키기 위해)
    }

    /// <summary>
    /// ERP 확장 샘플 데이터 생성 (품목, 재고, 판매, 발주)
    /// App.xaml.cs의 CreateSampleData 이후에 호출해야 합니다.
    /// </summary>
    public static void CreateErpSampleData(TranDbContext context)
    {
        // 이미 ERP 샘플 데이터가 있으면 스킵
        if (context.Products.Any())
            return;

        // ============================
        // 1. 품목 마스터 (Products)
        // ============================
        var product1 = new Product
        {
            ProductName = "의료용 테이프",
            ProductCode = "MED-001",
            Category = "소모품",
            Unit = "개",
            DefaultPrice = 3500m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var product2 = new Product
        {
            ProductName = "거즈",
            ProductCode = "MED-002",
            Category = "소모품",
            Unit = "개",
            DefaultPrice = 12000m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var product3 = new Product
        {
            ProductName = "반창고",
            ProductCode = "MED-003",
            Category = "소모품",
            Unit = "개",
            DefaultPrice = 1500m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var product4 = new Product
        {
            ProductName = "수술용 장갑",
            ProductCode = "MED-004",
            Category = "소모품",
            Unit = "박스",
            DefaultPrice = 25000m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var product5 = new Product
        {
            ProductName = "소독제",
            ProductCode = "MED-005",
            Category = "의약품",
            Unit = "병",
            DefaultPrice = 8000m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        context.Products.AddRange(product1, product2, product3, product4, product5);
        context.SaveChanges(); // Products 먼저 저장하여 PK 생성

        // ============================
        // 2. 재고 (Inventory)
        // ============================
        var inventories = new[]
        {
            new Inventory
            {
                ProductId = product1.ProductId,
                ConfirmedQuantity = 500m,
                PendingInQuantity = 0m,
                PendingOutQuantity = 0m,
                SafetyStock = 100m,
                LastUpdatedAt = DateTime.UtcNow
            },
            new Inventory
            {
                ProductId = product2.ProductId,
                ConfirmedQuantity = 200m,
                PendingInQuantity = 50m,
                PendingOutQuantity = 0m,
                SafetyStock = 50m,
                LastUpdatedAt = DateTime.UtcNow
            },
            new Inventory
            {
                ProductId = product3.ProductId,
                ConfirmedQuantity = 800m,
                PendingInQuantity = 0m,
                PendingOutQuantity = 0m,
                SafetyStock = 200m,
                LastUpdatedAt = DateTime.UtcNow
            },
            new Inventory
            {
                ProductId = product4.ProductId,
                ConfirmedQuantity = 30m,
                PendingInQuantity = 10m,
                PendingOutQuantity = 0m,
                SafetyStock = 20m,
                LastUpdatedAt = DateTime.UtcNow
            },
            new Inventory
            {
                ProductId = product5.ProductId,
                ConfirmedQuantity = 150m,
                PendingInQuantity = 0m,
                PendingOutQuantity = 0m,
                SafetyStock = 30m,
                LastUpdatedAt = DateTime.UtcNow
            }
        };

        context.Inventories.AddRange(inventories);
        context.SaveChanges();

        // ============================
        // 3. 샘플 판매 (Sale - 확정)
        // ============================
        // 기존 회사 참조 (Company PK는 string)
        var firstCompanyId = "COMP001";
        var secondCompanyId = "COMP002";

        var sale = new Sale
        {
            CompanyId = firstCompanyId,
            SaleDate = DateTime.Today.AddDays(-5),
            State = SaleState.Confirmed,
            TotalAmount = 175000m,
            ConfirmedAt = DateTime.UtcNow.AddDays(-4),
            CreatedAt = DateTime.UtcNow.AddDays(-5)
        };

        context.Sales.Add(sale);
        context.SaveChanges(); // Sale 먼저 저장하여 PK 생성

        var saleItem = new SaleItem
        {
            SaleId = sale.SaleId,
            ProductId = product1.ProductId,
            ProductName = "의료용 테이프",
            Quantity = 50m,
            UnitPrice = 3500m,
            LineAmount = 175000m
        };

        context.SaleItems.Add(saleItem);
        context.SaveChanges();

        // ============================
        // 4. 샘플 발주 (Order - 완료)
        // ============================
        var order = new Order
        {
            CompanyId = secondCompanyId,
            OrderDate = DateTime.Today.AddDays(-3),
            State = OrderState.Completed,
            TotalAmount = 250000m,
            CompletedAt = DateTime.UtcNow.AddDays(-2),
            CreatedAt = DateTime.UtcNow.AddDays(-3)
        };

        context.Orders.Add(order);
        context.SaveChanges(); // Order 먼저 저장하여 PK 생성

        var orderItem = new OrderItem
        {
            OrderId = order.OrderId,
            ProductId = product4.ProductId,
            ProductName = "수술용 장갑",
            Quantity = 10m,
            UnitPrice = 25000m,
            LineAmount = 250000m
        };

        context.OrderItems.Add(orderItem);
        context.SaveChanges();
    }

    /// <summary>
    /// 데이터베이스 삭제 (테스트용)
    /// </summary>
    public static void Drop(TranDbContext context)
    {
        context.Database.EnsureDeleted();
    }
}
