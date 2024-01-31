# `kiota config init`

## Description

`kiota config init` creates a new kiota-config.json file with the provided parameters. If the file already exists, it should error out and report it to the user. As the file gets created, it should be adding a `version` property with the value of the `kiota-config.json` current schema version. 

When `kiota config init` is executed, a `kiota-config.json` file would be created in the current directory where the command is being executed. If the user wants to create the file in a different directory, they should use the `--config-file` global parameter.

> [!NOTE] 
> If a project only needs a single API, using `kiota config init` is not mandatory as generating code using the `kiota client generate` command could generate a `kiota-config.json` file with values coming from the `kiota client generate` command (if no `kiota-config.json` is present). See [kiota client generate](./client-generate.md) for more information.

## Parameters

None.

## Using `kiota config init`

```bash
kiota config init
```

_The resulting `kiota-config.json` file will look like this:_

```jsonc
{
  "version": "1.0.0",
}
```

## File structure
```bash
/
 └─kiota-config.json
```