# Updates

## Scope

The Updates feature compares the commit embedded in the running application with the latest commit on a configured GitHub branch. A compact block at the bottom of the authenticated account page shows the current and remote commit versions and confirms whether Boodschap is current.

## Configuration

Configuration lives under `Features:Updates`:

- `Enabled` controls feature registration and UI visibility.
- `Owner`, `Repository`, and `Branch` identify the GitHub branch to check.
- `CurrentCommit` can explicitly override the running commit.
- `CacheDurationMinutes` controls both the successful-result cache and the background check interval. The minimum effective duration is one minute.

The build embeds `RepositoryCommit` assembly metadata. Local builds resolve it from `git rev-parse HEAD`. CI can provide `GITHUB_SHA` or `BUILD_COMMIT`; Docker builds can use `--build-arg BUILD_COMMIT=<sha>`. The GitHub Actions publication workflow passes `github.sha` as `BUILD_COMMIT`, so published images identify the exact workflow commit.

## Behavior

- Matching full or abbreviated commit hashes are considered up-to-date.
- A different branch-head commit produces an update link to that commit on GitHub.
- GitHub requests time out after ten seconds and transient HTTP failures are retried once after a short delay.
- Missing build metadata, invalid responses, and GitHub connectivity failures produce an unavailable status without interrupting the application or logging an exception stacktrace.
- Unavailable results are not cached, so a later visit can recover after a temporary GitHub failure.
- A hosted background service checks immediately when the application starts and then every configured cache interval.
- Results are cached process-wide for the configured duration. The account page normally reads that shared cached result instead of generating extra GitHub traffic.
- In a future MAUI host, each running app installation can register the same services and maintain its own cache. Android may suspend in-process timers while the app is fully backgrounded, so closed-app scheduling requires an Android-specific scheduler.