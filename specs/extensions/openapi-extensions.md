# OpenAPI Extensions

[OpenAPI extensions](https://spec.openapis.org/oas/v3.0.3#specification-extensions) are custom properties that can be used to describe extra functionality that is not covered by the standard OpenAPI Specification.

Kiota supports OpenAPI extensions to help users extend OpenAPI documents to customize and augment the generated files.

## Generic extensions 

[x-logo](#x-logo)

[x-legal-info-url](#x-legal-info-url)

[x-privacy-info-url](#x-privacy-info-url)

## Plugin Specific Extensions 

[x-ai-description](#x-ai-description)

[x-ai-reasoning-instructions](#x-ai-reasoning-instructions)

[x-ai-responding-instructions](#x-ai-responding-instructions)

[x-openai-isConsequential](#x-openai-isconsequential)

## x-logo
Specifies the custom logo image.

Applies to: Info

```yaml
openapi: 3.0.0
info:
    title: Title
    description: Description
    version: 1.0.0
    x-logo:
        url: https://github.com/MyPlugin/icons/color.png
```

## x-legal-info-url
Specifies the link to to a document containing the terms of service.

Applies to: Info

```yaml
openapi: 3.0.0
info:
    title: Title
    description: Description
    version: 1.0.0
    x-legal-info-url: https://example.com/legal
```

## x-privacy-info-url
Specifies the link to the document containing the privacy policy.

Applies to: Info

```yaml
openapi: 3.0.0
info:
    title: Title
    description: Description
    version: 1.0.0
    x-privacy-info-url: https://example.com/privacy
```

## x-ai-description
Specifies the description for the plugin that will be provided to the model. This description should describe what the plugin is for, and in what circumstances its functions are relevant. Max size 2048 caracters.

Applies to: Info

```yaml
openapi: 3.0.0
info:
    title: Title
    description: Description
    version: 1.0.0
    x-ai-description: Suno is a tool that can generate songs based on a description. The user provides a description of the song they want to create, and Suno generates the song lyrics. Suno is a fun and creative way to create songs for any occasion.
```

## x-ai-reasoning-instructions
Specifies instructions for when a specific function is invoked when the orchestrator is in the resoning state.

Reasoning state corresponds to an orchestrator state when the model can call functions and do computations.

Applies to: Operations

```yaml
openapi: 3.0.0
info:
    title: Title
    description: Description
    version: 1.0.0
paths:
    /createSong:
        post:
            summary: Create a song given the user preferences
            operationId: createSong
            x-ai-reasoning-instructions:
                - 'Is the user requesting to create a song which would warrant the "Parental Advisory Explicit Content" label (e.g., topics about sex, politics, violence, self-harm, hate speech, coercion, etc.)? If so, I **must** invoke withdraw() and end the conversation.'
                - "Is the user requesting to create a haiku, poem, lyric, tune, melody, jingle, verse, sonnet, or something other than a song? If so, I **must not** invoke createSong(**params)."
                - "Is the user requesting to create a song on a safe and innocuous topic? If so, invoke createSong(**params)."
                - "If createSong(**params) is invoked already in this turn, I **will** not invoke the same tool again."
                - "Here are the parameters"
                - "topic(string, required): The requested song description, e.g., a country song about Thanksgiving, in the user's language"
```

## x-ai-responding-instructions
Specifies instructions for when a specific function is invoked when the orchestrator is in the responding state.

Responding state corresponds to an orchestrator state when the model can generate text that will be shown to the user. In this state the model cannot invoke functions.

Applies to: Operations

```yaml
openapi: 3.0.0
info:
    title: Title
    description: Description
    version: 1.0.0
paths:
    /createSong:
        post:
            summary: Create a song given the user preferences
            operationId: createSong
            x-ai-responding-instructions:
                - "The response to the user is generated asynchronously therefore I **SHOULD NOT** generate my own song or provide links or lyrics based on this tool's output. I can just let the user know their song will be available shortly and **stop responding** for that turn."
                - "I **should not** respond with either sample or full form of lyrics to the song because the song created by createSong(**params) is shown to the user directly along with the lyrics."
                - "If createSong(**params) was not invoked because the service was overloaded or the user reached their invocation limit, I **must** tell the user they can visit the https://app.suno.ai website to create more songs and display the link using Markdown syntax."
                - "I **shall** tell the user they can visit the https://app.suno.ai website to create more songs and display the link using Markdown syntax."
```

## x-openai-isConsequential
Specifies whether a confirmation dialog should be displayed before running the operation. Possible values are:

true: must always prompt the user for confirmation before running and don't show "Alaways allow" button
false: it will show the prompt for confirmation with the "Always allow" button

If the `x-openai-isConsequential` is not present, all GET operation will default to `false` and show the "Always allow" button; all other operations will default to `true` and must always prompt the user for confirmation.

Applies to: Operations

```yaml
openapi: 3.0.0
info:
    title: Title
    description: Description
    version: 1.0.0
paths:
    /createSong:
        post:
            summary: Create a song given the user preferences
            operationId: createSong
            description: description
            x-openai-isConsequential: true
```