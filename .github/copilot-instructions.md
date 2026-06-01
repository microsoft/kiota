---
applyTo:
  - "**"
---

# Kiota Central Copilot Policies

This file defines repository-wide defaults for Copilot behavior. It is always applied.

## Scope and precedence

1. Apply this file for all work in the repository.
2. Also apply any matching files under `.github/instructions/`.
3. If guidance conflicts, follow the more specific instruction file for the target paths.

## Required defaults

1. Keep changes minimal and targeted. Avoid unrelated refactors.
2. Preserve existing style, naming, and public behavior unless the task requires changes.
3. Add or update tests for behavior changes or bug fixes.
4. Before proposing a commit, run relevant tests and ensure they pass.
5. If tests fail, fix issues and re-run tests before considering the work complete.
6. Prefer non-destructive actions and do not revert unrelated local changes.

## Security review defaults

When modifying code generation or writer/refiner logic, treat schema-derived values as untrusted and ensure literal-context sanitization at the emission site.

Use language/literal-appropriate sanitizers from `src/Kiota.Builder/Writers/StringExtensions.cs` (and Dart convention helpers where applicable).

## Writer hardening reminder

For writer changes that emit schema-derived text into generated code, ensure hostile content is escaped and covered by regression tests in `tests/Kiota.Builder.Tests/Writers/`.

## Validation checklist

Before finishing implementation work:

1. Build affected projects.
2. Run targeted tests first, then broader tests if impact is unclear.
3. Confirm no new warnings/errors were introduced by the change.
