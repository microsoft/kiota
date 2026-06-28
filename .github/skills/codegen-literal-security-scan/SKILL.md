---
name: codegen-literal-security-scan
description: Detect and remediate code-generation literal injection risks in Kiota writer code (wire names, serialization names, URL templates, base URLs, defaults, and path parameter keys).
---

Use this skill when auditing or modifying any code generator writer logic.

## Goal

Prevent generated-source injection (including potential RCE chains) by ensuring every untrusted schema-derived value is escaped for the destination literal context before being emitted.

## Focus surfaces (expanded)

Prioritize all literal-emission paths under `src/Kiota.Builder/Writers/**/*.cs`, especially:

- `WireName`, `SerializationName`, `IndexParameter.SerializationName`
- `UrlTemplateOverride`, URI template constants, base URL defaults
- `DefaultValue` (constructor assignments, getter fallback values, parameter signatures)
- Path/query parameter assignment keys in convention helpers
- Query parameters mapper constants and metadata constants
- Enum wire values and enum member attributes
- Property annotations/attributes that carry serialized names
- Backing store keys for custom/additional-data properties
- Documentation long/short comments that interpolate external links/labels

## Required checks

1. Locate candidate sinks:
   - search for interpolation/writes that include the focus surfaces and emit language string literals (`WriteLine`, `WriteLines`, `StartBlock`, doc attributes/tags).
2. Verify context-aware escaping is applied at the emission site:
   - double-quoted literal contexts must use `SanitizeDoubleQuote()`
   - single-quoted literal contexts must use `SanitizeSingleQuote()`
   - already-quoted defaults must use `SanitizeQuotedStringLiteral()`
   - Dart quoted literals must use `SanitizeDartSingleQuoteLiteral`/`SanitizeDartDoubleQuoteLiteral` (to also escape `$`)
   - Go struct tags must keep backticks and sanitize embedded quoted values (do not switch tag quoting style)
3. Confirm no fallback path emits the raw value.
4. Confirm convention-layer helpers (`AddParametersAssignment` and equivalents) are also sanitized, not only writer methods.

## Remediation rules

- Escape at the final write site, not only upstream.
- Keep quote style unchanged and apply matching sanitizer.
- Reuse existing helpers in `src/Kiota.Builder/Writers/StringExtensions.cs`.
- For cross-language consistency, patch all affected writers (C#/Dart/Go/Java/PHP/Python/Ruby/TypeScript and helpers).
- Preserve runtime semantics while hardening:
  - keep Go struct tag behavior intact
  - avoid changing enum/default value resolution logic
  - avoid introducing extra quoting around non-string literals

## High-risk sink families to explicitly review

1. Serializer/deserializer key emission (`WireName`, discriminator keys, query mapper keys).
2. Request-builder URL/template metadata (`UrlTemplateOverride`, URI template constants, base URL defaults).
3. Indexer/path parameter map writes (`urlTplParams[...]`, `.Add(...)`, `.put(...)`, map assignments).
4. Property/enum metadata attributes and constants (`QueryParameter`, `EnumMember`, enum objects, navigation/request metadata constants).
5. Default value emission in constructors, getters, and parameter signatures.
6. Documentation comments with external links/labels and deprecation text.

## Verification expectations

After changes:

1. Add/adjust regression tests in corresponding writer test suites under `tests/Kiota.Builder.Tests/Writers/**`.
2. Include malicious payloads with quote + newline/control characters (`"`, `'`, `\n`, `\r`, `\t`, `\\`, and `$` where relevant).
3. Ensure assertions validate escaped output, not just general generation success.
4. Validate targeted writer tests and then broader builder/solution validation.
