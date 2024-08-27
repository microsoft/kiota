# `kiota workspace init`

## Description

`kiota workspace init` creates a new workspace.json file with the provided parameters. If the file already exists, it should error out and report it to the user. As the file gets created, it should be adding a `version` property with the value of the `workspace.json` current schema version. 

When `kiota workspace init` is executed, a `workspace.json` file would be created in the current directory where the command is being executed. If the user wants to create the file in a different directory, they should use the `--workspace-file` global parameter.

> [!NOTE] 
> If a project only needs a single API, using `kiota workspace init` is not mandatory as generating code using the `kiota client generate` or `kiota plugin generate` command could generate a `workspace.json` file with values coming from the `kiota client generate` or `kiota plugin generate` commands (if no `workspace.json` is present). See [kiota client generate](./client-generate.md) or [kiota plugin generate](./plugin-generate.md) for more information.

## Parameters

None.

## Using `kiota workspace init`

```bash
kiota workspace init
```

_The resulting `workspace.json` file will look like this:_

```jsonc
{
  "version": "1.0.0",
}
```

## File structure
```bash
/
 └─.kiota
    └─workspace.json
```