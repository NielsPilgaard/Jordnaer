# Jordnaer.E2E.Tests

E2E browser tests using **NUnit 4.4 + Playwright 1.57**.

## Running Tests

```bash
# E2E tests (self-contained — no external app or secrets needed)
dotnet test tests/web/Jordnaer.E2E.Tests --filter Category!=SkipInCi
```

## Architecture

### Self-contained — no running app needed

E2E tests boot the real ASP.NET Core app in-process using `E2eWebApplicationFactory` (modelled on `JordnaerWebApplicationFactory` from the unit test project). SQL Server and Azurite are spun up automatically via Testcontainers. **No secrets, no deployed environment, no URL config required.**

### Key infrastructure files

| File | Purpose |
|------|---------|
| `Infrastructure/E2eWebApplicationFactory.cs` | Boots the app + containers, seeds test users |
| `Infrastructure/BrowserExtensions.cs` | Browser/page helpers (login, new page) |
| `Infrastructure/TestConfiguration.cs` | Playwright config (headless, browser, device) |
| `Infrastructure/PageObjectHelpers.cs` | Extension methods to create page objects |
| `Infrastructure/TestCategory.cs` | `UI`, `Authenticated`, `SkipInCi` enum |
| `SetUpFixture.cs` | Global NUnit setup — starts factory, logs in both users |

### Test users

Two users are seeded by `E2eWebApplicationFactory` on every test run. Credentials are hardcoded constants — no config needed:

| Constant | Value |
|----------|-------|
| `E2eWebApplicationFactory.UserAEmail` | `user-a@e2e.test` |
| `E2eWebApplicationFactory.UserAPassword` | `E2eUserA!1` |
| `E2eWebApplicationFactory.UserBEmail` | `user-b@e2e.test` |
| `E2eWebApplicationFactory.UserBPassword` | `E2eUserB!1` |

- **User A** → `SetUpFixture.Context` (auth state in `auth.json`)
- **User B** → `SetUpFixture.ContextB` (auth state in `auth-b.json`)
- **Unauthenticated** → `SetUpFixture.Browser` (no stored state)

### Page Object Model

All page interactions live in `PageObjects/`. Each class receives `IPage` via primary constructor and exposes async action methods plus `ILocator` getters for assertions. Register new page objects in `Infrastructure/PageObjectHelpers.cs`.

| Page Object | File | Key area |
|-------------|------|----------|
| `LoginPage` | `PageObjects/LoginPage.cs` | Login form |
| `LandingPage` | `PageObjects/LandingPage.cs` | Landing page nav/footer |
| `ChatPage` | `PageObjects/ChatPage.cs` | Chat user search + messaging |
| `TopBarPage` | `PageObjects/TopBarPage.cs` | Notification bell, nav links |
| `NotificationsPage` | `PageObjects/NotificationsPage.cs` | Notifications list, mark read |
| `GroupPage` | `PageObjects/GroupPage.cs` | Create/search groups |
| `ProfilePage` | `PageObjects/ProfilePage.cs` | Edit profile fields |
| `PostPage` | `PageObjects/PostPage.cs` | Create/delete posts |

## Adding a new E2E test

1. Create `AuthenticatedTests/MyFeatureTests.cs` (authenticated) or a root-level file (unauthenticated).
2. Inherit `BrowserTest` (from `Microsoft.Playwright.NUnit`).
3. Apply `[Parallelizable(ParallelScope.All)]`, `[TestFixture]`, and `[Category(nameof(TestCategory.UI))]`.
4. For authenticated tests also apply `[Category(nameof(TestCategory.Authenticated))]`.
5. Use `SetUpFixture.Context` (User A), `SetUpFixture.ContextB` (User B), or `SetUpFixture.Browser` (unauthenticated).
6. Use `SetUpFixture.BaseUrl` — never hardcode a URL.
7. If the test is order-dependent or mutates shared state, use `[Parallelizable(ParallelScope.None)]`.
8. Create or reuse a page object in `PageObjects/` — never inline locators directly in tests.

## Important notes

- The `WebApplicationFactory` boots the real Blazor Interactive Server app. The test server uses HTTP (`http://127.0.0.1:<random port>`), not HTTPS.
- `Microsoft.Playwright.Program.Main(["install"])` in `SetUpFixture` handles browser installation; it is a no-op when browsers are already installed.
- `UserManager<ApplicationUser>` is used to create test users so password hashing is correct. `EmailConfirmed = true` is required because `RequireConfirmedAccount = true` in the app's Identity options.
- Hosted services (`IHostedService`) are removed in `E2eWebApplicationFactory.ConfigureWebHost` to prevent background jobs from interfering.
