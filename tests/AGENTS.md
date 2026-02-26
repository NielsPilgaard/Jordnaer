# Tests

## Test Projects

| Project | Framework | Purpose |
|---------|-----------|---------|
| `web/Jordnaer.Tests` | xUnit 2.9.3 | Unit and integration tests |
| `web/Jordnaer.E2E.Tests` | NUnit 4.4 + Playwright 1.57 | E2E browser tests |

## Running Tests

```bash
# Unit tests (CI-safe)
dotnet test tests/web/Jordnaer.Tests --filter Category!=SkipInCi

# Specific test
dotnet test tests/web/Jordnaer.Tests --filter "FullyQualifiedName~TestName"

# E2E tests (requires deployed app)
dotnet test tests/web/Jordnaer.E2E.Tests
```

## Libraries

- **NSubstitute** / **Moq** - Mocking
- **FluentAssertions** - Assertions
- **Bogus** - Fake data generation
- **Testcontainers** - Database integration tests

## Conventions

- Tests that **cannot** run in CI: `[Trait("Category", "SkipInCi")]`
- Tests using external services that **can** run in CI: mark as integration tests
- Use `InternalsVisibleTo` (configured in `Directory.Build.props`) to test internal members
- CI pipeline: `.github/workflows/website_backend_ci.yml`
