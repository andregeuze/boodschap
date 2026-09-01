# Boodschap

Boodschap is a small Blazor Server grocery list app with local username/password authentication.

It lets you:
- sign in with a local account and bootstrap the first administrator
- create and open shopping lists
- add, remove, check off, and reorder grocery items
- switch between active and archived lists
- keep multiple open browser sessions synchronized in real time through Blazor Server updates

Data is stored in SQLite, so the app is easy to run locally and in Docker.

## Sessions

Authentication uses a persistent cookie with a 90-day sliding expiration window. Active use refreshes that window, while inactive sessions expire once the window is exceeded.

ASP.NET Core data-protection keys are stored in the same SQLite database as the authentication feature, in the `DataProtectionKeys` table. That means existing valid auth cookies can survive app restarts and redeployments as long as the same SQLite database is retained.

If a deployment replaces or loses the database file, users will need to sign in again because the app can no longer decrypt previously issued cookies.

## Architecture

The canonical architecture reference lives in [docs/architecture/README.md](docs/architecture/README.md).

That document covers the current repository shape, feature boundaries, shared-code rules, host composition, and SQLite persistence conventions. Feature-specific routes, ownership, and invariants live in:

- [sources/Features/Authentication/FEATURE.md](sources/Features/Authentication/FEATURE.md)
- [sources/Features/Nutrition/FEATURE.md](sources/Features/Nutrition/FEATURE.md)
- [sources/Features/Recipes/FEATURE.md](sources/Features/Recipes/FEATURE.md)
- [sources/Features/ShoppingLists/FEATURE.md](sources/Features/ShoppingLists/FEATURE.md)
- [sources/Features/Updates/FEATURE.md](sources/Features/Updates/FEATURE.md)

## Local Run

Local username/password authentication is now required before the shopping-list routes render.

From the repository root:

```powershell
# Terminal 1
npm --prefix sources/Boodschap run watch:css

# Terminal 2
dotnet run --project sources/Boodschap/Boodschap.csproj --launch-profile http
```

The app will be available at `http://localhost:5091`.

When you open the app, Boodschap will prompt for a local username and password before any shopping list content becomes available.
If no accounts exist yet, use the register form on the sign-in page to create the first account. That first account becomes the administrator. After that, self-service registration closes and administrators create additional accounts from the authenticated account page. Passwords are stored in SQLite as secure password hashes rather than plaintext.

Authenticated users can open `/account` to change their password. Administrators also use `/account` to create additional users and optionally mark them as administrators.

## GitHub Copilot MCP

The hosted app exposes an authenticated Streamable HTTP MCP server at `/mcp`. It lets GitHub Copilot list existing shopping lists and create a new list with optional initial grocery items.

Generate a dedicated 256-bit access key once:

```powershell
$keyBytes = [byte[]]::new(32)
[Security.Cryptography.RandomNumberGenerator]::Fill($keyBytes)
[Convert]::ToBase64String($keyBytes)
```

Provide that value to the hosted process as `Mcp__AccessKey`. For Docker Compose, keep the value in the deployment environment or an untracked `.env` file:

```yaml
services:
  boodschap:
    environment:
      Mcp__AccessKey: ${BOODSCHAP_MCP_ACCESS_KEY}
```

The workspace MCP configuration in `.vscode/mcp.json` points Copilot at `https://boodschap.geuze.dev/mcp` and securely prompts for the same key. The endpoint returns `401 Unauthorized` when the key is missing, incorrect, or not configured on the server.

## Docker

By default, the container uses SQLite at `/app/App_Data/boodschap.db`.

### Option 1: Build Locally

Build the image from this repository:

```powershell
docker build -t boodschap .
```

Run the locally built image:

```powershell
docker run -p 8080:8080 boodschap
```

### Option 2: Use the Prebuilt GitHub Image

Pull and run the published image from GitHub Container Registry:

```powershell
docker pull ghcr.io/andregeuze/boodschap:latest
docker run -p 8080:8080 ghcr.io/andregeuze/boodschap:latest
```

## Docker Compose

### Option 1: Build Locally

Use this when you want Docker Compose to build the image from the local source code in this repository.

```yaml
services:
  boodschap:
    build: .
    container_name: boodschap
    ports:
      - "8080:8080"
    volumes:
      - /srv/boodschap/appdata:/app/App_Data
    restart: unless-stopped
```

In this example:
- the SQLite database file inside the container stays at the default path: `/app/App_Data/boodschap.db`
- the host folder `/srv/boodschap/appdata` is mounted directly to `/app/App_Data`
- everything written to `App_Data` is persisted on the host filesystem
- that persisted folder now includes both app data and the data-protection key ring used to keep auth cookies valid across container restarts and redeploys

You can replace `/srv/boodschap/appdata` with any path you want to use on your server, NAS, or external storage.

### Option 2: Use the Prebuilt GitHub Image

Use this when you want Docker Compose to pull the published image directly from GitHub Container Registry instead of building it yourself.

```yaml
services:
  boodschap:
    image: ghcr.io/andregeuze/boodschap:latest
    container_name: boodschap
    ports:
      - "8080:8080"
    volumes:
      - /srv/boodschap/appdata:/app/App_Data
    restart: unless-stopped
```

  This uses the same persistent `App_Data` mapping as the local-build example, but skips the local build step and pulls `ghcr.io/andregeuze/boodschap:latest` directly.