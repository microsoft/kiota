---
parent: Kiota deep dive
---

# Status code mapping

## Generation

During the client generation, Kiota follows these rules to map status codes described by the OpenAPI description. The following table is ordered which means the first rule that matches will be the one used during generation.

| Code | Schema is present | Result type |
| ---- | ----------- | ------ |
| 200-203 | yes | model class |
| 2XX | yes | model class |
| 204, 205 | N/A | void |
| 201, 202 | no | void |
| 200, 203, 206 | no | stream |
| 2XX | no | stream |

> Note: for a schema to be considered present, it must be part of a structured content response ("application/json", "application/xml", "text/plain", "text/xml", "text/yaml").

## Runtime

At runtime the client can encounter response status codes that differ from what was originally documented due to how HTTP works. Default request adapters implementations follow these rules:

| Expected return type | Response status code | Response body is present | Returned value |
| ----------------------- | ----------------------- | ------------------------- | ---------------- |
| model class | 200-203 | yes | deserialized value |
| model class | 200-202, 204, 205 | no | null |
| void | 200-205 | N/A | void |
| stream | 200-203, 206 | yes | stream |
| stream | 200-205 | no | null |
