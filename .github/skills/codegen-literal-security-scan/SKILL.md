---
name: codegen-literal-security-scan
description: Detect and remediate code-generation literal injection risks in Kiota writer code (wire names, serialization names, URL templates, base URLs, defaults, and path parameter keys).
---

Use this skill when auditing or modifying any code generator writer logic.

## Goal

Prevent generated-source injection (including potential RCE chains) by ensuring every untrusted schema-derived value is escaped for the destination literal context before being emitted.

## Focus surfaces

Prioritize all literal-emission paths under `src/Kiota.Builder/Writers/**/*.cs`, especially:

- `WireName`
- `SerializationName`
- `UrlTemplateOverride`
- `BaseUrl`
- `DefaultValue`
- `IndexParameter.SerializationName`
- Path parameter map keys and query mapper outputs

## Required checks

1. Locate candidate sinks:
   - search for interpolation/writes that include the focus surfaces and emit language string literals (`WriteLine`, `WriteLines`, `StartBlock`, doc attributes/tags).
2. Verify context-aware escaping is applied at the emission site:
   - double-quoted literal contexts must use `SanitizeDoubleQuote()`
   - single-quoted literal contexts must use `SanitizeSingleQuote()`
   - already-quoted defaults must use `SanitizeQuotedStringLiteral()`
3. Confirm no fallback path emits the raw value.

## Remediation rules

- Escape at the final write site, not only upstream.
- Keep quote style unchanged and apply matching sanitizer.
- Reuse existing helpers in `src/Kiota.Builder/Writers/StringExtensions.cs`.
- For cross-language consistency, patch all affected writers (Python/Go/Ruby/PHP and any other impacted writer).

## Verification expectations

After changes:

1. Add/adjust regression tests in corresponding writer test suites under `tests/Kiota.Builder.Tests/Writers/**`.
2. Include malicious payloads with quote + newline/control characters.
3. Ensure assertions validate escaped output, not just general generation success.
