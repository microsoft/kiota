# Kiota Manifest Generation

Kiota generates client code for an API. With the advancement of the AI, API clients are not the only way one can consume an API. Kiota as a tool that outputs generated files for interacting with an API, should extend its support to also accomplish AI scenarios.

Today's AI models can easily generate messages and images for users. While this is helpful when building a simple chat app, it is not enough to build fully automated AI agents that can automate business processe and needs specific to one's company and empower users to achieve more. To do so, users need to combine AI models with other sources, such as APIs.

OpenAI has defined [OpenAI plugins](https://platform.openai.com/docs/plugins/introduction), one way to enable GPT to interact with APIs, allowing it to perform several actions. To build a plugin, it's necessary to create a [manifest file](https://platform.openai.com/docs/plugins/getting-started/plugin-manifest) that defines relevant metadata information that allows GPT to call an API.

In addition to OpenAI manifest, [API Manifest](https://www.ietf.org/archive/id/draft-miller-api-manifest-01.html) is another way to declare dependencies of APIs and their characteristics. API Manifest addresses a limitation present in both the OpenAI manifest and OpenAPI document, it can references one or more OpenAPI descriptions as dependencies.
For developers using [Semantic Kernel](
https://learn.microsoft.com/en-us/semantic-kernel/overview/) as their AI orchestractor, [API Manifest is supported as a input format](https://github.com/microsoft/semantic-kernel/pull/4961), in preview state, for plugin generation.


## Current Challenges

- Creating custom GPTs and AI plugins for existing APIs, specially for big APIs, requires great amount of effort.
- Kiota is currently specialized in client code generation and it doesn't meet the market expansion for AI.

## Goals

- Enable developers to customize Copilot to be more helpful in their daily lives, at specific tasks, at work, at home by providing tools to output OpenAI Manifests. 
- Enable developers to generate API Manifest that can be converted into Semantic Kernel API Manifest Plugins. 

## Proposal

We should introduce new commands to manage different types of manifests. Also a `manifests` entry should be added to [Kiota config](kiota-config.md).

Here is an example of what the kiota-config.json file could look like.

```jsonc
{
  "version": "1.0.0",
  "clients": { ... }, //if any
  "manifests": {
    "GitHub": {
      "descriptionLocation": "https://raw.githubusercontent.com/github/rest-api-description/main/descriptions/api.github.com/api.github.com.json",
      "includePatterns": ["/repos/{owner}/{repo}"],
      "excludePatterns": [],
      "type": "openai",
      "outputDirectory": "./generated/manifests/github",
      "overlayDirectory": "./overlays/manifests/github/overlay.yaml"
    }
  }
}
```

## Commands

In addition to managing clients:
* [kiota client add](../cli/client-add.md)
* [kiota client edit](../cli/client-edit.md)
* [kiota client generate](../cli/client-generate.md)
* [kiota client remove](../cli/client-remove.md)

We will provide manifest commands:
* [kiota manifest add](../cli/manifest-add.md)
* [kiota manifest edit](../cli/manifest-edit.md)
* [kiota manifest generate](../cli/manifest-generate.md)
* [kiota manifest remove](../cli/manifest-remove.md)


## End-to-end experience

### Add a manifest

```bash
kiota manifest add --manifest-name "GitHub" --openapi "https://raw.githubusercontent.com/github/rest-api-description/main/descriptions/api.github.com/api.github.com.json" --include-path "/repos/{owner}/{repo}" --type openai --output "./generated/manifests/github"
```

### Edit a manifest

```bash
kiota manifest edit --manifest-name "GitHub" --exclude-path "/repos/{owner}/{repo}#DELETE"
```

### Remove a manifest

```bash
kiota manifest remove --manifest-name "GitHub" --clean-output
```

### Generate all manifests

```bash
kiota manifest generate
```
