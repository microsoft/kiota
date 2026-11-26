# Kiota dev container

Everything needed to build, test and debug Kiota (generator, CLI, VS Code extension, and integration tests in all eight languages), at the versions CI uses. Works in VS Code, GitHub Codespaces and the [Dev Container CLI](https://github.com/devcontainers/cli), on x64 and ARM64.

## Open it

| Where | How |
| --- | --- |
| VS Code | With Docker and the [Dev Containers extension](https://marketplace.visualstudio.com/items?itemName=ms-vscode-remote.remote-containers): **Dev Containers: Reopen in Container**. The first build compiles PHP, Ruby and Python from source and takes about 15 minutes on a 4-core machine; later starts take seconds. On macOS and Windows, **Clone Repository in Container Volume** gives faster file access. |
| Codespaces | **Code > Codespaces > Create codespace on main** |
| CLI | `devcontainer up --workspace-folder .` then `devcontainer exec --workspace-folder . bash` |

## What's inside

Base image [`mcr.microsoft.com/devcontainers/base:3-resolute`](https://github.com/devcontainers/images/tree/main/src/base-ubuntu) (Ubuntu 26.04, as CI's `ubuntu-latest` runners) plus [dev container features](https://containers.dev/features):

| Tool | Version (source) | Used for |
| --- | --- | --- |
| .NET SDK | newest 10.0 (`global.json`) | generator, CLI, tests |
| PowerShell | 7.6 | `it/*.ps1` |
| Node.js | 24 (workflows) | VS Code extension, TypeScript ITs |
| Temurin + Maven | 17 + 3.9 (workflows) | Java ITs, MockServer |
| Go | 1.27 (workflows: `^1.25`, `it/go/go.mod`) | Go ITs, gofmt check in .NET tests |
| Python | 3.14 (workflows) | Python ITs (own interpreter, `pip install` works) |
| Ruby + Bundler | 3.3 (workflows) | Ruby ITs |
| PHP + Composer | 8.2 (workflows) | PHP ITs; Xdebug off (`XDEBUG_MODE=debug` to enable) |
| Dart | newest 3.x (`it/dart/pubspec.yaml`) | Dart ITs ([local feature](features/dart)) |
| xvfb + Electron libraries | | headless extension tests |
| GitHub CLI, reportgenerator, dotnet-outdated | 2.x, 5.x, 4.x | PRs, coverage reports, `dotnet outdated` |
| Pester | 6.x | tests for the scripts in `.devcontainer/` |

On creation the container runs `dotnet restore` and `npm ci` (in `vscode/`). Extensions: C# Dev Kit, OpenAPI, YAML, GitHub Actions and pull requests, GitLens, Live Share, markdownlint, cSpell, ESLint, Prettier, and one per integration-test language.

## Work in it

```bash
# Generator and CLI
dotnet build kiota.slnx
dotnet test kiota.slnx
dotnet run --project src/kiota -f net10.0 -- generate -l java -d tests/Kiota.Builder.IntegrationTests/ToDoApi.yaml -o /tmp/todo

# VS Code extension
npm run build --prefix vscode
npm run lint --prefix vscode

# One integration test, as CI runs it (any of csharp, go, java, python, ruby, php, dart, typescript)
dotnet publish src/kiota/kiota.csproj -c Release -p:PublishSingleFile=true -f net10.0 -o publish
pwsh it/generate-code.ps1 -descriptionUrl ./tests/Kiota.Builder.IntegrationTests/InheritingErrors.yaml -language python
# Set Stop first, as a GitHub Actions pwsh step does: without it, a failing Ruby test exits with 0
pwsh -c '$ErrorActionPreference = "Stop"; ./it/exec-cmd.ps1 -descriptionUrl ./tests/Kiota.Builder.IntegrationTests/InheritingErrors.yaml -language python'
```

- **Debug** a generation with the **Launch \<language\>** configurations; the extension with **Run Extension** in a window opened on `vscode/packages/microsoft-kiota`.
- **Extension and npm package tests** download a kiota release by default. To run them against your checkout, use `-Tier Full` below; it publishes kiota and passes it in through `KIOTA_SIDELOADING_BINARY_ZIP_PATH` and `lib/.kiotabin`.

## Check it

```bash
pwsh .devcontainer/test-devcontainer.ps1              # tools match CI's versions (about a second)
pwsh .devcontainer/test-devcontainer.ps1 -Tier Build  # + build the solution and the extension
pwsh .devcontainer/test-devcontainer.ps1 -Tier Full   # + all tests, one integration test per language
Invoke-Pester -Path ./.devcontainer/*.Tests.ps1       # the tests for these scripts (pwsh)
```

`Full` runs the integration tests in a temporary worktree (`HEAD` with your uncommitted changes to tracked files), so `it/` is never changed, and stops before them if `it/` or the description has local changes. If `.NET SDK` fails after `global.json` moved, run **Dev Containers: Rebuild Without Cache and Reopen in Container** (a plain rebuild keeps the cached SDK).

## Before you open a pull request

Open it in your fork first, from your branch into your fork's `main` (synced with `microsoft/kiota`, with Actions enabled on the fork's **Actions** tab). The repository's own workflows then run on your fork at no cost, on every operating system CI uses. Two of them fail in a fork for reasons that don't apply upstream: `.NET` at the code coverage upload after its tests, and **Build VSCode extension** because a fork has no Kiota release to download.

## Personal additions

Add your own extensions or features in your VS Code user settings, not here:

```jsonc
"dev.containers.defaultExtensions": ["<publisher.extension>"],
"dev.containers.defaultFeatures": { "ghcr.io/devcontainers/features/docker-outside-of-docker:1": {} }
```

## Maintenance

- **CI**: [`devcontainer.yml`](../.github/workflows/devcontainer.yml) builds the container from scratch on x64 and ARM64 and runs `-Tier Full` when the container or `global.json` change, and weekly; the weekly run also catches version changes in the CI workflows, `it/go/go.mod` and `it/dart/pubspec.yaml`.
- **Dependabot** updates the features ([`devcontainer-lock.json`](devcontainer-lock.json), one grouped PR) and the CLI CI uses ([`package.json`](package.json)). Those PRs skip the product test suites (`scripts/test-impact.cjs`) and are tested by this workflow, which is not a required check, so they are not auto-merged: merge them once it passes.
- **Changing a tool version**: update the CI workflow and the matching option in [`devcontainer.json`](devcontainer.json) together; `-Tier Tools` fails until they agree.
- **Base image**: patch updates come with the `3-resolute` tag; a new major is a manual bump.
- **Versioning**: the container is versioned with the repository. Each commit's `.devcontainer/` matches that commit's CI, so an older checkout gets the container that went with it.
