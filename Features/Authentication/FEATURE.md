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

### Presentation

- `Presentation/Pages/SignIn.razor`
- `Presentation/Pages/SignedOut.razor`
- `Presentation/Pages/Account.razor`
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
- `Application/Services/LocalAuthenticationService`

### Infrastructure

- `AuthenticationStateCurrentUserAccessor`
- `LocalAuthenticationDefaults`
- `ReturnUrlSanitizer`
- `Persistence/AuthenticationStoreInitializer`
- `Persistence/SqliteLocalUserRepository`

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
- `Program.cs` composes the feature through `AuthenticationModule`

## Invariants

- Shopping list routes require an authenticated local user.
- Passwords are stored as secure password hashes, never as plaintext.
- The first registered account becomes an administrator.
- Self-service registration closes after the first account is created.
- Additional accounts may only be created by administrators from the authenticated account-management surface.
- Return URLs must stay local to the application.
- Signing out clears the local application cookie before returning the user to a signed-out or sign-in surface.

## Integration Points

- Host composition: `Program.cs`
- App shell: `Components/App.razor`, `Components/Routes.razor`, `Components/Layout/MainLayout.razor`
- Account navigation: `Features/Authentication/Presentation/Components/UserMenu.razor`
- Shopping Lists routes: `Features/ShoppingLists/Presentation/Pages/`

## Evolution Rules

- Keep local credential validation and password hashing inside this feature.
- Expose authenticated-user data through `ICurrentUserAccessor` instead of reading claims throughout the codebase.
- Keep credential persistence in this feature and avoid leaking password-hash details outside it.
- Prefer administrator-managed account creation over re-opening anonymous registration.