# Updates

## Scope

The Updates feature compares the commit embedded in the running application with the latest commit on a configured GitHub branch. A compact block at the bottom of the authenticated account page shows the current and remote commit versions and confirms whether Boodschap is current.

## Configuration

Configuration lives under `Features:Updates`:

- `Enabled` controls feature registration and UI visibility.
- `Owner`, `Repository`, and `Branch` identify the GitHub branch to check.
- `CurrentCommit` can explicitly override the running commit.
- `CacheDurationMinutes` limits GitHub API traffic. The minimum effective duration is one minute.

The build embeds `RepositoryCommit` assembly metadata. Local builds resolve it from `git rev-parse HEAD`. CI can provide `GITHUB_SHA` or `BUILD_COMMIT`; Docker builds can use `--build-arg BUILD_COMMIT=<sha>`. The GitHub Actions publication workflow passes `github.sha` as `BUILD_COMMIT`, so published images identify the exact workflow commit.

## Behavior

- Matching full or abbreviated commit hashes are considered up-to-date.
- A different branch-head commit produces an update link to that commit on GitHub.
- Missing build metadata, invalid responses, and GitHub connectivity failures produce an unavailable status without interrupting the application.
- Results are cached process-wide for the configured duration.