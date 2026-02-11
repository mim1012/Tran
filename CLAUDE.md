# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Tran is a **B2B 거래명세표(Transaction Statement) management system** for medical device distribution, built as a WPF desktop application. It combines ERP modules (Order, Purchase, Sale, Quotation, Inventory) with peer-to-peer document exchange using state machine-based workflow control.

**Core Principle:** "상태가 곧 권한이다" (State IS Permission) - All UI/permissions are controlled by document state.

## Build & Run Commands

```bash
# Build entire solution
dotnet build Tran.sln

# Build specific project
dotnet build Tran.Desktop/Tran.Desktop.csproj

# Run application
dotnet run --project Tran.Desktop/Tran.Desktop.csproj
```

There are no test projects in this solution.

## Architecture

### Solution Structure & Dependencies

```
Tran.Desktop  →  Tran.Data  →  Tran.Core
(WPF/MVVM)       (EF Core)     (Domain models, services)
```

- **Tran.Core** — Pure domain layer with no external dependencies. Models, enums, service interfaces, `StateTransitionService`, `UserContext`.
- **Tran.Data** — EF Core 8.0 with SQLite, `DbContextFactory`, `DatabaseInitializer`, all service implementations. Also uses ClosedXML for Excel export.
- **Tran.Desktop** — WPF UI with MVVM pattern (no DI container). Custom Pretendard font embedded. Theme resources in `Themes/` (Colors.xaml, Typography.xaml, Controls.xaml, Components.xaml).

### No Dependency Injection Container

The app does **not** use a DI framework. Services are created manually:

```csharp
// ViewModels create DbContext directly via factory
using var context = DbContextFactory.Create();
var service = new SomeService(context);
```

`DbContextFactory.Create()` is the centralized entry point for all database access. Never construct `TranDbContext` directly.

### Application Startup Flow

```
App.xaml.cs OnStartup()
  → DatabaseInitializer.Initialize() + CreateErpSampleData()
  → CompanySelectionWindow (entry point, set in App.xaml StartupUri)
      → User selects company → UserContext.SetUser() → MainWorkspaceWindow
      → Or "Product Management" → MainWorkspaceWindow(productManagementMode: true)
```

### UserContext (Static Singleton)

`UserContext` is set when a company is selected and is required for `StateTransitionService` to allow state transitions (`IsInitialized` must be true). It holds `CurrentUserId`, `CurrentUserName`, `CurrentCompanyId`.

### Multi-Company Workspace Architecture

`MainWorkspaceWindow` uses a **tabbed MDI** pattern:
- Multiple company tabs open simultaneously (`CompanyWorkspaces: ObservableCollection<CompanyWorkspace>`)
- Each `CompanyWorkspace` contains 6 ViewModels: Order, Quotation, Purchase, Sale, Inventory, SalesStatistics
- Internal tabs per company: 발주(0), 견적(1), 구매(2), 판매(3), 재고(4), 통계(5)
- Separate global mode: 품목관리 (Product Management, single instance)
- **Cross-tab data sync** via `OnDataChanged` callbacks between ViewModels within a workspace

## Domain Model

### Document State Machine (Critical)

All transitions must go through `StateTransitionService`. Never set `Document.State` directly.

```
Draft → Sent → Received → Confirmed (terminal)
                        ↘ RevisionRequested → Draft (creates NEW version with -V suffix,
                                                      original becomes Superseded)
Draft → Cancelled (terminal)
```

**Terminal states:** Confirmed, Superseded, Cancelled

**Special handling:** RevisionRequested → Draft creates a new `Document` (incremented version, `-V` suffix in DocumentNumber), sets original to Superseded, and returns two `StateLog` entries in `StateTransitionResult`.

**Optimistic locking:** `Document.StateVersion` is an EF Core ConcurrencyToken preventing concurrent state transitions.

### ERP Module States

Order, Purchase, Sale, and Quotation each have their own state enums (Draft → Pending → Completed/Confirmed), separate from the Document state machine.

### Key Patterns

- **Soft delete:** `Company.IsActive` flag (Company.Status is `[NotMapped]`, computed from IsActive)
- **Schema-less extensions:** `DocumentItem.ExtraDataJson` for flexible data
- **ContentHash:** SHA-256 on Document content. Template changes never affect hash.
- **Audit trail:** `DocumentStateLog` records are immutable (DeleteBehavior.Restrict)

## Code Patterns

### ViewModel Pattern

```csharp
public class SomeViewModel : ViewModelBase
{
    // Use SetProperty for property changes (auto-raises PropertyChanged)
    private string _name;
    public string Name { get => _name; set => SetProperty(ref _name, value); }

    // Use AsyncRelayCommand for async operations (has IsExecuting guard)
    public ICommand SaveCommand { get; }

    // Cross-VM communication via callbacks
    public Action? OnDataChanged { get; set; }
}
```

- `ViewModelBase` — `INotifyPropertyChanged` + `IDisposable`, `SetProperty<T>`, `RaisePropertyChanged`
- `RelayCommand` / `RelayCommand<T>` — synchronous commands
- `AsyncRelayCommand` — async commands with built-in double-click prevention via `IsExecuting`

### State Transitions

```csharp
var service = new StateTransitionService();
if (service.CanTransition(document.State, DocumentState.Sent))
{
    var result = await service.TransitionAsync(document, DocumentState.Sent, userId, reason);
    // result.StateLogs (list) should all be persisted
    // result.NewVersionDocument is non-null for RevisionRequested→Draft
}
```

## Database

- **SQLite** file: `tran.db` (created in working directory)
- **No migrations** — uses `EnsureCreated()` via `DatabaseInitializer`
- **Sample data:** 10 companies, 20 products, 80+ sales records, 50+ purchases (8 months of history) seeded on first run when Documents table is empty
- Key indexes: `idx_documents_state`, `idx_documents_company` (FromCompanyId+ToCompanyId), `idx_logs_document`

## UI Layer Rules

### Screen Hierarchy

```
거래명세표 (Core) — Only place where document state machine operates
거래처 관리 (Address Book) — Relationship management
정산 관리 (Derived) — Read-only aggregation of confirmed documents
양식 관리 (Template) — Output formatting only, never affects ContentHash
로그/이력 (Audit) — Immutable evidence storage
```

### State-Based UI Colors

| State | Badge Background | Badge Text |
|-------|------------------|------------|
| Draft | `#F0F0F0` | `#555555` |
| Sent | `#E8F1FF` | `#1E5EFF` |
| Confirmed | `#E6F4EA` | `#1E7F34` |
| RevisionRequested | `#FFF4E5` | `#E67700` |

These colors must be consistent across ALL screens. Theme defined in `Themes/Colors.xaml` with `Tran.Primary.*` and `Tran.Neutral.*` brush naming.

## Korean Language Context

- 거래명세표 = Transaction Statement / Invoice
- 거래처 = Trading Partner / Business Partner
- 정산 = Settlement
- 양식 = Template / Form
- 확정 = Confirmed, 전송 = Sent, 수정요청 = Revision Request
- 발주 = Order, 견적 = Quotation, 구매 = Purchase, 판매 = Sale, 재고 = Inventory
