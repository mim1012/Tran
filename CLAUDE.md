# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Tran is a **B2B 거래명세표(Transaction Statement) management system** built as a WPF desktop application. It enables peer-to-peer document exchange between companies with state machine-based workflow control.

**Core Principle:** "상태가 곧 권한이다" (State IS Permission) - All UI/permissions are controlled by document state.

## Build & Run Commands

```bash
# Build entire solution
dotnet build Tran.sln

# Build specific project
dotnet build Tran.Desktop/Tran.Desktop.csproj

# Run application
dotnet run --project Tran.Desktop/Tran.Desktop.csproj

# Or run built executable directly
./Tran.Desktop/bin/Debug/net8.0-windows/Tran.Desktop.exe
```

## Architecture

### Solution Structure

```
Tran.sln
├── Tran.Core/          # Domain models, business logic, services
├── Tran.Data/          # EF Core DbContext, SQLite persistence
└── Tran.Desktop/       # WPF UI (MVVM pattern)
```

### Layer Dependencies

```
Tran.Desktop → Tran.Data → Tran.Core
```

### Key Technologies

- .NET 8.0
- WPF (Windows Presentation Foundation)
- Entity Framework Core 8.0 with SQLite
- MVVM Pattern

## Domain Model

### Document State Machine (Critical)

The state machine is the heart of the system. All transitions must go through `StateTransitionService`.

```
Draft → Sent → Received → Confirmed (terminal)
                       ↘ RevisionRequested → Draft (new version)
Draft → Cancelled (terminal)
```

**Terminal states:** Confirmed, Superseded, Cancelled (no further transitions allowed)

### Core Entities

- **Document** - Transaction statement with state, hash, versions
- **DocumentItem** - Line items within a document
- **Company** - Trading partners (address book)
- **DocumentStateLog** - Immutable audit trail for disputes
- **DocumentTemplate** - Output formatting (never affects hash)
- **Settlement** - Read-only aggregation of confirmed documents

## UI Layer Rules

### Hierarchy (from docs/UI_DEVELOPMENT_STRATEGY.md)

```
거래명세표 (Core) - Only place where state machine operates
    ↓
거래처 관리 (Address Book) - Relationship management
    ↓
정산 관리 (Derived) - Read-only aggregation
    ↓
양식 관리 (Template) - Output formatting only
    ↓
로그/이력 (Audit) - Evidence storage
```

### Immutable Rules

- Only 거래명세표 screen can trigger state transitions
- No document modifications from other screens
- No changes that affect ContentHash outside core screen
- Template changes NEVER affect document hash

### State-Based UI Colors (from docs/UI_UX_GUIDELINES.md)

| State | Badge Background | Badge Text |
|-------|------------------|------------|
| Draft | `#F0F0F0` | `#555555` |
| Sent | `#E8F1FF` | `#1E5EFF` |
| Confirmed | `#E6F4EA` | `#1E7F34` |
| RevisionRequested | `#FFF4E5` | `#E67700` |

These colors must be consistent across ALL screens.

## Code Patterns

### State Transitions

Always use `StateTransitionService` for document state changes:

```csharp
var service = new StateTransitionService();
if (service.CanTransition(document.State, DocumentState.Sent))
{
    var result = await service.TransitionAsync(document, DocumentState.Sent, userId, reason);
    // result.StateLog should be persisted
}
```

### DbContext Usage

```csharp
var options = new DbContextOptionsBuilder<TranDbContext>()
    .UseSqlite("Data Source=tran.db")
    .Options;

using var context = new TranDbContext(options);
```

### ViewModel Pattern

ViewModels inherit from `ViewModelBase` and implement `INotifyPropertyChanged`. Use `RelayCommand` for command binding.

```csharp
public class SomeViewModel : ViewModelBase
{
    public ICommand SomeCommand { get; }
    // Properties with OnPropertyChanged notifications
}
```

## Database

SQLite database file: `tran.db` (located in Tran.Desktop directory)

Key indexes:
- `idx_documents_state` - Query by document state
- `idx_documents_company` - Query by FromCompanyId, ToCompanyId
- `idx_logs_document` - Audit log lookup

## Korean Language Context

The application uses Korean terminology:
- 거래명세표 = Transaction Statement / Invoice
- 거래처 = Trading Partner / Business Partner
- 정산 = Settlement
- 양식 = Template / Form
- 확정 = Confirmed
- 전송 = Sent
- 수정요청 = Revision Request
