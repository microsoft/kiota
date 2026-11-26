const { spawnSync } = require("node:child_process");
const { appendFileSync, readFileSync } = require("node:fs");

// Classify by behavioral impact, not package contents. Do not ignore Markdown
// globally: source resources and test fixtures can also use that format.
function isNonImpactful(path) {
    return /^(docs\/|\.vscode\/|\.devcontainer\/|\.github\/(ISSUE_TEMPLATE|agents|instructions|skills)\/)/.test(path)
        || /^(README|CHANGELOG|CODE_OF_CONDUCT|CONTRIBUTING|SECURITY|SUPPORT)\.md$/i.test(path)
        || /^(abstractions|authentication|http|serialization|it)\/readme\.md$/i.test(path)
        || /^specs\/.*\.md$/i.test(path)
        || /^vscode\/packages\/(microsoft-kiota|npm-package)\/[^/]+\.md$/i.test(path)
        || path === "LICENSE"
        || path === ".github/copilot-instructions.md";
}

function requiresTests(eventName, event, headSha, git = spawnSync) {
    let base;
    let head;
    if (eventName === "pull_request") {
        base = event.pull_request?.base?.sha;
        // Compare the tested merge tree, not just the PR head, against the target.
        head = headSha;
    } else if (eventName === "merge_group") {
        base = event.merge_group?.base_sha;
        head = event.merge_group?.head_sha;
    } else {
        return true;
    }

    if (!/^[a-f0-9]{40}$/.test(base ?? "") || !/^[a-f0-9]{40}$/.test(head ?? "")) {
        console.warn("::warning::Missing comparison SHAs; running all builds and tests.");
        return true;
    }

    // Disabling rename detection includes both the old and new paths of a move.
    const diff = git("git", ["diff", "--name-only", "--no-renames", "-z", base, head, "--"], {
        encoding: "utf8",
        maxBuffer: 64 * 1024 * 1024,
    });
    if (diff.error || diff.status !== 0) {
        console.warn("::warning::Unable to compare changed paths; running all builds and tests.");
        return true;
    }

    const paths = diff.stdout.split("\0").filter(Boolean);
    // An empty comparison is not sufficient evidence to bypass the tests.
    return paths.length === 0 || paths.some(path => !isNonImpactful(path));
}

if (require.main === module) {
    const event = JSON.parse(readFileSync(process.env.GITHUB_EVENT_PATH, "utf8"));
    const runTests = requiresTests(process.env.GITHUB_EVENT_NAME, event, process.env.GITHUB_SHA);
    appendFileSync(process.env.GITHUB_OUTPUT, `run-tests=${runTests}\n`);
    console.log(runTests ? "Changes may affect software behavior or test execution; running tests." : "Only known non-behavioral paths changed; no build or tests required.");
}

module.exports = { requiresTests };
