# Rust implementation comparison report (Darrel vs Mintu)

## Executive outcome

The best reconciliation path is a **hybrid**:

1. Use **Mintu's runtime architecture and Rust module-oriented generator shape** as the base.
2. Merge in **Darrel's Kiota-repo integration completeness** (appsettings wiring, public API export mapping, refiner coverage) plus known refiner-ordering safeguards.

Neither implementation is merge-ready on its own against current guidance and expected Kiota parity.

---

## Evaluation framework

The comparison is grounded in:

1. Kiota add-language guidance and the guidance updates provided in docs PR #223.
2. Rust implementation expectations discussed in issue #4436.
3. Generator PRs #7571 (Darrel) and #7574 (Mintu).
4. Runtime repos `darrelmiller/kiota-rust` and `Gmin2/kiota-rs`.
5. Generated-client examples in both approaches.
6. Cross-language serialization baselines from active Kiota stacks (`kiota-dotnet`, `kiota-java`, `kiota-abstractions-go` + `kiota-serialization-json-go`).

---

## Consolidated findings

### 1) Generator integration and codegen architecture

| Area | Darrel's approach (#7571) | Mintu's approach (#7574) | Finding |
|---|---|---|---|
| Kiota integration completeness | Includes Rust dependency block in `appsettings.json` and Rust mapping in `PublicAPIExportService` | Missing those integration points in PR diff | Darrel is more complete for in-repo integration |
| File/module strategy | Flatter snake_case file pathing | Rust-native namespace/file strategy (`lib.rs`/`mod.rs`) | Mintu is more idiomatic and maintainable for Rust |
| Barreled behavior | No Rust addition to barreled languages | Adds Rust to barreled languages | Mintu aligns better with module exports |
| Refiner tests | Includes Rust refiner tests | No equivalent refiner test file in PR set | Darrel has stronger refiner test footprint |
| Codegen style | More serde-centric generated models | More Kiota-abstraction-centric generated models (`Parsable` flow) | Mintu is closer to long-term Kiota architecture intent |

**Net:** Darrel is stronger on Kiota repository wiring; Mintu is stronger on Rust codegen structure and architecture direction.

### 2) Runtime architecture and behavior

| Area | Darrel runtime (`kiota-rust`) | Mintu runtime (`kiota-rs`) | Finding |
|---|---|---|---|
| Crate scope | Abstractions + HTTP adapter + JSON | Abstractions + HTTP + JSON/Form/Text/Multipart + Bundle (+ auth crate) | Mintu has broader runtime surface |
| Error model | Mostly `Box<dyn Error>` | Typed `KiotaError`/`ApiError` | Mintu has stronger error modeling |
| Adapter model | `send_raw`/`send_raw_collection` + serde value conversions | Typed `send`/`send_collection`/`send_primitive` with parsable factories | Mintu aligns better with Kiota abstraction layering |
| URI template support | Manual replacement, no `std-uritemplate` | Manual replacement with explicit TODO for `std-uritemplate` | Both miss the explicit guidance requirement |
| Bundle/default adapter | No bundle meta-package | `kiota-bundle` with default adapter helper | Mintu aligns with bundle guidance |
| Ordering-sensitive maps | Mostly `HashMap` | Uses `IndexMap` in important places | Mintu is better aligned with ordering concerns |

Additional runtime-specific gaps:

- Darrel `RequestInformation::set_content_from_parsable` sets content type but does not serialize parsable content into request body.
- Darrel serializer/deserializer registration helpers instantiate factories without storing/registering them in shared registries.
- Mintu parse-node generic methods that require `Self: Sized` are not invocable through trait objects (`&dyn ParseNode`), creating ergonomics and abstraction friction.

### 3) Generated client examples

| Area | Darrel examples | Mintu examples | Finding |
|---|---|---|---|
| Setup ergonomics | Client constructors auto-register JSON factories and support default base URL setup | Often manual parse-node registration; bundle exists but is not consistently used in examples | Darrel has smoother immediate UX; Mintu has better primitive but inconsistent adoption |
| Request execution | Heavy use of `send_raw` and `serde_json::from_value` | Typed send paths + parsable/downcast flow | Mintu is closer to serializer-agnostic Kiota model |
| Known generated gaps | Less explicit TODOs in examples, but serialization coupling is high | TODOs in collection-body paths and object collection serialization (`Pet.tags`) | Mintu needs closure on generated TODO paths before preview readiness |

Common example/runtime concern:

- Raw URL constructor behavior exists, but end-to-end raw URL override semantics are not clearly validated in either approach.

### 4) Serialization parity vs other Kiota languages

#### 4.1 Abstractions-level type/shape coverage

Legend: ✅ implemented, ⚠️ partial/mismatched, ❌ missing.

| Capability | Baseline (dotnet/java/go) | Darrel Rust | Mintu Rust |
|---|---|---|---|
| string, bool, int32/int64, float/double | ✅ | ✅ | ✅ |
| byte/u8 and int8/sbyte | ✅ | ❌ | ✅ |
| short/int16 | ✅ (notably in Java) | ❌ | ❌ |
| decimal / big decimal | ✅ (dotnet/java) | ❌ | ❌ |
| UUID/GUID | ✅ | ✅ | ✅ |
| datetime + date + time + duration | ✅ | ✅ | ✅ |
| enum value APIs | ✅ | ❌ | ✅ |
| enum collection/set APIs | ✅ | ❌ | ✅ |
| primitive collection APIs | ✅ | ✅ (narrow explicit set) | ✅ |
| object value APIs | ✅ | ❌ (raw-value centric) | ✅ |
| object collection APIs | ✅ | ⚠️ node-collection only | ✅ |
| explicit null write | ✅ | ❌ | ✅ |
| additional data write | ✅ | ✅ | ✅ |

#### 4.2 Format-level behavior (JSON/Form/Text/Multipart)

| Behavior | Baseline (dotnet/java/go) | Darrel Rust | Mintu Rust |
|---|---|---|---|
| JSON byte[] wire format | Base64 string | Hex string on write; plain UTF-8 string bytes on read | JSON numeric array for bytes |
| JSON numeric breadth | Broad (incl decimal in some stacks) | Narrow | Missing decimal/short parity |
| Form primitive collections | Supported in mature stacks | Not available (no form crate) | Explicitly rejected |
| Form byte[] handling | Supported in mature stacks | Not available (no form crate) | Explicitly rejected |
| Text scalar serialization | Supported | Not available (no text crate) | Implemented |
| Multipart support | Partial even in mature stacks, body-centric | Not available (no multipart crate) | Partial |

**High-impact serialization conclusion:** both Rust implementations currently diverge from established Kiota JSON byte-array behavior, which is a compatibility risk for generated clients and wire-level interoperability.

### 5) Unified gap register

| Severity | Gap | Darrel | Mintu |
|---|---|---|---|
| **Critical** | RFC6570 URI template support via `std-uritemplate` | Missing | Missing (TODO present) |
| **Critical** | JSON byte-array wire-format parity (base64 behavior) | Divergent | Divergent |
| **Critical** | Raw URL override semantics end-to-end confidence | Weak/unclear | Weak/unclear |
| **High** | Request body serialization completeness for generated bodies | Parsable body path incomplete | Known generated TODOs for collection/object-collection paths |
| **High** | Kiota-repo integration completeness in generator PR | Strong | Missing appsettings/export wiring |
| **High** | Bundle + default adapter guidance alignment | Missing | Present |
| **Medium** | Error mapping parity (`4XX/5XX/XXX`) in generated flows | Thin | Thin |
| **Medium** | Type coverage parity (decimal/short and related breadth) | Missing | Missing |
| **Medium** | Form serializer parity for common scenarios | Not present | Narrow/collection-rejecting |
| **Medium** | Serializer-agnostic purity in generated code | Serde-coupled | Better direction, some coupling remains |

---

## Consolidated recommendations

### A) Target architecture decision

Adopt **Mintu as the structural base** (runtime shape, module/file model, typed adapter and error abstractions), and merge **Darrel's integration/test deltas** into that base.

### B) Mandatory merge gates (must be complete before preview/merge)

1. Implement RFC6570 URI template expansion with `std-uritemplate`.
2. Align JSON byte-array handling with Kiota baseline behavior (base64-compatible read/write).
3. Close generated serialization TODOs that block common body scenarios:
   - collection request-body serialization (`createWithArray` / `createWithList` patterns),
   - object collection model serialization (for example `tags`-style fields).
4. Prove raw URL override semantics in end-to-end generated-client paths.
5. Port Darrel integration wiring into the Mintu base:
   - Rust dependency block in `appsettings.json`,
   - Rust export mapping in `PublicAPIExportService`,
   - Rust refiner test footprint.

### C) Priority follow-ups (post-gate but near-term)

1. Normalize sample ergonomics around the bundle default adapter helper so examples match recommended usage.
2. Improve form serializer/deserializer coverage for practical `application/x-www-form-urlencoded` scenarios (notably primitive collections and byte-array handling where required by Kiota behavior).
3. Clarify and improve trait-object ergonomics around parse-node generic APIs in Rust abstractions.
4. Expand type parity where feasible (decimal/short equivalents), with explicit documented limits where not adopted.

### D) Design policy recommendation

Keep generated code centered on Kiota abstractions and serializer-agnostic contracts. If serde derives are desired for developer ergonomics, expose them as optional features rather than baseline coupling.

### E) Implementation safety recommendation

Preserve established refiner-ordering safeguards (notably request-builder property-to-method transformation order relative to type/property corrections) to avoid downstream rename/lookup regressions.

---

## Final guidance

For maintainable parity with the broader Kiota ecosystem, proceed with the **hybrid plan**: Mintu architecture baseline, Darrel integration/test deltas, and strict enforcement of URI-template + serialization parity gates before declaring Rust preview-ready.
