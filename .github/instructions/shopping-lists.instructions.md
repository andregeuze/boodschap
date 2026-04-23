---
name: "Shopping Lists Feature"
description: "Use when working on the Shopping Lists feature, including list routes, filters, item add/remove, archiving, reorder behavior, realtime refresh, SQLite persistence, and Shopping Lists feature tests."
applyTo:
  - "Features/ShoppingLists/**"
  - "tests/Boodschap.Features.ShoppingLists.Tests/**"
---
# Shopping Lists Feature Guidelines

- Preserve the feature boundary defined in `Features/ShoppingLists/FEATURE.md`.
- Use `ShoppingItemFilters` constants for item filter values instead of duplicating string literals.
- Keep realtime notifications in `ShoppingListService` orchestration through `StoreChangeNotifier`, not in Razor pages or repository code.
- Keep drag-and-drop reorder behavior implemented with Blazor C# event handlers in `Presentation/Pages/ShoppingListPage.razor`.
- Keep SQLite persistence behind `IShoppingListRepository` and `IShoppingListService`.
- When adding new workflows, prefer expanding the application layer before expanding page logic.
- Maintain feature tests for ordering, mutation semantics, and feature invariants when changing repository or application behavior.