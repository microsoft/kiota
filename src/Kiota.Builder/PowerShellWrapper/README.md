# PowerShell Wrapper Generator

This folder implements the `PowerShellWrapper` generation language for kiota: it reads a
Microsoft Graph OpenAPI document and emits PowerShell cmdlet classes that call the
Kiota-generated C# request builders. It is the AutoRest replacement path for the Microsoft
Graph PowerShell SDK (see the project design spec for goals and non-goals).

This README covers the architecture, how to generate and package a module, the naming
algorithm, how quality is measured, and what is deliberately not built yet. The step-by-step
verification runbook lives in the msgraph-sdk-powershell repo at `tools/WrapperRunbook.md`.

## 1. Two repos, one pipeline

| Repo | Holds |
|---|---|
| `kiota` (this one) | The generator: naming, emitters, generation service, unit tests, this README |
| `msgraph-sdk-powershell` | The OpenAPI specs (`openApiDocs_KiotaCompat/`), the generated modules (`generated/`), the verification tools (`tools/Compare-WrapperCmdletNames.ps1`, `tools/Update-WrapperModuleManifests.ps1`, `tools/WrapperRunbook.md`), and the naming oracle (`src/Authentication/Authentication/custom/common/MgCommandMetadata.json`) |

The oracle file is the Method+URI to command-name inventory that ships inside
`Microsoft.Graph.Authentication` and powers `Find-MgGraphCommand`, about 29,000 entries.
Every naming rule in this generator cites it as ground truth.

## 2. Architecture

Generation flow, one module at a time:

```
kiota generate -l PowerShellWrapper
  -> KiotaBuilder loads the OpenAPI doc and applies --include-path/--exclude-path
  -> PowerShellWrapperGenerationService walks the filtered operations
       - NamingOverrides.IsSuppressed skips operations the published SDK omits
       - Naming.Resolve names each operation (verb from HTTP method, noun from path)
       - GETs are held back and paired: a list GET and its item GET that share a noun
         become one public dispatcher cmdlet plus two internal _List/_Get cmdlets
       - CmdletEmitter renders each cmdlet class as C# source
  -> one .g.cs file per cmdlet, plus Shared.g.cs, written to the output folder
```

File map:

| File | Role |
|---|---|
| `PowerShellWrapperGenerationService.cs` | Orchestrates a generation run; owns GET pairing and suppression |
| `CmdletNaming.cs` | `Naming.Resolve`: verb map, noun from path, builder expression, path params |
| `Singularizer.cs` | Word-level singularization rules (section 5 below) |
| `NamingOverrides.cs` | The checked-in exception data: renames and suppressions, each with a cited source |
| `CmdletEmitter.cs` | C# source templates for the cmdlet shapes (item GET, list GET, dispatcher, New, Update, Remove, shared auth helpers) |
| `SchemaProperties.cs` | Maps body-schema primitives onto write-cmdlet parameters |
| `OperationInfo.cs`, `EmitContext.cs` | Plain data carriers |

## 3. Generating a module

Build the CLI once, then generate against a spec:

```powershell
dotnet build src\kiota\kiota.csproj -c Debug -f net10.0

src\kiota\bin\Debug\net10.0\kiota.exe generate -l PowerShellWrapper `
  -d <path>\openApiDocs_KiotaCompat\v1.0\Mail.yml `
  -o <path>\generated\Mail `
  -c ApiClient -n Microsoft.Graph.PowerShell.Mail.Client `
  --include-path '/users/{user-id}/message[s]#GET,POST' `
  --include-path '/users/{user-id}/messages/{message-id}*#GET,DELETE'
```

Things that will bite you if you don't know them:

- **kiota skips generation when nothing changed.** The `kiota-lock.json` in the output folder
  records the spec hash and settings; a matching hash means "no changes detected". To force a
  rerun after a generator change, delete `kiota-lock.json` first. Do NOT use `--clean-output`:
  it wipes the whole output folder, including the `Module/` project that lives next to the
  generated sources.
- **Each module's `kiota-lock.json` is the record of how it was generated** (spec path,
  include patterns, namespace). A regeneration script can drive every module from its lock
  file alone; see the runbook.
- **Generation writes only `*.g.cs`.** The `Module/` folder (csproj, psd1 manifest) is
  maintained separately, which leads to the next section.

## 4. Packaging: the manifest trap

Each module's `.psd1` manifest lists `CmdletsToExport` explicitly. PowerShell gives **no
warning** when an export list matches nothing, so a renamed or suppressed cmdlet leaves the
module importing successfully while exporting nothing. This happened in practice when the
naming fix landed.

After any generation that changes cmdlet names, regenerate the manifests from the generated
sources (msgraph-sdk-powershell repo):

```powershell
tools\Update-WrapperModuleManifests.ps1
```

It rewrites every manifest's `CmdletsToExport` from the `[Cmdlet(...)]` attributes actually
present in the `.g.cs` files, so packaging cannot drift from the generated surface.

## 5. Naming algorithm

The target is byte-identical parity with the published `Microsoft.Graph.*` cmdlet names
(project success criterion: 100% name match per module against Microsoft.Graph 2.37.0+).

### 5.1 Verb: HTTP method

| HTTP method | PowerShell verb |
|---|---|
| GET | `Get` |
| POST | `New` |
| PATCH | `Update` |
| PUT | `Set` |
| DELETE | `Remove` |

### 5.2 Noun: URL path, not operationId

The noun is built from the URL path template. The operationId is deliberately not used: it
carries whatever plurality and casing the spec author chose, while the published names are a
deterministic function of the path. This also keeps naming and request routing
(`BuildBuilderExpression`) on one source of truth.

For `GET /identity/conditionalAccess/policies/{conditionalAccessPolicy-id}`:

1. Drop `{parameter}` segments: `identity`, `conditionalAccess`, `policies`.
2. Pascal-case each segment and singularize it (5.3): `Identity`, `ConditionalAccess`,
   `Policy`.
3. Collapse seams (5.4), apply overrides (5.5), prefix `Mg`:
   `MgIdentityConditionalAccessPolicy`.
4. Final name: `Get-MgIdentityConditionalAccessPolicy`, identical to the published SDK.

OData type-cast segments (`graph.user` in the KiotaCompat docs) become `As` + type name, no
singularization (cast type names are already singular): `GET /groups/{id}/owners/{id}/graph.user`
yields `Get-MgGroupOwnerAsUser`, matching the published convention.

### 5.3 Singularization (`Singularizer.cs`)

The published SDK's inflector works per camel-case word, not per whole segment. Proof from
shipping names: `termsAndConditions` ships as `Update-MgDeviceManagementTermAndCondition` (both
words singularized) and `onPremisesSynchronization` as
`Get-MgDirectoryOnPremiseSynchronization` (an interior word singularized). So each segment is
split into camel-case words and each word runs through the ordered rules below; first match
wins. Order is part of the algorithm.

| # | Rule | Example (shipping cmdlet) |
|---|---|---|
| 0 | Version tag `_v<N>` on the segment: singularize the stem, append `V<N>` | `alerts_v2` -> `AlertV2` (`Get-MgSecurityAlertV2`) |
| 1 | Words shorter than 3 chars, or all-uppercase (acronyms), unchanged | `OS` stays `OS` |
| 2 | Irregulars table | `children` -> `Child` (`Get-MgDriveItemChild`), `people` -> `Person` (`Get-MgUserPerson`) |
| 3 | Invariants table: words the SDK never singularizes | `windows` stays (`Get-MgUserSettingWindows`) |
| 4 | `ies` -> `y` | `policies` -> `Policy` |
| 5 | `uses` -> `us` | `statuses` -> `Status` (`Get-MgDeviceManagementDeviceConfigurationUserStatus`) |
| 6 | `es` after `x`, `z`, `ch`, `sh`, `ss`: drop `es` | `bookingBusinesses` -> `BookingBusiness`, `mailboxes` -> `Mailbox` |
| 7 | Ends `ss`, `us`, `is`: unchanged | `conditionalAccess`, `status`, `analysis` stay put |
| 8 | Ends `s`: drop it | `messages` -> `Message`, `plans` -> `Plan`, `settings` -> `Setting` |

The irregulars and invariants tables grow only from parity-harness evidence (section 6), never
from intuition.

### 5.4 Seam collapse

Applied while concatenating the singularized segments:

- **Adjacent duplicates** are dropped: `/users/{id}/onenote/sectionGroups/{id}/sectionGroups`
  yields `MgUserOnenoteSectionGroup...`, matching the published family
  (`Get-MgUserOnenoteSectionGroupCount`), not `...SectionGroupSectionGroup...`.
- **Boundary word overlap**: when the previous part's trailing word starts the next part, the
  repeated word is dropped once. `/domains/{id}/domainNameReferences` yields
  `MgDomainNameReference` (`Get-MgDomainNameReference`), and
  `.../managedDevices/{id}/deviceConfigurationStates` yields
  `...ManagedDeviceConfigurationState` (`Get-MgUserManagedDeviceConfigurationState`).

### 5.5 Overrides and suppression (`NamingOverrides.cs`)

The published names are not 100% algorithmic: a handful come from hand-written AutoRest
directives in the msgraph-sdk-powershell module configs. Those cases are mirrored as checked-in
data, one citation per entry, never as code branches:

| Entry | Source directive |
|---|---|
| Strip leading `Solution` for `/solutions/**` nouns (`Get-MgBookingBusiness`) | `src/Bookings/Bookings.md`: `^Solution(.*)$ -> $1` |
| `GET /users/{id}/calendar` -> `UserDefaultCalendar` | `src/Calendar/Calendar.md`: `^(User)(Calendar)$ -> $1Default$2` |
| Suppress `PATCH /users/{id}/calendar` (SDK ships no such cmdlet) | `src/Calendar/Calendar.md`: `remove-path-by-operation user_UpdateCalendar` |

Before adding an entry, prove the published name cannot fall out of the rules, and cite where
the SDK's own pipeline hand-tuned it. Known future entries of the "mirror their artifact" kind:
`Get-MgDeviceManagementIoUpdateStatus` (the published inflector singularized `Ios` to `Io`).

### 5.6 Deliberate deviations from the published surface

- The internal `Get-MgX_Get` / `Get-MgX_List` cmdlets are visible alongside each public
  dispatcher. The published SDK hides its variants inside parameter sets instead. This is the
  design spec's resolved parameter-set decision and belongs in the migration guide.

## 6. How quality is measured

Quality here is a number a script prints, not an assertion:

- **Unit goldens**: `tests/Kiota.Builder.Tests/PowerShellWrapper/NamingTests.cs` pins the rules
  to published names taken from the oracle. Run:
  `dotnet test tests\Kiota.Builder.Tests\Kiota.Builder.Tests.csproj --filter "FullyQualifiedName~PowerShellWrapper"`.
  Every new naming behavior gets its row here first, with the expected value copied from
  `Find-MgGraphCommand` or the oracle file, never typed from memory.
- **Parity gate**: `tools/Compare-WrapperCmdletNames.ps1` (msgraph-sdk-powershell repo)
  reconstructs each generated cmdlet's URI from its builder expression, joins it to the
  oracle, prints per-module parity, and exits 1 on any mismatch. Status 2026-07-24: 66/66
  across the 15 pilot modules.
- **Full-surface dry run**: simulating the algorithm over every v1.0 CRUD operation in the
  oracle (about 9,000) scores 68.4% with zero module-specific data; the remainder is
  categorized as per-module directive renames (for example `IdentityGovernance...` prefixes
  dropped to `EntitlementManagement...`), which become override entries as modules are
  onboarded, exactly where AutoRest keeps them today.

The end-to-end verification runbook (generate, gate, build, import, oracle cross-check, live
tenant) is `tools/WrapperRunbook.md` in the msgraph-sdk-powershell repo.

## 7. Current status and known gaps

Honest state as of 2026-07-24, so nobody has to reverse-engineer it from git history:

| Area | State |
|---|---|
| Name parity | 100% on the 15 pilot modules, gated |
| Auth, query params, body binding | Working, verified against a live tenant |
| Pagination (`-All`, nextLink warning) | Designed in the spec, not yet wired |
| Breadth | 66 cmdlets; the full SDK surface is ~29,000 operations |
| Operation classes | No `$count`, `$ref`, `$value`, delta, OData actions/functions (`Invoke` verb), or cast endpoints generated yet (the cast naming rule exists and is tested) |
| Body binding depth | Top-level primitive properties only; no complex types, enums, or typed formats |
| Self-referencing paths | `/sites/{id}/sites` ships as the hand-renamed `Get-MgSubSite`; not yet onboarded |
| Oracle cast join | The oracle renders cast URIs without the `graph.` prefix; the parity gate needs a cast-aware join before the first cast operation is onboarded |
