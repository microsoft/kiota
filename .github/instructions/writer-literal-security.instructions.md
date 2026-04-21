---
applyTo:
  - "src/Kiota.Builder/Writers/**"
  - "src/Kiota.Builder/Refiners/**"
  - "tests/Kiota.Builder.Tests/Writers/**"
---

# Code-Generation Literal Injection Review

When reviewing changes to Kiota writer or refiner code, check for **generated-source injection** risks. Schema-controlled values (from OpenAPI specs) flow into generated source code literals. If they are not escaped at the emission site, a hostile schema can inject arbitrary code into the generated output.

## Unsafe values (schema-controlled)

- `WireName`, `SerializationName`, `IndexParameter.SerializationName`
- `DefaultValue` (constructors, getters, parameter signatures)
- `UrlTemplateOverride`, URI template constants, base URL defaults
- Path/query parameter assignment keys
- Enum wire values and enum member attributes
- Property annotations/attributes carrying serialized names
- Backing store keys, discriminator keys, query mapper keys

## Required sanitization by context

Every schema-derived value must be escaped for its destination literal context **at the final write site** (not only upstream):

| Literal context | Required sanitizer |
|---|---|
| Double-quoted string (`"..."`) | `SanitizeDoubleQuote()` |
| Single-quoted string (`'...'`) | `SanitizeSingleQuote()` |
| Already-quoted default value | `SanitizeQuotedStringLiteral()` |
| Dart single-quoted literal | `SanitizeDartSingleQuoteLiteral()` |
| Dart double-quoted literal | `SanitizeDartDoubleQuoteLiteral()` |
| PHP single-quoted literal | `ReplaceDoubleQuoteWithSingleQuote()` |

Sanitizers are in `src/Kiota.Builder/Writers/StringExtensions.cs` and `DartConventionService.cs`.

## What to flag

1. **Any `WriteLine`/`WriteLines`/`StartBlock` interpolation** that embeds a schema-controlled value into a string literal without a matching sanitizer call.
2. **New `GetDefaultValue`-style helpers** that receive `defaultValue` — verify the caller sanitizes before passing in, or the helper sanitizes internally.
3. **Fallback/else branches** that emit the raw value while other branches sanitize.
4. **Convention-layer helpers** (`AddParametersAssignment`, etc.) — verify they also sanitize, not only the writer methods.
5. **Cross-language consistency** — if one writer sanitizes a surface, all language writers must do the same for the equivalent surface.

## Test expectations

Changes to writer emission paths should include regression tests in `tests/Kiota.Builder.Tests/Writers/` that:
- Use hostile payloads containing `"`, `'`, `\n`, `\r`, `\t`, `\\`, and `$`.
- Assert the output contains **escaped** characters, not raw injection payloads.
