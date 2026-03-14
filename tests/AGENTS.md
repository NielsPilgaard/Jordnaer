# Tests

## Test Projects

| Project | Framework | Purpose | Docs |
|---------|-----------|---------|------|
| `web/Jordnaer.Tests` | xUnit 2.9.3 | Unit and integration tests | [AGENTS.md](web/Jordnaer.Tests/AGENTS.md) |
| `web/Jordnaer.E2E.Tests` | NUnit 4.4 + Playwright 1.57 | E2E browser tests | [AGENTS.md](web/Jordnaer.E2E.Tests/AGENTS.md) |

## Running Tests

```bash
# Unit tests (CI-safe)
dotnet test tests/web/Jordnaer.Tests --filter Category!=SkipInCi

# E2E tests (self-contained — no external app or secrets needed)
dotnet test tests/web/Jordnaer.E2E.Tests --filter Category!=SkipInCi
```
