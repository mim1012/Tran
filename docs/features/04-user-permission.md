# 사용자/권한 관리 상세 명세

> **핵심 원칙**: 역할 기반 접근 제어 (RBAC) + 거래처별 데이터 격리

---

## 1. 사용자 관리 (User Management)

### 1.1 데이터 모델

```csharp
public class User
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }

    public string Name { get; set; }           // 이름
    public string Phone { get; set; }          // 연락처
    public string Department { get; set; }     // 부서

    // 소속 회사 (거래처)
    public int? CompanyId { get; set; }
    public Company Company { get; set; }

    // 역할
    public List<UserRole> Roles { get; set; }

    // 상태
    public UserState State { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public int LoginFailCount { get; set; }
    public DateTime? LockedUntil { get; set; }

    public DateTime CreatedAt { get; set; }
    public int? CreatedBy { get; set; }
}

public class UserRole
{
    public int UserId { get; set; }
    public User User { get; set; }

    public int RoleId { get; set; }
    public Role Role { get; set; }

    public DateTime AssignedAt { get; set; }
    public int AssignedBy { get; set; }
}

public class Role
{
    public int Id { get; set; }
    public string Name { get; set; }           // 역할명
    public string Description { get; set; }    // 설명
    public RoleType Type { get; set; }         // 역할 유형

    public List<RolePermission> Permissions { get; set; }

    public bool IsSystemRole { get; set; }     // 시스템 기본 역할 (삭제 불가)
}

public enum RoleType
{
    Hospital = 1,      // 병원
    Headquarters = 2,  // 본사
    Logistics = 3,     // 물류
    Admin = 4          // 관리자
}

public enum UserState
{
    Active = 0,        // 활성
    Inactive = 1,      // 비활성
    Locked = 2,        // 잠김
    Pending = 3        // 승인대기
}
```

### 1.2 기본 역할 정의

| 역할 | 유형 | 설명 | 주요 권한 |
|------|------|------|-----------|
| Hospital | 병원 | 병원 사용자 | 발주 요청, 납품 확인, 본인 거래 조회 |
| Headquarters | 본사 | 본사 직원 | 전체 거래 관리, 단가 관리, 정산 |
| Logistics | 물류 | 물류 담당자 | 재고, 입출고, 배송 관리 |
| Admin | 관리자 | 시스템 관리자 | 모든 권한 + 사용자/권한 관리 |

### 1.3 사용자 관리 화면

```
┌─────────────────────────────────────────────────────────────────────┐
│ 사용자 관리                                          [+ 사용자 등록]│
├─────────────────────────────────────────────────────────────────────┤
│ [역할 ▼] [상태 ▼] [소속 ▼]                          [검색...]      │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │ 이름     │ 이메일           │ 소속     │ 역할   │ 상태     │   │
│  ├──────────┼──────────────────┼──────────┼────────┼──────────┤   │
│  │ 홍길동   │ hong@company.com │ 본사     │ 본사   │ ✅ 활성  │   │
│  │ 김영희   │ kim@hospital.com │ A병원    │ 병원   │ ✅ 활성  │   │
│  │ 박철수   │ park@logistics.com│ 물류팀  │ 물류   │ ✅ 활성  │   │
│  │ 이민수   │ lee@company.com  │ 본사     │ 관리자 │ ✅ 활성  │   │
│  │ 최수진   │ choi@hospital.com│ B의원    │ 병원   │ 🔒 잠김  │   │
│  └─────────────────────────────────────────────────────────────┘   │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

### 1.4 사용자 등록 서비스

```csharp
public class UserService
{
    /// <summary>
    /// 사용자 등록
    /// </summary>
    public async Task<User> CreateUserAsync(UserCreateDto dto)
    {
        // 중복 체크
        if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
        {
            throw new DuplicateEmailException(dto.Email);
        }

        var user = new User
        {
            Username = dto.Username,
            Email = dto.Email,
            PasswordHash = _passwordHasher.Hash(dto.Password),
            Name = dto.Name,
            Phone = dto.Phone,
            Department = dto.Department,
            CompanyId = dto.CompanyId,
            State = dto.RequireApproval ? UserState.Pending : UserState.Active,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _currentUser.Id
        };

        _context.Users.Add(user);

        // 역할 할당
        foreach (var roleId in dto.RoleIds)
        {
            user.Roles.Add(new UserRole
            {
                RoleId = roleId,
                AssignedAt = DateTime.UtcNow,
                AssignedBy = _currentUser.Id
            });
        }

        await _context.SaveChangesAsync();

        // 이메일 발송
        if (dto.SendWelcomeEmail)
        {
            await _emailService.SendWelcomeEmailAsync(user, dto.Password);
        }

        return user;
    }

    /// <summary>
    /// 비밀번호 변경
    /// </summary>
    public async Task ChangePasswordAsync(int userId, PasswordChangeDto dto)
    {
        var user = await _context.Users.FindAsync(userId);

        // 현재 비밀번호 확인
        if (!_passwordHasher.Verify(dto.CurrentPassword, user.PasswordHash))
        {
            throw new InvalidPasswordException();
        }

        user.PasswordHash = _passwordHasher.Hash(dto.NewPassword);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// 로그인 실패 처리
    /// </summary>
    public async Task HandleLoginFailureAsync(User user)
    {
        user.LoginFailCount++;

        // 5회 실패 시 잠금
        if (user.LoginFailCount >= 5)
        {
            user.State = UserState.Locked;
            user.LockedUntil = DateTime.UtcNow.AddHours(1);
        }

        await _context.SaveChangesAsync();
    }
}
```

---

## 2. 권한 관리 (Permission Management)

### 2.1 권한 모델

```csharp
public class Permission
{
    public int Id { get; set; }
    public string Code { get; set; }           // 권한 코드
    public string Name { get; set; }           // 권한명
    public string Description { get; set; }    // 설명
    public PermissionCategory Category { get; set; }
}

public class RolePermission
{
    public int RoleId { get; set; }
    public Role Role { get; set; }

    public int PermissionId { get; set; }
    public Permission Permission { get; set; }

    public bool CanView { get; set; }
    public bool CanCreate { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
}

public enum PermissionCategory
{
    Document = 1,      // 서류 관리
    Order = 2,         // 발주 관리
    Purchase = 3,      // 구매 관리
    Sale = 4,          // 판매 관리
    Inventory = 5,     // 재고 관리
    Finance = 6,       // 재무/정산
    System = 7         // 시스템
}
```

### 2.2 권한 정의

```csharp
public static class Permissions
{
    // 서류 관리
    public const string QuotationView = "quotation.view";
    public const string QuotationCreate = "quotation.create";
    public const string QuotationEdit = "quotation.edit";
    public const string QuotationDelete = "quotation.delete";
    public const string QuotationApprove = "quotation.approve";

    public const string ContractView = "contract.view";
    public const string ContractCreate = "contract.create";
    public const string ContractEdit = "contract.edit";
    public const string ContractDelete = "contract.delete";

    public const string PriceView = "price.view";
    public const string PriceEdit = "price.edit";

    // 발주 관리
    public const string OrderView = "order.view";
    public const string OrderCreate = "order.create";
    public const string OrderApprove = "order.approve";
    public const string OrderReject = "order.reject";

    // 구매 관리
    public const string PurchaseView = "purchase.view";
    public const string PurchaseCreate = "purchase.create";
    public const string PurchaseEdit = "purchase.edit";

    // 판매 관리
    public const string SaleView = "sale.view";
    public const string SaleCreate = "sale.create";
    public const string SaleEdit = "sale.edit";
    public const string SaleConfirm = "sale.confirm";

    // 재고 관리
    public const string InventoryView = "inventory.view";
    public const string InventoryAdjust = "inventory.adjust";
    public const string DefectManage = "defect.manage";

    // 재무/정산
    public const string ReceivableView = "receivable.view";
    public const string ReceivableManage = "receivable.manage";
    public const string TaxInvoiceView = "taxinvoice.view";
    public const string TaxInvoiceIssue = "taxinvoice.issue";
    public const string TaxInvoiceTransmit = "taxinvoice.transmit";

    // 시스템
    public const string UserManage = "user.manage";
    public const string RoleManage = "role.manage";
    public const string AuditLogView = "auditlog.view";
    public const string SettingsManage = "settings.manage";
}
```

### 2.3 역할별 권한 매트릭스

```
┌────────────────────┬────────┬────────┬────────┬────────┐
│ 권한               │ 병원   │ 본사   │ 물류   │ 관리자 │
├────────────────────┼────────┼────────┼────────┼────────┤
│ 견적서 조회        │ ○      │ ○      │ -      │ ○      │
│ 견적서 작성        │ -      │ ○      │ -      │ ○      │
│ 견적서 승인        │ -      │ ○      │ -      │ ○      │
├────────────────────┼────────┼────────┼────────┼────────┤
│ 계약서 조회        │ 본인만 │ ○      │ -      │ ○      │
│ 계약서 작성        │ -      │ ○      │ -      │ ○      │
├────────────────────┼────────┼────────┼────────┼────────┤
│ 단가 조회          │ 본인만 │ ○      │ -      │ ○      │
│ 단가 수정          │ -      │ ○      │ -      │ ○      │
├────────────────────┼────────┼────────┼────────┼────────┤
│ 발주 조회          │ ○      │ ○      │ ○      │ ○      │
│ 발주 요청          │ ○      │ ○      │ -      │ ○      │
│ 발주 승인          │ -      │ ○      │ -      │ ○      │
├────────────────────┼────────┼────────┼────────┼────────┤
│ 구매 조회          │ -      │ ○      │ ○      │ ○      │
│ 구매 관리          │ -      │ ○      │ ○      │ ○      │
├────────────────────┼────────┼────────┼────────┼────────┤
│ 판매 조회          │ 본인만 │ ○      │ ○      │ ○      │
│ 판매 등록          │ -      │ ○      │ ○      │ ○      │
│ 판매 확정          │ -      │ ○      │ ○      │ ○      │
├────────────────────┼────────┼────────┼────────┼────────┤
│ 재고 조회          │ -      │ ○      │ ○      │ ○      │
│ 재고 조정          │ -      │ -      │ ○      │ ○      │
│ 불량 관리          │ -      │ ○      │ ○      │ ○      │
├────────────────────┼────────┼────────┼────────┼────────┤
│ 채권 조회          │ -      │ ○      │ -      │ ○      │
│ 채권 관리          │ -      │ ○      │ -      │ ○      │
│ 세금계산서 발행    │ -      │ ○      │ -      │ ○      │
├────────────────────┼────────┼────────┼────────┼────────┤
│ 사용자 관리        │ -      │ -      │ -      │ ○      │
│ 권한 관리          │ -      │ -      │ -      │ ○      │
│ 감사 로그          │ -      │ -      │ -      │ ○      │
└────────────────────┴────────┴────────┴────────┴────────┘

○ = 접근 가능, - = 접근 불가, 본인만 = 본인 관련 데이터만
```

### 2.4 권한 검증 서비스

```csharp
public class AuthorizationService
{
    /// <summary>
    /// 권한 확인
    /// </summary>
    public async Task<bool> HasPermissionAsync(int userId, string permissionCode)
    {
        var user = await _context.Users
            .Include(u => u.Roles)
            .ThenInclude(ur => ur.Role)
            .ThenInclude(r => r.Permissions)
            .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null || user.State != UserState.Active)
            return false;

        return user.Roles
            .SelectMany(ur => ur.Role.Permissions)
            .Any(rp => rp.Permission.Code == permissionCode && rp.CanView);
    }

    /// <summary>
    /// 데이터 접근 권한 확인 (거래처 격리)
    /// </summary>
    public async Task<bool> CanAccessDataAsync(int userId, int? companyId)
    {
        var user = await _context.Users
            .Include(u => u.Roles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId);

        // 관리자/본사는 모든 데이터 접근 가능
        if (user.Roles.Any(ur =>
            ur.Role.Type == RoleType.Admin ||
            ur.Role.Type == RoleType.Headquarters))
        {
            return true;
        }

        // 물류는 재고/배송 관련만 접근 가능
        if (user.Roles.Any(ur => ur.Role.Type == RoleType.Logistics))
        {
            return true;  // 거래처 제한 없음 (재고는 전체)
        }

        // 병원은 본인 거래처만
        if (user.Roles.Any(ur => ur.Role.Type == RoleType.Hospital))
        {
            return companyId == user.CompanyId;
        }

        return false;
    }

    /// <summary>
    /// 데이터 필터 적용
    /// </summary>
    public async Task<IQueryable<T>> ApplyDataFilterAsync<T>(
        IQueryable<T> query,
        int userId) where T : class, ICompanyScoped
    {
        var user = await _context.Users
            .Include(u => u.Roles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId);

        // 관리자/본사는 필터 없음
        if (user.Roles.Any(ur =>
            ur.Role.Type == RoleType.Admin ||
            ur.Role.Type == RoleType.Headquarters))
        {
            return query;
        }

        // 병원은 본인 거래처만
        if (user.CompanyId.HasValue)
        {
            return query.Where(x => x.CompanyId == user.CompanyId);
        }

        // 그 외는 빈 결과
        return query.Where(x => false);
    }
}
```

### 2.5 권한 기반 UI 제어

```csharp
public class UiPermissionService
{
    /// <summary>
    /// 화면별 접근 가능 여부
    /// </summary>
    public async Task<Dictionary<string, bool>> GetScreenPermissionsAsync(int userId)
    {
        return new Dictionary<string, bool>
        {
            ["Quotation"] = await HasPermissionAsync(userId, Permissions.QuotationView),
            ["Contract"] = await HasPermissionAsync(userId, Permissions.ContractView),
            ["Price"] = await HasPermissionAsync(userId, Permissions.PriceView),
            ["Order"] = await HasPermissionAsync(userId, Permissions.OrderView),
            ["Purchase"] = await HasPermissionAsync(userId, Permissions.PurchaseView),
            ["Sale"] = await HasPermissionAsync(userId, Permissions.SaleView),
            ["Inventory"] = await HasPermissionAsync(userId, Permissions.InventoryView),
            ["Receivable"] = await HasPermissionAsync(userId, Permissions.ReceivableView),
            ["TaxInvoice"] = await HasPermissionAsync(userId, Permissions.TaxInvoiceView),
            ["UserManagement"] = await HasPermissionAsync(userId, Permissions.UserManage),
            ["Settings"] = await HasPermissionAsync(userId, Permissions.SettingsManage)
        };
    }

    /// <summary>
    /// 버튼별 표시 여부
    /// </summary>
    public async Task<Dictionary<string, bool>> GetButtonPermissionsAsync(
        int userId,
        string screen)
    {
        return screen switch
        {
            "Quotation" => new Dictionary<string, bool>
            {
                ["Create"] = await HasPermissionAsync(userId, Permissions.QuotationCreate),
                ["Edit"] = await HasPermissionAsync(userId, Permissions.QuotationEdit),
                ["Delete"] = await HasPermissionAsync(userId, Permissions.QuotationDelete),
                ["Approve"] = await HasPermissionAsync(userId, Permissions.QuotationApprove)
            },
            "Order" => new Dictionary<string, bool>
            {
                ["Create"] = await HasPermissionAsync(userId, Permissions.OrderCreate),
                ["Approve"] = await HasPermissionAsync(userId, Permissions.OrderApprove),
                ["Reject"] = await HasPermissionAsync(userId, Permissions.OrderReject)
            },
            // ... 기타 화면
            _ => new Dictionary<string, bool>()
        };
    }
}
```

---

## 3. 데이터 격리 (Data Isolation)

### 3.1 거래처별 데이터 격리

```csharp
/// <summary>
/// 거래처 범위 인터페이스
/// </summary>
public interface ICompanyScoped
{
    int CompanyId { get; set; }
}

/// <summary>
/// 자동 데이터 필터링 (EF Core Global Query Filter)
/// </summary>
public class TranDbContext : DbContext
{
    private readonly int? _currentCompanyId;
    private readonly bool _isAdminOrHeadquarters;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // 거래처 범위 엔티티에 자동 필터 적용
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ICompanyScoped).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var filter = Expression.Lambda(
                    Expression.OrElse(
                        Expression.Constant(_isAdminOrHeadquarters),
                        Expression.Equal(
                            Expression.Property(parameter, "CompanyId"),
                            Expression.Constant(_currentCompanyId)
                        )
                    ),
                    parameter
                );

                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
            }
        }
    }
}
```

### 3.2 단가 정보 격리

```csharp
public class PriceIsolationService
{
    /// <summary>
    /// 단가 조회 (거래처별 격리)
    /// </summary>
    public async Task<CompanyPrice> GetPriceAsync(int companyId, int productId)
    {
        // 현재 사용자가 해당 거래처 데이터에 접근 가능한지 확인
        if (!await _authService.CanAccessDataAsync(_currentUser.Id, companyId))
        {
            throw new UnauthorizedAccessException("해당 거래처의 단가 정보에 접근할 수 없습니다.");
        }

        return await _context.CompanyPrices
            .Where(p => p.CompanyId == companyId && p.ProductId == productId)
            .OrderByDescending(p => p.EffectiveDate)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// 단가 비교 방지 (병원 간)
    /// </summary>
    public async Task<List<PriceComparisonDto>> GetPriceComparisonAsync(
        int userId,
        int productId)
    {
        var user = await _context.Users.Include(u => u.Roles).FirstAsync(u => u.Id == userId);

        // 병원 사용자는 단가 비교 불가
        if (user.Roles.Any(r => r.Role.Type == RoleType.Hospital))
        {
            throw new UnauthorizedAccessException("거래처 간 단가 비교 권한이 없습니다.");
        }

        // 본사/관리자만 비교 가능
        return await _context.CompanyPrices
            .Where(p => p.ProductId == productId)
            .GroupBy(p => p.CompanyId)
            .Select(g => new PriceComparisonDto
            {
                CompanyId = g.Key,
                CurrentPrice = g.OrderByDescending(p => p.EffectiveDate).First().UnitPrice
            })
            .ToListAsync();
    }
}
```

---

## 4. 감사 로그 (Audit Log)

### 4.1 데이터 모델

```csharp
public class AuditLog
{
    public long Id { get; set; }

    public string Action { get; set; }          // 동작 (Create, Update, Delete, Login, etc.)
    public string EntityType { get; set; }      // 엔티티 유형
    public int? EntityId { get; set; }          // 엔티티 ID

    public string OldValue { get; set; }        // 변경 전 값 (JSON)
    public string NewValue { get; set; }        // 변경 후 값 (JSON)

    public int UserId { get; set; }             // 수행자
    public string UserName { get; set; }        // 수행자 이름 (비정규화)
    public string IpAddress { get; set; }       // IP 주소
    public string UserAgent { get; set; }       // 브라우저/클라이언트

    public string Reason { get; set; }          // 사유 (있는 경우)
    public DateTime Timestamp { get; set; }
}
```

### 4.2 감사 로그 서비스

```csharp
public class AuditService
{
    /// <summary>
    /// 감사 로그 기록
    /// </summary>
    public async Task LogAsync(AuditLogEntry entry)
    {
        var log = new AuditLog
        {
            Action = entry.Action,
            EntityType = entry.EntityType,
            EntityId = entry.EntityId,
            OldValue = entry.OldValue != null ? JsonSerializer.Serialize(entry.OldValue) : null,
            NewValue = entry.NewValue != null ? JsonSerializer.Serialize(entry.NewValue) : null,
            UserId = _currentUser.Id,
            UserName = _currentUser.Name,
            IpAddress = _httpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = _httpContext.Request.Headers["User-Agent"],
            Reason = entry.Reason,
            Timestamp = DateTime.UtcNow
        };

        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// 중요 동작 자동 기록 (인터셉터)
    /// </summary>
    public class AuditInterceptor : SaveChangesInterceptor
    {
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            var context = eventData.Context;
            var entries = context.ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added ||
                            e.State == EntityState.Modified ||
                            e.State == EntityState.Deleted);

            foreach (var entry in entries)
            {
                // 민감 데이터 변경 감지
                if (IsSensitiveEntity(entry.Entity.GetType()))
                {
                    await _auditService.LogAsync(new AuditLogEntry
                    {
                        Action = entry.State.ToString(),
                        EntityType = entry.Entity.GetType().Name,
                        EntityId = GetEntityId(entry),
                        OldValue = entry.State == EntityState.Modified ? GetOriginalValues(entry) : null,
                        NewValue = entry.State != EntityState.Deleted ? GetCurrentValues(entry) : null
                    });
                }
            }

            return result;
        }

        private bool IsSensitiveEntity(Type type)
        {
            return type == typeof(User) ||
                   type == typeof(CompanyPrice) ||
                   type == typeof(Contract) ||
                   type == typeof(Receivable);
        }
    }
}
```

### 4.3 감사 로그 조회 화면

```
┌─────────────────────────────────────────────────────────────────────┐
│ 감사 로그                                              [Excel 내보내기]│
├─────────────────────────────────────────────────────────────────────┤
│ [날짜 ▼] [동작 ▼] [사용자 ▼] [대상 ▼]                  [검색...]   │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │ 시간              │ 사용자  │ 동작   │ 대상       │ 상세    │   │
│  ├───────────────────┼─────────┼────────┼────────────┼─────────┤   │
│  │ 2025-01-22 14:32  │ 홍길동  │ Update │ 단가       │ [보기]  │   │
│  │ 2025-01-22 14:30  │ 김영희  │ Create │ 발주       │ [보기]  │   │
│  │ 2025-01-22 14:25  │ 이민수  │ Login  │ 사용자     │ [보기]  │   │
│  │ 2025-01-22 14:20  │ 박철수  │ Update │ 재고       │ [보기]  │   │
│  └─────────────────────────────────────────────────────────────┘   │
│                                                                     │
│  ┌─ 로그 상세 ───────────────────────────────────────────────────┐ │
│  │                                                                │ │
│  │  시간: 2025-01-22 14:32:15                                    │ │
│  │  사용자: 홍길동 (hong@company.com)                            │ │
│  │  IP: 192.168.1.100                                            │ │
│  │  동작: 단가 수정 (CompanyPrice)                               │ │
│  │                                                                │ │
│  │  변경 내용:                                                    │ │
│  │  ┌──────────────┬──────────────┬──────────────┐               │ │
│  │  │ 필드         │ 변경 전      │ 변경 후      │               │ │
│  │  ├──────────────┼──────────────┼──────────────┤               │ │
│  │  │ UnitPrice    │ ₩3,800      │ ₩3,500      │               │ │
│  │  │ EffectiveDate│ 2024-11-20  │ 2025-01-22  │               │ │
│  │  └──────────────┴──────────────┴──────────────┘               │ │
│  │                                                                │ │
│  │  사유: 견적서 #QT-0122-0001 확정에 따른 단가 변경             │ │
│  │                                                                │ │
│  └────────────────────────────────────────────────────────────────┘ │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

---

## 5. 보안 설정

### 5.1 비밀번호 정책

```csharp
public class PasswordPolicy
{
    public int MinLength { get; set; } = 8;
    public bool RequireUppercase { get; set; } = true;
    public bool RequireLowercase { get; set; } = true;
    public bool RequireDigit { get; set; } = true;
    public bool RequireSpecialChar { get; set; } = true;
    public int MaxLoginAttempts { get; set; } = 5;
    public int LockoutDurationMinutes { get; set; } = 60;
    public int PasswordExpirationDays { get; set; } = 90;
    public int PasswordHistoryCount { get; set; } = 5;  // 최근 5개 비밀번호 재사용 불가
}

public class PasswordValidator
{
    public ValidationResult Validate(string password, PasswordPolicy policy)
    {
        var errors = new List<string>();

        if (password.Length < policy.MinLength)
            errors.Add($"비밀번호는 {policy.MinLength}자 이상이어야 합니다.");

        if (policy.RequireUppercase && !password.Any(char.IsUpper))
            errors.Add("대문자를 포함해야 합니다.");

        if (policy.RequireLowercase && !password.Any(char.IsLower))
            errors.Add("소문자를 포함해야 합니다.");

        if (policy.RequireDigit && !password.Any(char.IsDigit))
            errors.Add("숫자를 포함해야 합니다.");

        if (policy.RequireSpecialChar && !password.Any(c => !char.IsLetterOrDigit(c)))
            errors.Add("특수문자를 포함해야 합니다.");

        return new ValidationResult
        {
            IsValid = !errors.Any(),
            Errors = errors
        };
    }
}
```

### 5.2 세션 관리

```csharp
public class SessionSettings
{
    public int SessionTimeoutMinutes { get; set; } = 30;
    public bool SingleSessionOnly { get; set; } = false;  // 동시 로그인 허용 여부
    public bool RememberMeEnabled { get; set; } = true;
    public int RememberMeDays { get; set; } = 30;
}

public class SessionService
{
    /// <summary>
    /// 세션 생성
    /// </summary>
    public async Task<Session> CreateSessionAsync(User user, bool rememberMe)
    {
        // 단일 세션 정책인 경우 기존 세션 종료
        if (_settings.SingleSessionOnly)
        {
            await InvalidateUserSessionsAsync(user.Id);
        }

        var session = new Session
        {
            UserId = user.Id,
            Token = GenerateSecureToken(),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = rememberMe
                ? DateTime.UtcNow.AddDays(_settings.RememberMeDays)
                : DateTime.UtcNow.AddMinutes(_settings.SessionTimeoutMinutes),
            IpAddress = GetClientIpAddress(),
            UserAgent = GetUserAgent()
        };

        _context.Sessions.Add(session);
        await _context.SaveChangesAsync();

        return session;
    }

    /// <summary>
    /// 세션 갱신 (활동 시)
    /// </summary>
    public async Task RefreshSessionAsync(string token)
    {
        var session = await _context.Sessions.FirstOrDefaultAsync(s => s.Token == token);

        if (session != null && session.ExpiresAt > DateTime.UtcNow)
        {
            session.ExpiresAt = DateTime.UtcNow.AddMinutes(_settings.SessionTimeoutMinutes);
            await _context.SaveChangesAsync();
        }
    }
}
```
