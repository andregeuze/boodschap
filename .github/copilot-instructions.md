# Copilot Instructions

## Project Overview

**Boodschap** is a Blazor Server grocery list application built on .NET 10. It lets users add, remove, reorder (drag-and-drop), and filter grocery items. "Boodschap" is Dutch for "errand" or "grocery item".

## Tech Stack

- **.NET 10** — Blazor Server with `InteractiveServer` render mode
- **Tailwind CSS v3** — all styling; no Bootstrap, no inline `style=` attributes
- **Vanilla JavaScript** — only for browser APIs unavailable in Blazor (e.g. drag-and-drop via Pointer Events in `wwwroot/js/groceryList.js`)
- **Docker** — multi-stage build: Node (Tailwind) → .NET SDK (publish) → .NET ASP.NET runtime

## Project Structure

```
Components/          Blazor components (.razor)
  Pages/             Page-level components (routable)
  Layout/            Shell layout, nav, reconnect modal
Styles/              app.tailwind.css  ← Tailwind source (edit this, not app.css)
wwwroot/
  app.css            ← Tailwind output (generated, do not edit manually)
  js/                Vanilla JS modules
Program.cs           ASP.NET host setup
Dockerfile           Multi-stage container build
```

## Coding Conventions

- **Razor components**: co-locate `@code { }` blocks at the bottom of `.razor` files; no separate `.razor.cs` code-behind unless the file grows very large.
- **C# style**: modern C# — primary constructors, collection expressions `[...]`, pattern matching, `var` where the type is obvious, nullable reference types enabled.
- **Tailwind**: write utility classes directly in markup. Run `npm run watch:css` during development to auto-rebuild `wwwroot/app.css`. Run `npm run build:css` for a minified production build.
- **JavaScript**: keep JS minimal and scoped to `window.groceryList.*`. Call JS from Blazor via `IJSRuntime`. Avoid jQuery or external JS libraries.
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

- State is currently **in-memory** (no database). If persistence is added, prefer a lightweight option like SQLite via EF Core.
- The drag-and-drop logic lives entirely in `wwwroot/js/groceryList.js` and communicates back to Blazor via `[JSInvokable]` methods.
- Do **not** modify `wwwroot/app.css` directly — it is overwritten by Tailwind on every build.
- The app runs behind a reverse proxy; `UseForwardedHeaders` is configured in `Program.cs`.
