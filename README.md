# Boodschap

Boodschap is a small Blazor Server grocery list app.

It lets you:
- create and open shopping lists
- add, remove, check off, and reorder grocery items
- switch between active and archived lists
- keep multiple open browser sessions synchronized in real time through Blazor Server updates

Data is stored in SQLite, so the app is easy to run locally and in Docker.

## Local Run

From the project root:

```powershell
# Terminal 1
npm run watch:css

# Terminal 2
dotnet run --launch-profile http
```

The app will be available at `http://localhost:5091`.

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