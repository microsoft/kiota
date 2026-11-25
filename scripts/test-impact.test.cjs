const assert = require("node:assert/strict");
const { spawnSync } = require("node:child_process");
const { mkdtempSync, mkdirSync, readFileSync, rmSync, writeFileSync } = require("node:fs");
const { tmpdir } = require("node:os");
const { join } = require("node:path");
const { test } = require("node:test");
const { requiresTests } = require("./test-impact.cjs");

const base = "a".repeat(40);
const head = "b".repeat(40);
const pr = { pull_request: { base: { sha: base } } };
const group = { merge_group: { base_sha: base, head_sha: head } };
const diff = paths => () => ({ status: 0, stdout: paths.join("\0") + "\0" });

test("known non-behavioral paths bypass tests on PRs and merge groups", () => {
    for (const [eventName, event] of [["pull_request", pr], ["merge_group", group]]) {
        assert.equal(requiresTests(eventName, event, head, diff([
            "docs/tutorial.md", ".vscode/settings.json", "CHANGELOG.md", "CONTRIBUTING.md",
            ".github/ISSUE_TEMPLATE/bug.yml", ".github/instructions/testing.md",
            ".github/agents/reviewer.md", ".github/skills/review/SKILL.md",
            ".github/copilot-instructions.md", "README.md", "LICENSE",
            "abstractions/README.md", "authentication/README.md", "http/README.md",
            "serialization/README.md", "it/Readme.md", "specs/cli/client-add.md",
            "vscode/packages/microsoft-kiota/README.md",
            "vscode/packages/microsoft-kiota/CHANGELOG.md",
            "vscode/packages/microsoft-kiota/debugging.md",
            "vscode/packages/npm-package/readme.md",
            "vscode/packages/npm-package/CONTRIBUTING.md",
        ])), false);
        for (const path of [
            "src/kiota/Program.cs", "tests/Kiota.Tests/CommandTests.cs", "it/go/go.mod",
            "vscode/package-lock.json", "global.json", "Directory.Packages.props", "kiota.slnx",
            ".editorconfig", ".github/workflows/dotnet.yml", "scripts/test-impact.cjs",
            "resources/index-block-list.yml", "unknown/input", "src/docs/example.md",
            "docs-like/file.md", "src/new\nfile.cs",
            "src/Kiota.Builder/Resources/README.md", "tests/fixtures/README.md",
            "it/python/fixtures/README.md", "resources/template.md",
            "vscode/packages/microsoft-kiota/src/fixtures/README.md",
            "specs/example.yaml", "unknown/README.md",
        ]) {
            assert.equal(requiresTests(eventName, event, head, diff(["README.md", "docs/tutorial.md", path])), true, path);
        }
    }
});

test("uses the PR merge SHA and the complete merge group range without rename detection", () => {
    for (const [eventName, event] of [["pull_request", pr], ["merge_group", group]]) {
        requiresTests(eventName, event, head, (command, args) => {
            assert.equal(command, "git");
            assert.deepEqual(args, ["diff", "--name-only", "--no-renames", "-z", base, head, "--"]);
            return diff(["docs/tutorial.md"])();
        });
    }
});

test("pushes, manual runs, missing SHAs, empty diffs and git errors never bypass tests", () => {
    const unexpectedGit = () => { throw new Error("Git should not be invoked"); };
    for (const eventName of ["push", "workflow_dispatch", "schedule"]) {
        assert.equal(requiresTests(eventName, {}, head, unexpectedGit), true);
    }
    assert.equal(requiresTests("pull_request", {}, head, unexpectedGit), true);
    assert.equal(requiresTests("merge_group", group, head, () => ({ status: 128 })), true);
    assert.equal(requiresTests("pull_request", pr, head, () => ({ status: null, error: new Error("git failed") })), true);
    assert.equal(requiresTests("pull_request", pr, head, diff([])), true);
});

test("required workflow gates pass only successful runs or explicitly authorized bypasses", () => {
    for (const [workflow, prerequisites] of [
        ["dotnet", ["dotnet-tests"]],
        ["integration-tests", ["build", "download-openapi-specs", "integration"]],
        ["idempotency-tests", ["build", "download-openapi-specs", "idempotency"]],
        ["surface-area-tests", ["build", "surface-diff"]],
        ["build-vscode-extension", ["test_and_generate_binaries", "build"]],
    ]) {
        const yaml = readFileSync(join(__dirname, "..", ".github", "workflows", `${workflow}.yml`), "utf8");
        const expression = yaml.match(/PASSED: \$\{\{ (.+) \}\}/)?.[1];
        assert.ok(expression, workflow);
        const evaluate = (detectorResult, runTests, results) => {
            const needs = {
                changes: { result: detectorResult, outputs: { "run-tests": runTests } },
                ...Object.fromEntries(prerequisites.map((name, i) => [name, { result: results[i] }])),
            };
            const resolved = expression.replace(
                /needs\.([\w-]+)\.(outputs\.run-tests|result)/g,
                (_, job, property) => JSON.stringify(property === "result" ? needs[job].result : needs[job].outputs["run-tests"]),
            );
            // Evaluate the actual Actions gate's boolean expression, not a duplicate implementation.
            return require("node:vm").runInNewContext(resolved);
        };
        const success = prerequisites.map(() => "success");
        const skipped = prerequisites.map(() => "skipped");
        assert.equal(evaluate("success", "true", success), true, workflow);
        assert.equal(evaluate("success", "false", skipped), true, workflow);
        assert.equal(evaluate("success", "", skipped), false, workflow);
        for (const result of ["failure", "cancelled", "skipped"]) {
            for (let i = 0; i < prerequisites.length; i++) {
                const results = [...success];
                results[i] = result;
                assert.equal(evaluate("success", "true", results), false, `${workflow}: ${prerequisites[i]} ${result}`);
            }
            assert.equal(evaluate(result, "false", skipped), false, `${workflow}: detector ${result}`);
        }
    }
});

test("real git comparisons detect deletions, moves into docs and mixed merge-group changes", t => {
    const cwd = mkdtempSync(join(tmpdir(), "kiota-test-impact-"));
    t.after(() => rmSync(cwd, { recursive: true, force: true }));
    const git = (...args) => {
        const result = spawnSync("git", args, { cwd, encoding: "utf8" });
        assert.equal(result.status, 0, result.stderr);
        return result.stdout.trim();
    };
    git("init", "--quiet");
    git("config", "user.email", "test@example.invalid");
    git("config", "user.name", "Test");
    git("config", "commit.gpgsign", "false");
    mkdirSync(join(cwd, "src"));
    mkdirSync(join(cwd, "docs"));
    writeFileSync(join(cwd, "src", "input.cs"), "source");
    writeFileSync(join(cwd, "docs", "guide.md"), "docs");
    writeFileSync(join(cwd, "README.md"), "package documentation");
    git("add", ".");
    git("commit", "--quiet", "-m", "base");
    const baseSha = git("rev-parse", "HEAD");
    const realGit = (command, args, options) => spawnSync(command, args, { ...options, cwd });
    const check = () => requiresTests("merge_group", {
        merge_group: { base_sha: baseSha, head_sha: git("rev-parse", "HEAD") },
    }, undefined, realGit);

    git("rm", "docs/guide.md");
    writeFileSync(join(cwd, "README.md"), "updated package documentation");
    git("add", "README.md");
    git("commit", "--quiet", "-m", "update documentation");
    assert.equal(check(), false);
    const eventPath = join(cwd, "event.json");
    const outputPath = join(cwd, "output.txt");
    writeFileSync(eventPath, JSON.stringify({
        pull_request: { base: { sha: baseSha } },
    }));
    const cli = spawnSync(process.execPath, [join(__dirname, "test-impact.cjs")], {
        cwd,
        encoding: "utf8",
        env: {
            ...process.env,
            GITHUB_EVENT_NAME: "pull_request",
            GITHUB_EVENT_PATH: eventPath,
            GITHUB_SHA: git("rev-parse", "HEAD"),
            GITHUB_OUTPUT: outputPath,
        },
    });
    assert.equal(cli.status, 0, cli.stderr);
    assert.equal(readFileSync(outputPath, "utf8"), "run-tests=false\n");

    mkdirSync(join(cwd, "docs"), { recursive: true });
    git("mv", "src/input.cs", "docs/input.md");
    git("commit", "--quiet", "-m", "move source into docs");
    assert.equal(check(), true);
});
