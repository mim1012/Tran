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
    /// 데이터베이스 초기화 (없으면 생성, 있으면 그대로 유지)
    /// </summary>
    public static void Initialize(TranDbContext context)
    {
        context.Database.EnsureCreated();
    }

    /// <summary>
    /// 의료기기 업종 ERP 샘플 데이터 생성
    /// 5개 카테고리, 20개 품목, 8개월치 판매/매입 데이터
    /// </summary>
    public static void CreateErpSampleData(TranDbContext context, string ownerCompanyId = "COMP001")
    {
        if (context.Products.Any())
            return;

        var now = DateTime.UtcNow;
        var today = DateTime.Today;

        // ════════════════════════════════════════════
        // 1. 품목 마스터 (Products) — 의료기기 5개 카테고리, 20개 품목
        // ════════════════════════════════════════════
        var products = new Product[]
        {
            // 진단기기 (5)
            new() { ProductName = "자동혈압계", ProductCode = "DG-001", Category = "진단기기", Unit = "대", DefaultPrice = 85000m, IsActive = true, CreatedAt = now },
            new() { ProductName = "비접촉체온계", ProductCode = "DG-002", Category = "진단기기", Unit = "개", DefaultPrice = 35000m, IsActive = true, CreatedAt = now },
            new() { ProductName = "혈당측정기", ProductCode = "DG-003", Category = "진단기기", Unit = "세트", DefaultPrice = 45000m, IsActive = true, CreatedAt = now },
            new() { ProductName = "청진기", ProductCode = "DG-004", Category = "진단기기", Unit = "개", DefaultPrice = 120000m, IsActive = true, CreatedAt = now },
            new() { ProductName = "산소포화도측정기", ProductCode = "DG-005", Category = "진단기기", Unit = "대", DefaultPrice = 65000m, IsActive = true, CreatedAt = now },
            // 수술용품 (4)
            new() { ProductName = "수술용 장갑", ProductCode = "SG-001", Category = "수술용품", Unit = "박스", DefaultPrice = 28000m, IsActive = true, CreatedAt = now },
            new() { ProductName = "수술용 가운", ProductCode = "SG-002", Category = "수술용품", Unit = "벌", DefaultPrice = 35000m, IsActive = true, CreatedAt = now },
            new() { ProductName = "봉합사", ProductCode = "SG-003", Category = "수술용품", Unit = "팩", DefaultPrice = 18000m, IsActive = true, CreatedAt = now },
            new() { ProductName = "수술용 메스", ProductCode = "SG-004", Category = "수술용품", Unit = "박스", DefaultPrice = 42000m, IsActive = true, CreatedAt = now },
            // 소모품 (4)
            new() { ProductName = "의료용 테이프", ProductCode = "SM-001", Category = "소모품", Unit = "개", DefaultPrice = 3500m, IsActive = true, CreatedAt = now },
            new() { ProductName = "멸균거즈", ProductCode = "SM-002", Category = "소모품", Unit = "팩", DefaultPrice = 12000m, IsActive = true, CreatedAt = now },
            new() { ProductName = "일회용 주사기", ProductCode = "SM-003", Category = "소모품", Unit = "박스", DefaultPrice = 15000m, IsActive = true, CreatedAt = now },
            new() { ProductName = "수액세트", ProductCode = "SM-004", Category = "소모품", Unit = "박스", DefaultPrice = 22000m, IsActive = true, CreatedAt = now },
            // 위생용품 (4)
            new() { ProductName = "손소독제", ProductCode = "WS-001", Category = "위생용품", Unit = "병", DefaultPrice = 8000m, IsActive = true, CreatedAt = now },
            new() { ProductName = "의료용 마스크", ProductCode = "WS-002", Category = "위생용품", Unit = "박스", DefaultPrice = 25000m, IsActive = true, CreatedAt = now },
            new() { ProductName = "표면소독제", ProductCode = "WS-003", Category = "위생용품", Unit = "병", DefaultPrice = 15000m, IsActive = true, CreatedAt = now },
            new() { ProductName = "일회용 캡", ProductCode = "WS-004", Category = "위생용품", Unit = "박스", DefaultPrice = 12000m, IsActive = true, CreatedAt = now },
            // 재활기기 (3)
            new() { ProductName = "탄력밴드 세트", ProductCode = "RH-001", Category = "재활기기", Unit = "세트", DefaultPrice = 22000m, IsActive = true, CreatedAt = now },
            new() { ProductName = "무릎보조기", ProductCode = "RH-002", Category = "재활기기", Unit = "개", DefaultPrice = 180000m, IsActive = true, CreatedAt = now },
            new() { ProductName = "냉온찜질팩", ProductCode = "RH-003", Category = "재활기기", Unit = "개", DefaultPrice = 8500m, IsActive = true, CreatedAt = now },
        };

        context.Products.AddRange(products);
        context.SaveChanges();

        // ════════════════════════════════════════════
        // 2. 재고 (Inventory)
        // ════════════════════════════════════════════
        var inventories = products.Select((p, idx) =>
        {
            var stockLevels = new (decimal confirmed, decimal safety)[]
            {
                (120, 30), (200, 50), (80, 20), (45, 10), (60, 15),    // 진단
                (150, 40), (80, 20), (300, 80), (100, 25),             // 수술
                (500, 100), (400, 80), (250, 60), (180, 40),           // 소모
                (350, 70), (200, 50), (150, 30), (120, 25),            // 위생
                (90, 20), (25, 5), (200, 40)                           // 재활
            };
            var (confirmed, safety) = stockLevels[idx];
            return new Inventory
            {
                ProductId = p.ProductId,
                ConfirmedQuantity = confirmed,
                PendingInQuantity = idx % 4 == 0 ? 30m : 0m,
                PendingOutQuantity = idx % 3 == 0 ? 10m : 0m,
                SafetyStock = safety,
                LastUpdatedAt = now
            };
        }).ToArray();

        context.Inventories.AddRange(inventories);
        context.SaveChanges();

        // ════════════════════════════════════════════
        // 3. 판매 데이터 (Sales) — 8개월간 80+ 건
        // ════════════════════════════════════════════
        var saleCustomers = new[] { "COMP002", "COMP003", "COMP004", "COMP005", "COMP006", "COMP007" };
        var random = new Random(42); // 재현 가능한 seed

        // 월별 판매 패턴 (여름 비수기, 겨울 성수기 반영)
        var monthlyPatterns = new (int year, int month, int saleCount, decimal multiplier)[]
        {
            (2025, 7, 8, 0.8m),
            (2025, 8, 7, 0.75m),
            (2025, 9, 10, 0.9m),
            (2025, 10, 12, 1.0m),
            (2025, 11, 14, 1.1m),
            (2025, 12, 16, 1.2m),
            (2026, 1, 13, 1.15m),
            (2026, 2, 10, 1.0m),
        };

        var allSaleItems = new List<SaleItem>();

        foreach (var (year, month, saleCount, multiplier) in monthlyPatterns)
        {
            var monthStart = new DateTime(year, month, 1);
            var daysInMonth = DateTime.DaysInMonth(year, month);
            // 이번 달에는 오늘까지만
            var maxDay = (year == today.Year && month == today.Month)
                ? Math.Min(today.Day, daysInMonth) : daysInMonth;

            for (int s = 0; s < saleCount; s++)
            {
                var saleDay = random.Next(1, maxDay + 1);
                var saleDate = new DateTime(year, month, saleDay);
                var customerId = saleCustomers[random.Next(saleCustomers.Length)];

                // 각 판매에 1~4개 품목
                var itemCount = random.Next(1, 5);
                var saleItemList = new List<(Product prod, decimal qty, decimal price)>();
                decimal totalAmount = 0;

                for (int i = 0; i < itemCount; i++)
                {
                    var prod = products[random.Next(products.Length)];
                    var qty = (decimal)(random.Next(5, 50));
                    var price = prod.DefaultPrice * multiplier;
                    price = Math.Round(price / 100) * 100; // 100원 단위 반올림
                    var lineAmt = qty * price;
                    totalAmount += lineAmt;
                    saleItemList.Add((prod, qty, price));
                }

                var sale = new Sale
                {
                    OwnerCompanyId = ownerCompanyId,
                    CompanyId = customerId,
                    SaleDate = saleDate,
                    State = SaleState.Confirmed,
                    TotalAmount = totalAmount,
                    ConfirmedAt = saleDate.AddDays(1),
                    CreatedAt = saleDate
                };

                context.Sales.Add(sale);
                context.SaveChanges();

                foreach (var (prod, qty, price) in saleItemList)
                {
                    allSaleItems.Add(new SaleItem
                    {
                        SaleId = sale.SaleId,
                        ProductId = prod.ProductId,
                        ProductName = prod.ProductName,
                        Quantity = qty,
                        UnitPrice = price,
                        LineAmount = qty * price
                    });
                }
            }
        }

        // 오늘 날짜 Draft 판매 3건 (미확정)
        for (int d = 0; d < 3; d++)
        {
            var prod = products[random.Next(products.Length)];
            var qty = (decimal)random.Next(10, 30);
            var draftSale = new Sale
            {
                OwnerCompanyId = ownerCompanyId,
                CompanyId = saleCustomers[random.Next(saleCustomers.Length)],
                SaleDate = today,
                State = SaleState.Draft,
                TotalAmount = qty * prod.DefaultPrice,
                CreatedAt = now
            };
            context.Sales.Add(draftSale);
            context.SaveChanges();

            allSaleItems.Add(new SaleItem
            {
                SaleId = draftSale.SaleId,
                ProductId = prod.ProductId,
                ProductName = prod.ProductName,
                Quantity = qty,
                UnitPrice = prod.DefaultPrice,
                LineAmount = qty * prod.DefaultPrice
            });
        }

        context.SaleItems.AddRange(allSaleItems);
        context.SaveChanges();

        // ════════════════════════════════════════════
        // 4. 매입 데이터 (Purchases) — 8개월간 50+ 건
        // ════════════════════════════════════════════
        var suppliers = new[] { "COMP008", "COMP009", "COMP010" };
        var allPurchaseItems = new List<PurchaseItem>();

        // 매입 원가율: 판매가의 55~70%
        decimal costRate = 0.62m;

        foreach (var (year, month, saleCount, multiplier) in monthlyPatterns)
        {
            var monthStart = new DateTime(year, month, 1);
            var daysInMonth = DateTime.DaysInMonth(year, month);
            var maxDay = (year == today.Year && month == today.Month)
                ? Math.Min(today.Day, daysInMonth) : daysInMonth;
            var purchaseCount = Math.Max(4, saleCount * 2 / 3);

            for (int p = 0; p < purchaseCount; p++)
            {
                var purchDay = random.Next(1, maxDay + 1);
                var purchDate = new DateTime(year, month, purchDay);
                var supplierId = suppliers[random.Next(suppliers.Length)];

                var itemCount = random.Next(2, 6);
                var purchItemList = new List<(Product prod, decimal qty, decimal price)>();
                decimal totalAmount = 0;

                for (int i = 0; i < itemCount; i++)
                {
                    var prod = products[random.Next(products.Length)];
                    var qty = (decimal)random.Next(20, 100);
                    var price = Math.Round(prod.DefaultPrice * costRate / 100) * 100;
                    var lineAmt = qty * price;
                    totalAmount += lineAmt;
                    purchItemList.Add((prod, qty, price));
                }

                var purchase = new Purchase
                {
                    OwnerCompanyId = ownerCompanyId,
                    CompanyId = supplierId,
                    PurchaseDate = purchDate,
                    State = PurchaseState.Delivered,
                    TotalAmount = totalAmount,
                    DeliveredAt = purchDate.AddDays(2),
                    CreatedAt = purchDate
                };

                context.Purchases.Add(purchase);
                context.SaveChanges();

                foreach (var (prod, qty, price) in purchItemList)
                {
                    allPurchaseItems.Add(new PurchaseItem
                    {
                        PurchaseId = purchase.PurchaseId,
                        ProductId = prod.ProductId,
                        ProductName = prod.ProductName,
                        OrderedQuantity = qty,
                        ReceivedQuantity = qty,
                        DefectQuantity = 0,
                        UnitPrice = price,
                        LineAmount = qty * price
                    });
                }
            }
        }

        // 입고대기 매입 3건
        for (int i = 0; i < 3; i++)
        {
            var prod = products[random.Next(products.Length)];
            var qty = (decimal)random.Next(30, 80);
            var price = Math.Round(prod.DefaultPrice * costRate / 100) * 100;

            var pendingPurchase = new Purchase
            {
                OwnerCompanyId = ownerCompanyId,
                CompanyId = suppliers[random.Next(suppliers.Length)],
                PurchaseDate = today.AddDays(-random.Next(1, 5)),
                State = PurchaseState.PendingDelivery,
                TotalAmount = qty * price,
                CreatedAt = now
            };
            context.Purchases.Add(pendingPurchase);
            context.SaveChanges();

            allPurchaseItems.Add(new PurchaseItem
            {
                PurchaseId = pendingPurchase.PurchaseId,
                ProductId = prod.ProductId,
                ProductName = prod.ProductName,
                OrderedQuantity = qty,
                ReceivedQuantity = 0,
                DefectQuantity = 0,
                UnitPrice = price,
                LineAmount = qty * price
            });
        }

        context.PurchaseItems.AddRange(allPurchaseItems);
        context.SaveChanges();

        // ════════════════════════════════════════════
        // 5. 발주 데이터 (Orders) — Draft 4건 + Completed 3건
        // ════════════════════════════════════════════

        // 대기 발주 (Draft)
        for (int i = 0; i < 4; i++)
        {
            var prod = products[random.Next(products.Length)];
            var qty = (decimal)random.Next(20, 60);
            var price = Math.Round(prod.DefaultPrice * costRate / 100) * 100;

            var order = new Order
            {
                OwnerCompanyId = ownerCompanyId,
                CompanyId = suppliers[random.Next(suppliers.Length)],
                OrderDate = today.AddDays(-random.Next(0, 3)),
                State = OrderState.Draft,
                TotalAmount = qty * price,
                CreatedAt = now
            };
            context.Orders.Add(order);
            context.SaveChanges();

            context.OrderItems.Add(new OrderItem
            {
                OrderId = order.OrderId,
                ProductId = prod.ProductId,
                ProductName = prod.ProductName,
                Quantity = qty,
                UnitPrice = price,
                LineAmount = qty * price
            });
        }

        // 완료 발주
        for (int i = 0; i < 3; i++)
        {
            var prod = products[random.Next(products.Length)];
            var qty = (decimal)random.Next(30, 80);
            var price = Math.Round(prod.DefaultPrice * costRate / 100) * 100;

            var order = new Order
            {
                OwnerCompanyId = ownerCompanyId,
                CompanyId = suppliers[random.Next(suppliers.Length)],
                OrderDate = today.AddDays(-random.Next(10, 30)),
                State = OrderState.Completed,
                TotalAmount = qty * price,
                CompletedAt = today.AddDays(-random.Next(5, 10)),
                CreatedAt = now
            };
            context.Orders.Add(order);
            context.SaveChanges();

            context.OrderItems.Add(new OrderItem
            {
                OrderId = order.OrderId,
                ProductId = prod.ProductId,
                ProductName = prod.ProductName,
                Quantity = qty,
                UnitPrice = price,
                LineAmount = qty * price
            });
        }

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
