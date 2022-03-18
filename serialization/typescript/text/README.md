# To-do

![TypeScript](https://github.com/microsoft/kiota/actions/workflows/serialization-typescript-text.yml/badge.svg)

- [ ] browserlist configuration for compat
- [ ] eslint configuration for linting
- [ ] unit tests (chai + mocha + chai as promised + some coverage reporter)
- [ ] doc comments

## Using the Serialization text implementations

1. Add a `.npmrc` file with the following content

    ```Config
    @microsoft:registry=https://npm.pkg.github.com/
    ```

1. `npm login --scope=@microsoft --registry=https://npm.pkg.github.com` (use a token with package:read, repo and SSO enabled for the Microsoft organization as the password)
1. `npm i @microsoft/kiota-serialization-text -S`.
