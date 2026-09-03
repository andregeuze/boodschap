# Authentication Feature

## Purpose

Authentication owns sign-in, sign-out, first-user bootstrap registration, local account management, authenticated user access, and the route protection needed to keep Boodschap behind local username/password login.

## Owned Surface

### Routes

- `/sign-in`
- `/signed-out`
- `/account`
- `/account/login`
- `/account/register`
- `/account/logout`

### Mobile API

- `POST /api/auth/login`
- `POST /api/auth/refresh`
- `GET /api/auth/me`
- `POST /api/auth/password`
- `POST /api/auth/users`

The API does not expose self-service registration. Authenticated mobile users can change their own password, and administrators can create additional accounts. Mobile sign-out removes the protected access and refresh tokens from device SecureStorage. The built-in opaque bearer-token handler has no revocation store, so there is no server-side logout or revoke endpoint.

### Presentation

- `Presentation/Pages/SignIn.razor`
- `Presentation/Pages/SignedOut.razor`
- `Presentation/Components/AccountSettings.razor`
- `Presentation/Components/SignInGate.razor`
- `Presentation/Components/UserMenu.razor`
- `Presentation/AuthenticationEndpoints.cs`

### Application

- `Application/Contracts/CurrentUser`
- `Application/Contracts/ICurrentUserAccessor`
- `Application/Contracts/ILocalAuthenticationService`
- `Application/Contracts/ILocalUserRepository`
- `Application/Contracts/LocalAuthenticationErrorCodes`
- `Application/Contracts/LocalAuthenticationResult`
- `Application/Contracts/LocalPasswordChangeResult`
- `Application/Contracts/Api/` transport and token-store contracts
- `Application/Services/LocalAuthenticationService`

### Infrastructure

- `AuthenticationStateCurrentUserAccessor`
- `LocalAuthenticationDefaults`
- `ReturnUrlSanitizer`
- `Persistence/AuthenticationDbContext`
- `Persistence/AuthenticationStoreInitializer`
- `Persistence/LocalUserRepository`
- `Remote/RemoteAuthenticationClient`
- `Infrastructure/Persistence/Migrations/`
- SQLite-backed ASP.NET Core data-protection key storage for auth cookie and antiforgery continuity across restarts/redeployments

## Domain Language

- authenticated user
- local account
- administrator
- bootstrap registration
- username/password sign-in
- return URL

Keep these terms inside the feature unless another feature genuinely needs them.

## Architectural Boundary

This feature follows the same feature-first boundary as the rest of the app:

- `Presentation` depends on `Application`
- `Infrastructure` depends on `Application`
- `sources/Boodschap/Program.cs` composes the feature through `AuthenticationModule`
- the host-owned `/account` page composes `AccountSettings` with optional cross-feature content

## Invariants

- Shopping list routes require an authenticated local user.
- Authentication cookies are issued as persistent cookies with a 90-day sliding expiration window, and active use refreshes that window so signed-in sessions survive browser restarts until inactivity exceeds it.
- Passwords are stored as secure password hashes, never as plaintext.
- The first registered account becomes an administrator.
- Self-service registration closes after the first account is created.
- Additional accounts may only be created by administrators from the authenticated account-management surface.
- Return URLs must stay local to the application.
- Signing out clears the local application cookie before returning the user to a signed-out or sign-in surface.
- The data-protection key ring is persisted in the authentication SQLite store in the `DataProtectionKeys` table so existing valid auth cookies remain decryptable after app restarts or redeployments that keep the database.
- Development startup seeds a local admin login for quick local access when the account is missing.
- Mobile credential verification uses the same `ILocalAuthenticationService` as cookie sign-in; the mobile API never reads password hashes directly.
- Mobile API routes explicitly require the `Boodschap.MobileBearer` scheme. Cookie authentication remains the default for the hosted Blazor UI.
- Access tokens expire after one hour and refresh tokens after 30 days. Both are opaque Data Protection tickets protected by the persisted authentication key ring.
- Login and refresh are limited to ten requests per minute per forwarded client IP.
- Mobile registration is always closed; additional account creation remains administrator-only on both hosted web and mobile.

## Integration Points

- Host composition: `sources/Boodschap/Program.cs`
- Native client composition: `sources/Boodschap.Mobile/MauiProgram.cs`
- Native sign-in parity: `sources/Boodschap.Mobile/Presentation/Views/LoginView.xaml`
- App shell: `sources/Boodschap/Components/App.razor`, `sources/Boodschap/Components/Routes.razor`, `sources/Boodschap/Components/Layout/MainLayout.razor`
- Account route composition: `sources/Boodschap/Components/Pages/Account.razor`
- Account navigation: `sources/Features/Authentication/Presentation/Components/UserMenu.razor`
- Shopping Lists routes: `sources/Features/ShoppingLists/Presentation/Pages/`

## Evolution Rules

- Keep local credential validation and password hashing inside this feature.
- Keep bearer tickets opaque and Data Protection-backed; do not introduce separate JWT signing without a concrete interoperability requirement.
- Keep mobile token persistence behind `IApiTokenStore`; the MAUI implementation stores tokens only in platform SecureStorage.
- Expose authenticated-user data through `ICurrentUserAccessor` instead of reading claims throughout the codebase.
- Keep EF Core credential persistence in this feature and avoid leaking password-hash details outside it.
- Keep persisted data-protection keys in the same durable store as the authentication feature when changing hosting or deployment topology; if the SQLite database is replaced, existing cookies will no longer be valid.
- Keep authentication migrations isolated in the feature-owned history table `__AuthenticationMigrationsHistory`; do not merge feature histories into the default EF Core history table.
- Prefer administrator-managed account creation over re-opening anonymous registration.
