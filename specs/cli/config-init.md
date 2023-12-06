# `kiota config init`

## Description

`kiota config init` creates a new kiota.config file with the provided parameters. If the file already exists, it should error out and report it to the user. As the file gets created, it should be adding a `version` property with the value of the `kiota.config` current schema version. 

When `kiota config init` is executed, a `kiota.config` file would be created in the current directory where the command is being executed. If the user wants to create the file in a different directory, they should use the `--config-file` global parameter.

> [!NOTE] 
> If a project only needs a single API, using `kiota config init` is not mandatory as generating code using the `kiota client generate` command could generate a `kiota.config` file with values coming from the `kiota client generate` command (if no `kiota.config` is present). See [kiota client generate](./client-generate.md) for more information.

## Parameters

| Parameters | Required | Example | Description |
| -- | -- | -- | -- |
| `--config-file \| --cf` | No | ../../kiota.config | Path to an existing `kiota.config` file. Defaults to `./` |

## Using `kiota config init`

```bash
kiota config init
```
_Results in the following `kiota.config` file:_
```json
{
  "version": "1.0.0",
}
```