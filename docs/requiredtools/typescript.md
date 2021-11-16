---
parent: Required tools
---

# Required tools for TypeScript

- [NodeJS 14](https://nodejs.org/en/)
- [TypeScript](https://www.typescriptlang.org/)

## Initializing target projects

Before you can compile and run the target project, you will need to initialize it. After initializing the test project, you will need to add references to the [abstraction](https://github.com/microsoft/kiota/tree/main/abstractions/typescript), [authentication](https://github.com/microsoft/kiota/tree/main/authentication/typescript/azure), [http](https://github.com/microsoft/kiota/tree/main/http/typescript/fetch), and [serialization](https://github.com/microsoft/kiota/tree/main/serialization/typescript/json) packages from the GitHub feed.

Clone a NodeJS/front end TypeScript starter like [this one](https://github.com/FreekMencke/node-typescript-starter).

```shell
npm i @azure/identity node-fetch
```
