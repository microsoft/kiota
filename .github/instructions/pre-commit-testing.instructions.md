---
applyTo:
  - "**"
---

# Pre-Commit Testing Requirements

Before creating any git commit, agents **must** run the relevant tests and verify they pass.

## Rules

1. **Always run tests before committing.** Never commit code that has not been validated by running the relevant test suite.
2. **Scope tests appropriately.** Run at minimum the tests related to the files you changed. If unsure which tests cover your changes, run the full test project that contains the modified code.
3. **Fix failing tests before committing.** If tests fail, diagnose and fix the issue. Do not commit with known test failures unless explicitly instructed by the user.
4. **Re-run tests after fixing failures.** After making corrections, run tests again to confirm they pass.

## Test Commands

| Project | Command |
|---------|---------|
| Kiota.Builder | `dotnet test tests/Kiota.Builder.Tests/Kiota.Builder.Tests.csproj` |
| VS Code Extension | `cd vscode/packages && npm test` |

Use `--filter "FullyQualifiedName~ClassName"` to scope .NET tests to specific test classes when a full run is unnecessary.
