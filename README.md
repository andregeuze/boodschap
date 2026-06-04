# Boodschap

Boodschap is a small Blazor Server grocery list app with local username/password authentication.

It lets you:
- sign in with a local account and bootstrap the first administrator
- create and open shopping lists
- add, remove, check off, and reorder grocery items
- switch between active and archived lists
- keep multiple open browser sessions synchronized in real time through Blazor Server updates

Data is stored in SQLite, so the app is easy to run locally and in Docker.

Each feature-owned EF Core `DbContext` keeps its own migration history table in that shared SQLite database:
- Authentication uses `__AuthenticationMigrationsHistory`
- Shopping Lists uses `__ShoppingListsMigrationsHistory`

The repository does not use the default EF Core history table `__EFMigrationsHistory` as part of its intended design.

## Project Structure

The repository uses feature-first vertical slices.

```text
Components/          App shell, routing, and layout only
Features/
  Authentication/
    Application/
      Contracts/
      Services/
    Domain/
    Infrastructure/
    Presentation/
  ShoppingLists/
    Application/
      Contracts/
      Services/
    Domain/
    Infrastructure/
    Presentation/
Shared/              Cross-feature building blocks
Styles/              Tailwind source files
tests/               Feature test projects and smoke-test docs
Program.cs           Composition root
```

`Program.cs` composes the current features through `AuthenticationModule` and `ShoppingListsModule`.

Because each feature owns its own migration history table, changing that table name is a database-shape change. Existing local databases created against an older history-table convention should be recreated unless you intentionally migrate their history rows yourself.

## Local Run

Local username/password authentication is now required before the shopping-list routes render.

From the project root:

```powershell
# Terminal 1
npm run watch:css

# Terminal 2
dotnet run --launch-profile http
```

The app will be available at `http://localhost:5091`.

When you open the app, Boodschap will prompt for a local username and password before any shopping list content becomes available.
If no accounts exist yet, use the register form on the sign-in page to create the first account. That first account becomes the administrator. After that, self-service registration closes and administrators create additional accounts from the authenticated account page. Passwords are stored in SQLite as secure password hashes rather than plaintext.

Authenticated users can open `/account` to change their password. Administrators also use `/account` to create additional users and optionally mark them as administrators.

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