# Code Style & Conventions

## C# / .NET Backend
- Target: .NET 10, C# 14
- Nullable reference types enabled
- File-scoped namespaces: `namespace X;`
- Primary constructors for DI
- Record types for DTOs, Commands, Queries, Events
- PascalCase (public), _camelCase (private), I prefix for interfaces
- All I/O async with `Async` suffix, accept CancellationToken everywhere
- CQRS: Commands return Result<T> or void, Queries return data
- Feature folders: group by feature not type
- Constructor injection only
- EF Core: no lazy loading, explicit .Include()
- No magic strings — use constants or enums

## Angular / Frontend
- Angular 21+ standalone components (no NgModules)
- Package manager: bun (NEVER npm/yarn)
- Do NOT set `standalone: true` in decorators (default in v20+)
- Signals over RxJS for state
- `@if`, `@for`, `@switch` control flow (never *ngIf, *ngFor)
- spartan.ng: brain/helm pattern (brn-*/hlm-*)
- Tailwind utility-first, no custom CSS unless necessary
- `input()` and `output()` functions, not decorators
- `ChangeDetectionStrategy.OnPush` always
- `inject()` function, not constructor injection
- `providedIn: 'root'` for singleton services
- Kebab-case file naming
- Lazy-loaded feature routes

## Commit Messages
- Format: `type(scope): description`
- Types: feat, fix, refactor, docs, style, test, chore, ci, build
