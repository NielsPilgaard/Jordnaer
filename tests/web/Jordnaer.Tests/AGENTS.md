# Jordnaer.Tests

Unit and integration tests using **xUnit 2.9.3**.

## Running Tests

```bash
# All tests (CI-safe)
dotnet test tests/web/Jordnaer.Tests --filter Category!=SkipInCi

# Specific test
dotnet test tests/web/Jordnaer.Tests --filter "FullyQualifiedName~TestName"
```

## Libraries

- **NSubstitute** / **Moq** - Mocking
- **FluentAssertions** - Assertions
- **Bogus** - Fake data generation
- **Testcontainers** (`MsSql` + `Azurite`) - Ephemeral SQL Server and blob storage containers

## Conventions

- Tests that **cannot** run in CI: `[Category(nameof(TestCategory.SkipInCi))]`
- Use `InternalsVisibleTo` (configured in `Directory.Build.props`) to test internal members
- CI pipeline: `.github/workflows/website_backend_ci.yml`
