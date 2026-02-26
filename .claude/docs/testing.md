# Testing Guide

## Test Projects

| Project | Framework | Purpose |
|---------|-----------|---------|
| `tests/web/Jordnaer.Tests` | xUnit 2.9.3 | Unit tests |
| `tests/web/Jordnaer.E2E.Tests` | NUnit 4.4 + Playwright 1.57 | E2E browser tests |

## Running Tests

```bash
# All unit tests (excludes SkipInCi)
dotnet test tests/web/Jordnaer.Tests --filter Category!=SkipInCi

# Specific test by name
dotnet test tests/web/Jordnaer.Tests --filter "FullyQualifiedName~YourTestName"

# E2E tests (requires running app at https://mini-moeder.dk or local)
dotnet test tests/web/Jordnaer.E2E.Tests
```

## Test Libraries

- **NSubstitute** / **Moq** - Mocking
- **FluentAssertions** - Assertions
- **Bogus** - Fake data generation
- **Testcontainers** - Database integration tests

## Conventions

- Tests that require external services we **cannot** run in CI should be marked with `[Trait("Category", "SkipInCi")]`
- Tests that use external services but **can** run should be marked as integration tests
- CI runs: `dotnet test --filter Category!=SkipInCi`
- Unit test project has `InternalsVisibleTo` access (configured in `Directory.Build.props`)

## CI Pipeline

Tests run automatically on all pushes and PRs to main via `.github/workflows/website_backend_ci.yml`.
E2E tests run after deployment via `.github/workflows/website_frontend_ci.yml`.
