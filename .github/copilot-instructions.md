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
Components/          Blazor components (.razor)
  Pages/             Page-level components (routable)
  Layout/            Shell layout, nav, reconnect modal
Styles/              app.tailwind.css  ← Tailwind source (edit this, not app.css)
wwwroot/
  app.css            ← Tailwind output (generated, do not edit manually)
Program.cs           ASP.NET host setup
Dockerfile           Multi-stage container build
```

## Coding Conventions

- **Razor components**: co-locate `@code { }` blocks at the bottom of `.razor` files; no separate `.razor.cs` code-behind unless the file grows very large.
- **C# style**: modern C# — primary constructors, collection expressions `[...]`, pattern matching, `var` where the type is obvious, nullable reference types enabled.
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

- State is currently **in-memory** (no database). If persistence is added, prefer a lightweight option like SQLite via EF Core.
- Drag-and-drop reordering uses HTML5 DnD entirely in C# via Blazor event handlers on the `<li>` elements in `Home.razor`. There is no JavaScript file for this.
- Do **not** modify `wwwroot/app.css` directly — it is overwritten by Tailwind on every build.
- The app runs behind a reverse proxy; `UseForwardedHeaders` is configured in `Program.cs`.
