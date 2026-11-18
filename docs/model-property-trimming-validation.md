# Property-filter protection validation

The protection contract is branch-local: a required/discriminator constraint protects a declaration when both can be active in the same composition entry. Repeated occurrences of the same union reuse its branch choice; independent unions have independent choices. Reference-local constraints are conjunctive with the reference target. Every schema is considered as an entry, including schemas inside cycles. Nested properties, items, and additional-property schemas have independent entry contexts; they do not impose their field requirements on the containing object.

## Independent differential oracle

`OpenApiPropertyFilterOracleTests` does not use the production activation conditions, decision graph, or protection helpers. For each small graph it enumerates every concrete union-choice assignment, walks every schema entry with a stack and visited set, and records which declarations coexist with a constraint. It compares that result against both inclusion trimming and exclusion rejection.

The suite includes eight named graph families and 64 deterministic generated graphs. These cover shared wrappers, partially overlapping independent unions, constrained references, reference-owned sibling declarations, discriminator constraints, cycles, and three-way alternatives. The oracle checks this generator protection contract, not arbitrary JSON Schema payload satisfiability. Exhaustive choice enumeration is deliberately limited to small graphs.

```powershell
dotnet run --project tests\Kiota.Builder.Tests\Kiota.Builder.Tests.csproj -- `
  -class Kiota.Builder.Tests.OpenApiPropertyFilterOracleTests -noLogo
```

## Isolated resource measurements

Restore the builder-test project once, then run:

```powershell
dotnet restore tests\Kiota.Builder.Tests\Kiota.Builder.Tests.csproj
.\it\measure-property-filtering.ps1 -Iterations 3 -OutputPath synthetic.json
```

The default cases are 2,048 disconnected required-field models, a depth-24 shared diamond, and a 24-node union cycle. Each baseline and filtered sample runs in a fresh Release-mode test process. The script enforces configurable per-sample limits (`-TimeoutSeconds`, default 120; `-MaxWorkingSetMiB`, default 1,024) and writes raw JSON samples.

To reproduce the real-world case, download the pinned GitHub description:

```powershell
$uri = 'https://raw.githubusercontent.com/github/rest-api-description/c9c95fe34ced05fe076001f5c55704742bc8e7f8/descriptions/api.github.com/api.github.com.json'
Invoke-WebRequest -Uri $uri -OutFile github-openapi.json
.\it\measure-property-filtering.ps1 -Scenarios file -DescriptionPath github-openapi.json `
  -IncludeProperty 'simple-user#login' -Iterations 3 -OutputPath github-resources.json
```

The reports distinguish parsing time, filter time, managed bytes allocated during filtering, and whole-process peak resident memory. Peak memory includes the test host, parsing, and JIT; it is not the filter's exclusive live heap. Baselines parse the same description but apply no property selectors. These are cold-process measurements, not throughput benchmarks or formal worst-case complexity bounds.

A Windows x64/.NET 10.0.15 run produced the following results (three samples, median filter time, mean filter allocations, maximum process peak):

| Input | Baseline ms | Filtered ms | Filter allocations MiB | Baseline peak MiB | Filtered peak MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| 2,048 disconnected models | 2.8 | 108.4 | 18.6 | 86.6 | 95.3 |
| Depth-24 shared diamond | 2.7 | 46.8 | 6.4 | 84.7 | 88.7 |
| 24-node union cycle | 2.7 | 52.5 | 6.2 | 85.0 | 88.9 |
| GitHub, 1,016 components / 41,231 declarations | 2.7 | 666.3 | 155.2 | 218.5 | 311.0 |

The pinned description's SHA-256 is `6d9bbb20d8865b01464028937f2403954ad46598b4ebd3513dcb1a15b1a84aa7`. Its selector removes four optional declarations while preserving required fields. Results vary with runtime and hardware; CI publishes fresh raw measurements rather than enforcing these timings as thresholds.

## Generated-client compilation and runtime checks

```powershell
npm ci --prefix it\typescript
.\it\validate-property-filtering.ps1 -KiotaExecutable .\publish\kiota.exe
```

Use the platform's published Kiota executable, or a local development executable. `-Languages CSharp` and `-Languages TypeScript` select individual checks. `-TypeScriptNodeModules` can point at an already prepared directory containing the existing TypeScript integration dependencies.

The harness generates fresh clients from `PropertyFiltering.yaml`, compiles C#, type-checks TypeScript, and runs both against their JSON serialization runtimes. Checks cover retained inherited/discriminator fields, recursive collections, omitted declarations and unreachable models, branch-local required union fields, reference-owned sibling and inline `allOf` declarations, and preservation of additional data. C# reuses the maintained `it/csharp/basic/basic.csproj` dependency manifest; TypeScript reuses `it/typescript` dependencies. No service or mock server is needed, and generated clients are removed afterward.

The integration workflow runs these cross-language checks and bounded resource samples. Its aggregate integration status includes the new validation job, and raw resource JSON is uploaded as `property-filter-resources`. The independent oracle runs with the regular builder test suite.
