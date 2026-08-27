# Copilot Instructions

## Project Overview

**Boodschap** is a Blazor Server grocery list application built on .NET 10. It requires local username/password authentication and lets users add, remove, reorder (drag-and-drop), and filter grocery items. "Boodschap" is Dutch for "errand" or "grocery item".

## Tech Stack

- **.NET 10** — Blazor Server with `InteractiveServer` render mode
- **Tailwind CSS v3** — all styling; no Bootstrap, no inline `style=` attributes
- **JavaScript** — none. All drag-and-drop is handled in C# using Blazor's built-in HTML5 drag-and-drop event handlers (`@ondragstart`, `@ondragend`, `@ondragenter`, `@ondrop`, `@ondragover:preventDefault`). Do not add JavaScript files unless strictly necessary for a browser API unavailable in Blazor.
- **Docker** — multi-stage build: Node (Tailwind) → .NET SDK (publish) → .NET ASP.NET runtime

## Project Structure

```
sources/
  Boodschap/          Web host, app shell, configuration, Tailwind, static assets
  Features/          Feature-first vertical slices
    Authentication/  One Razor project containing Domain, Application, Infrastructure, Presentation
    ShoppingLists/   One Razor project containing Domain, Application, Infrastructure, Presentation
    Nutrition/       One Razor project containing Domain, Application, Infrastructure, Presentation
    Recipes/         One Razor project containing Domain, Application, Infrastructure, Presentation
    Updates/         One Razor project containing Domain, Application, Infrastructure, Presentation
  Shared/            Shared Razor project for cross-feature technical building blocks
  Directory.Build.props
tests/               Feature-aligned test projects and smoke-test docs
docs/                Architecture and development plans
Dockerfile           Multi-stage container build
```

## Coding Conventions

- **Razor components**: co-locate `@code { }` blocks at the bottom of `.razor` files; no separate `.razor.cs` code-behind unless the file grows very large.
- **C# style**: modern C# — primary constructors, collection expressions `[...]`, pattern matching, `var` where the type is obvious, nullable reference types enabled.
- **Feature boundaries**: new business functionality belongs in one `sources/Features/<FeatureName>/Boodschap.Features.<FeatureName>.csproj` Razor project with `Domain`, `Application`, `Infrastructure`, and `Presentation` folders. Do not create projects per layer. Follow the current feature layout by splitting richer application layers into `Application/Contracts/` and `Application/Services/`.
- **Project dependencies**: Shared references no feature. Features must not reference another feature's Infrastructure or Presentation. Nutrition may reference Authentication Application contracts; Recipes may reference Nutrition Application/Domain. The Web host composes all features explicitly.
- **Shared code**: only move code into `sources/Shared/` when it is truly used by multiple features and contains no feature-specific business language.
- **Language**: write source code, identifiers, comments, documentation, and tests in English. User-facing UI text must be Dutch through .NET localization resources; only Dutch resources are implemented for now. Treat `Boodschap` as the product name and do not localize it unless the user explicitly asks for another language.
- **JavaScript** — none. Do not add JavaScript files unless strictly necessary for a browser API unavailable in Blazor.
- **Tailwind**: write utility classes directly in markup. From the repository root, run `npm --prefix sources/Boodschap run watch:css` during development to auto-rebuild `sources/Boodschap/wwwroot/app.css`. Run `npm --prefix sources/Boodschap run build:css` for a minified production build.
- **Circular icon buttons**: use Tailwind utility classes with `inline-flex`, fixed equal `h-*`/`w-*`, `rounded-full`, subtle `ring-*`/`shadow-*`, hover and `focus-visible` states, and a real inline SVG icon. Keep visible text out of icon-only buttons; use `aria-label` for the accessible name. Do not use raw text glyphs like `+`, `×`, or `✓` for polished action buttons.
- **No magic strings for item state** — use the existing filter values `"All"`, `"Needed"`, `"Purchased"`.

## Running Locally

```bash
# Terminal 1 — watch Tailwind
npm --prefix sources/Boodschap run watch:css

# Terminal 2 — run the app
dotnet run --project sources/Boodschap/Boodschap.csproj
```

## Running Tests

When running `dotnet test`, always redirect test build output away from the app's normal `bin/` folder so `dotnet watch` or `dotnet run` can keep serving the app without file-lock conflicts:

```powershell
$out = Join-Path $env:TEMP 'boodschap-test-bin'
dotnet test tests/Boodschap.Features.ShoppingLists.Tests/Boodschap.Features.ShoppingLists.Tests.csproj /p:OutputPath="$out\"
```

Use the same `/p:OutputPath="$out\"` pattern for focused test runs and future feature work. Do not stop `dotnet watch` or `dotnet run` just to run tests unless a task explicitly requires validating the normal publish/build output.

## Playwright MCP Artifacts

- Always run browser-based testing in a visible VS Code integrated browser tab so the user can watch the test. After the app is ready, use `open_browser_page` as the first browser action and keep that tab open for the entire test run.
- Do not launch an external browser window or use a headless browser session. Do not silently fall back to either mode; if the VS Code integrated browser is unavailable, report the blocker before continuing with browser testing.
- Prefer the VS Code integrated browser automation tools for navigation and interaction. Only use `mcp_playwright_browser_*` tools when they operate on the visible VS Code browser page rather than starting a separate hidden or external browser session.
- Before taking the first Playwright MCP screenshot in a test run, create one run directory using `.artifacts/<YYYY-MM-DD>-<test-run-id>/screenshots/`. Use the current local date and a short unique test-run ID, for example `.artifacts/2026-08-27-a1b2c3/screenshots/`.
- Pass a path inside that run's `screenshots/` directory to every Playwright MCP screenshot call. Use concise, descriptive file names such as `shopping-list-after-add.png`.
- Reuse the same dated test-run directory for all screenshots from the same browser test run. Start a new test-run ID for each separate run.
- Do not store Playwright MCP screenshots in the repository root, `.playwright-mcp/`, or directly under `.artifacts/`.

## Building the Docker Image

```bash
docker build -t boodschap .
docker run -p 8080:8080 boodschap
```

## What to Keep in Mind

- State is persisted in **SQLite** via EF Core.
- Authentication persistence and bootstrap initialization live under `sources/Features/Authentication/Infrastructure/Persistence/`, and feature-facing auth contracts live under `sources/Features/Authentication/Application/Contracts/`.
- `ConnectionStrings:Boodschap` may be provided either as a full SQLite connection string or as a raw database file path; Docker-related or startup/config changes should be smoke-tested with the raw-path form as well.
- Blazor Server circuits should stay synchronized across sessions. When a list or item changes, prefer store-level notifications so all connected sessions refresh over the existing SignalR/WebSocket connection.
- Drag-and-drop reordering uses HTML5 DnD entirely in C# via Blazor event handlers on the `<li>` elements in `sources/Features/ShoppingLists/Presentation/Pages/ShoppingListPage.razor`. There is no JavaScript file for this.
- Shopping list persistence lives in `sources/Features/ShoppingLists/Infrastructure/Persistence/` and should stay behind feature-level application contracts in `sources/Features/ShoppingLists/Application/Contracts/`.
- Do **not** modify `sources/Boodschap/wwwroot/app.css` directly — it is overwritten by Tailwind on every build.
- The app runs behind a reverse proxy; `UseForwardedHeaders` is configured in `sources/Boodschap/Program.cs`.
- After implementing any feature, bugfix, or behavioral change, run the smoke test against the disposable smoke-test database before considering the task complete.
