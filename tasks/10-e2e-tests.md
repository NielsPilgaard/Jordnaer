# Task: Expand E2E Test Coverage

## Goal

Expand the existing Playwright E2E test suite to give confidence that the application works correctly before each release. Tests must pass in CI (GitHub Actions) and cover all key user flows, including multi-user interactions. **Tests run against an isolated, ephemeral database — no production or staging environment is involved.**

---

## Framework Decision: Stay with C# Playwright

The existing C# Playwright infrastructure has critical gaps (see below), but **the framework choice should stay C#**, not switch to JavaScript/TypeScript. The reason is the core requirement: booting the real ASP.NET Core app with Testcontainers SQL Server via `WebApplicationFactory<Program>`. In C# this is a single in-process class. In JS Playwright you would need a separate .NET subprocess to manage container startup, apply migrations, seed users, and pass the dynamic URL back across a process boundary — significantly more complexity for no gain. The test authoring API is identical between C# and JS Playwright.

### Known problems with the existing infrastructure (fix these, don't work around them)

1. **All authenticated tests are permanently `[Category(SkipInCi)]`** — `ChatTests` and `AuthenticatedTopBarTests` have never run in CI. This tag must be removed from all tests once `E2eWebApplicationFactory` is in place (since there is no longer any dependency on a running external app or production credentials).
2. **Tests hit production** — `ChatTests` searches for `"Niels Pilgaard Grøndahl"`, a real user. These tests must be rewritten to use the seeded test users.
3. **`TopBarTests.cs`** (root-level) is empty with a TODO — update it or delete it; the logic lives in `AuthenticatedTests/AuthenticatedTopBarTests.cs`.

---

## Tech Stack (do not change)

- **Framework**: C# Playwright with NUnit (`tests/web/Jordnaer.E2E.Tests/`)
- **Pattern**: Page Object Model — all page interactions go in `PageObjects/`
- **Auth**: Single global login via `SetUpFixture.cs`, state persisted to `auth.json`
- **Config**: `TestConfiguration` reads from `appsettings.json` + UserSecrets
- **Categories**: `TestCategory.UI`, `TestCategory.Authenticated` (drop `SkipInCi` from authenticated tests after factory is in place)
- **Parallelism**: `[Parallelizable(ParallelScope.All)]` on each `[TestFixture]`

---

## Existing Coverage (do not duplicate)

- `LoginTests` — login form, external provider buttons, reset/create account links
- `LandingPageTests` — navigation, footer links
- `ScreenResolutionTests` — 17 viewports with screenshots
- `ChatTests` — user search, send message, clear input, footer visibility
- `AuthenticatedTopBarTests` — topbar navigation when logged in

---

## Infrastructure Change: Isolated App + Database via WebApplicationFactory

The current E2E tests hit an already-running app via `BaseUrl`. Replace this with an in-process `WebApplicationFactory<Program>` that boots the real app with ephemeral Testcontainers infrastructure — **no credentials, no secrets, no running app required**.

The unit test project (`tests/web/Jordnaer.Tests/`) already does this with `JordnaerWebApplicationFactory`. The E2E project needs the same approach.

### 1. Add NuGet packages to `Jordnaer.E2E.Tests.csproj`

```xml
<PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="10.0.*" />
<PackageReference Include="Testcontainers.MsSql" Version="4.*" />
<PackageReference Include="Testcontainers.Azurite" Version="4.*" />
```

Use the same versions that are already in `Jordnaer.Tests.csproj`.

### 2. Create `Infrastructure/E2eWebApplicationFactory.cs`

Model it on `JordnaerWebApplicationFactory` from the unit test project, with these additions:

- After `InitializeAsync()` starts the containers and applies migrations, **seed two test users** directly into the database via `IDbContextFactory<JordnaerDbContext>` (or EF Core directly). Use ASP.NET Core Identity's `UserManager<JordnaerUser>` to create the users so that password hashing is correct.
- Expose the server address via `Server.BaseAddress` so `SetUpFixture` can read it.
- Use a fake `AzureEmailService` connection string (same fake value already used in the unit tests).
- Remove `IHostedService` registrations to prevent background jobs from interfering.

Seed data:

```csharp
// Hardcoded test users — seeded into the ephemeral database at startup
public const string UserAEmail    = "user-a@e2e.test";
public const string UserAPassword = "E2eUserA!1";
public const string UserBEmail    = "user-b@e2e.test";
public const string UserBPassword = "E2eUserB!1";
```

These replace all `TestConfiguration.Username` / `TestConfiguration.Password` references. Remove those properties from `PlaywrightOptions` (and from `appsettings.json`).

### 3. Update `SetUpFixture.cs`

- Instantiate and start `E2eWebApplicationFactory` in `[OneTimeSetUp]`. Call `InitializeAsync()` (it is `IAsyncLifetime`).
- Read `BaseUrl` from `factory.Server.BaseAddress.ToString()` instead of `TestConfiguration.Values.BaseUrl`.
- Log in as User A (using `E2eWebApplicationFactory.UserAEmail` / `UserAPassword`) and save state to `auth.json` — same as before.
- Log in as User B (using `UserBEmail` / `UserBPassword`) and save state to `auth-b.json` — new, follows the same pattern.
- In `[OneTimeTearDown]`, call `factory.DisposeAsync()` after closing the browser.

### 4. Update `TestConfiguration` / `PlaywrightOptions`

- Remove `Username`, `Password`, `SecondUsername`, `SecondPassword` — credentials are now constants in `E2eWebApplicationFactory`.
- Remove `BaseUrl` from config (or keep it as an optional override for local development against a running app, defaulting to `null`). `SetUpFixture` sets the URL from the factory.

---

## Test Users

Two seeded users are available in every test run. Their credentials are constants — no secrets or config needed:

| Constant | Value |
|---|---|
| `E2eWebApplicationFactory.UserAEmail` | `user-a@e2e.test` |
| `E2eWebApplicationFactory.UserAPassword` | `E2eUserA!1` |
| `E2eWebApplicationFactory.UserBEmail` | `user-b@e2e.test` |
| `E2eWebApplicationFactory.UserBPassword` | `E2eUserB!1` |

- **User A** — `auth.json` — used by `SetUpFixture.Context` (existing pattern)
- **User B** — `auth-b.json` — used by a new `SetUpFixture.ContextB` for multi-user tests

---

## Tests to Add

### 1. `TopBarTests` (update the existing empty file)

- Unauthenticated topbar shows Login and Register links
- Authenticated topbar shows user avatar / username
- Authenticated topbar shows notification bell icon
- Notification count badge appears when there are unread notifications

### 2. `NotificationTests` (new file, `AuthenticatedTests/NotificationTests.cs`)

These tests use both User A (`Context`) and User B (`ContextB`).

- **Receive notification**: User A performs an action that triggers a notification for User B (e.g. sends a chat message or invites B to a group). Assert that User B's notification bell badge count increases.
- **Mark as read**: User B opens the notifications dropdown/page and marks a notification as read. Assert the badge count decreases.
- **Clear all**: User B clears all notifications. Assert the badge count reaches zero.

### 3. `GroupTests` (new file, `AuthenticatedTests/GroupTests.cs`)

- **Create group**: User A creates a group with a name, description, and category. Assert the group appears in User A's group list.
- **Search group**: Navigate to the group search page, search by the group name created above. Assert the group appears in results.
- **Send join request / invite**: User A invites User B to the group (or User B requests to join). Assert that a notification or pending-membership entry is visible.
- **Accept membership**: User A (as group owner) accepts User B's membership. Assert User B now appears in the members list.

### 4. `ProfileTests` (new file, `AuthenticatedTests/ProfileTests.cs`)

- **View own profile**: Navigate to the profile page. Assert name, bio, and children info are visible.
- **Edit profile**: Update the display name field, save. Assert the updated name is reflected on the page.
- **Upload profile image**: Upload a valid image. Assert the avatar updates. (Azurite is running via Testcontainers so this does not need `SkipInCi`.)

### 5. `PostTests` (new file, `AuthenticatedTests/PostTests.cs`)

- **Create post**: User A creates a post with text content. Assert the post appears in the feed.
- **View post**: Navigate to the post. Assert the content matches what was entered.
- **Delete post**: User A deletes the post. Assert it is no longer visible in the feed.

---

## Page Objects to Add

Create a `PageObject` class for each new area that does not already have one:

| Page Object | File | Responsibilities |
|---|---|---|
| `TopBarPage` | `PageObjects/TopBarPage.cs` | Notification bell, badge count, avatar, nav links |
| `NotificationsPage` | `PageObjects/NotificationsPage.cs` | Open panel, read/unread items, mark read, clear all |
| `GroupPage` | `PageObjects/GroupPage.cs` | Create, search, invite, accept/reject membership |
| `ProfilePage` | `PageObjects/ProfilePage.cs` | View fields, edit name/bio, upload image |
| `PostPage` | `PageObjects/PostPage.cs` | Create post, view feed, delete post |

Follow the pattern of the existing `ChatPage.cs` — expose async methods that take an `IPage` argument and encapsulate all locators inside the class. Register each new page object in `Infrastructure/PageObjectHelpers.cs`.

---

## CI Integration

The tests already use `GitHubActionsTestLogger`. Because everything spins up via Testcontainers, **no environment-specific secrets are needed** — only Docker must be available on the runner (it is by default on `ubuntu-latest`).

Add this step to the GitHub Actions workflow (`.github/workflows/`) before the deploy step:

```yaml
- name: Install Playwright browsers
  run: pwsh tests/web/Jordnaer.E2E.Tests/bin/Release/net10.0/playwright.ps1 install --with-deps chromium

- name: Run E2E tests
  run: dotnet test tests/web/Jordnaer.E2E.Tests --filter "Category!=SkipInCi" --logger GitHubActions
  env:
    Playwright__Headless: "true"
```

No secrets block required.

---

## Acceptance Criteria

- [ ] `E2eWebApplicationFactory` starts the real app with Testcontainers SQL Server + Azurite and seeds User A and User B
- [ ] `SetUpFixture` derives `BaseUrl` from the factory's `Server.BaseAddress` — no URL config needed
- [ ] `auth.json` (User A) and `auth-b.json` (User B) are both created in global setup
- [ ] `Username` / `Password` fields are removed from `PlaywrightOptions` and `appsettings.json`
- [ ] All new tests pass locally with `dotnet test tests/web/Jordnaer.E2E.Tests --filter "Category!=SkipInCi"`
- [ ] No existing tests are broken
- [ ] Each new test file follows the existing conventions (NUnit, `[Parallelizable]`, Page Object Model, `TestCategory`)
- [ ] Multi-user tests use `ContextB` (loaded from `auth-b.json`) and do not share browser context with User A
- [ ] The GitHub Actions workflow runs without any E2E-specific secrets
- [ ] The build fails if E2E tests fail
