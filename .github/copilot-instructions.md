# Copilot Instructions

## Project Overview

**Boodschap** is a Blazor Server grocery list application built on .NET 10. It lets users add, remove, reorder (drag-and-drop), and filter grocery items. "Boodschap" is Dutch for "errand" or "grocery item".

## Tech Stack

- **.NET 10** — Blazor Server with `InteractiveServer` render mode
- **Tailwind CSS v3** — all styling; no Bootstrap, no inline `style=` attributes
- **JavaScript** — none. All drag-and-drop is handled in C# using Blazor's built-in HTML5 drag-and-drop event handlers (`@ondragstart`, `@ondragend`, `@ondragenter`, `@ondrop`, `@ondragover:preventDefault`). Do not add JavaScript files unless strictly necessary for a browser API unavailable in Blazor.
- **Docker** — multi-stage build: Node (Tailwind) → .NET SDK (publish) → .NET ASP.NET runtime

## Project Structure

```
Components/          App shell only: App.razor, Routes.razor, Layout/
Features/            Feature-first vertical slices
  ShoppingLists/     Domain, Application, Infrastructure, Presentation
Shared/              Cross-feature UI and technical building blocks only
Styles/              app.tailwind.css  ← Tailwind source (edit this, not app.css)
docs/                Architecture and development plans
wwwroot/
  app.css            ← Tailwind output (generated, do not edit manually)
Program.cs           ASP.NET host setup and composition root
Dockerfile           Multi-stage container build
```

## Coding Conventions

- **Razor components**: co-locate `@code { }` blocks at the bottom of `.razor` files; no separate `.razor.cs` code-behind unless the file grows very large.
- **C# style**: modern C# — primary constructors, collection expressions `[...]`, pattern matching, `var` where the type is obvious, nullable reference types enabled.
- **Feature boundaries**: new business functionality belongs under `Features/<FeatureName>/` with `Domain`, `Application`, `Infrastructure`, and `Presentation` folders.
- **Shared code**: only move code into `Shared/` when it is truly used by multiple features and contains no feature-specific business language.
- **JavaScript** — none. Do not add JavaScript files unless strictly necessary for a browser API unavailable in Blazor.
- **Tailwind**: write utility classes directly in markup. Run `npm run watch:css` during development to auto-rebuild `wwwroot/app.css`. Run `npm run build:css` for a minified production build.
- **No magic strings for item state** — use the existing filter values `"All"`, `"Needed"`, `"Purchased"`.

## Running Locally

```bash
# Terminal 1 — watch Tailwind
npm run watch:css

# Terminal 2 — run the app
dotnet run
```

## Building the Docker Image

```bash
docker build -t boodschap .
docker run -p 8080:8080 boodschap
```

## What to Keep in Mind

- State is persisted in **SQLite** via EF Core.
- `ConnectionStrings:Boodschap` may be provided either as a full SQLite connection string or as a raw database file path; Docker-related or startup/config changes should be smoke-tested with the raw-path form as well.
- Blazor Server circuits should stay synchronized across sessions. When a list or item changes, prefer store-level notifications so all connected sessions refresh over the existing SignalR/WebSocket connection.
- Drag-and-drop reordering uses HTML5 DnD entirely in C# via Blazor event handlers on the `<li>` elements in `Features/ShoppingLists/Presentation/Pages/ShoppingListPage.razor`. There is no JavaScript file for this.
- Shopping list persistence lives in `Features/ShoppingLists/Infrastructure/Persistence/` and should stay behind feature-level application contracts.
- Do **not** modify `wwwroot/app.css` directly — it is overwritten by Tailwind on every build.
- The app runs behind a reverse proxy; `UseForwardedHeaders` is configured in `Program.cs`.
- After implementing any feature, bugfix, or behavioral change, run the smoke test against the disposable smoke-test database before considering the task complete.
