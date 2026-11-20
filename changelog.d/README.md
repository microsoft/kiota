# Changelog entries

A pull request that changes Kiota for its users adds its changelog entry as a new file in this folder, instead of editing `CHANGELOG.md`. Two pull requests never edit the same file, so merging one of them doesn't put the others in conflict. When Kiota is released, a script collects the files into `CHANGELOG.md`.

## Add an entry

1. Create a Markdown file in one of these folders:
    - `changed/` for a bug fix or a change in behavior.
    - `added/` for a new feature.
2. Give it any name that is unlikely to clash with another pull request, for example your branch name: `changed/fix-ruby-keyword-parameters.md`.
3. Write the entry the way it should read in `CHANGELOG.md`: one line, written for Kiota users. A leading `- ` is optional, and you can link an issue as usual.

For example, `changed/fix-ruby-keyword-parameters.md`:

```markdown
Fixed Ruby generation for path parameters named after Ruby keywords, such as `{class}`. [#8531](https://github.com/microsoft/kiota/pull/8531)
```

To reword or drop an entry before it is released, edit or delete its file.

Pull requests where every commit is a `test:`, `ci:` or `chore:` commit don't need an entry.

## Preview the next release notes

```powershell
./scripts/merge-changelog-entries.ps1
```

This adds the pending entries under `## [Unreleased]` in your copy of `CHANGELOG.md` and keeps the files. The weekly preview build runs the same command, so the preview release notes list the pending entries. Run `git checkout CHANGELOG.md` to undo it locally.

## Release (maintainers)

Run the release script as before:

```powershell
./scripts/prepare-release.ps1 -version x.y.z
```

Besides bumping the version, it now:

- moves every entry in this folder, and any line still under `## [Unreleased]` in `CHANGELOG.md`, into a new `## [x.y.z] - date` section, in the order the entries were merged;
- deletes the entry files;
- fails if there is nothing to release, if the version is already in `CHANGELOG.md`, or if an entry is empty or in the wrong place.

Commit the updated `CHANGELOG.md` and the deleted entry files with the version bump.
