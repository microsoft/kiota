# To-do

![Java](https://github.com/microsoft/kiota/actions/workflows/serialization-java-text.yml/badge.svg)

- [ ] checkstyles
- [ ] spotbugs
- [ ] android api level linting
- [ ] javadoc
- [ ] cobertura

## Using the core implementations

1. In `build.gradle` in the `repositories` section:

    ```Groovy
    maven {
        url = uri("https://maven.pkg.github.com/microsoft/kiota")
        credentials {
            username = project.findProperty("gpr.user") ?: System.getenv("USERNAME")
            password = project.findProperty("gpr.key") ?: System.getenv("TOKEN")
        }
    }
    ```

1. In `build.gradle` in the `dependencies` section:

    ```Groovy
    api 'com.microsoft.kiota.serialization:kiota-text:1.+'
    ```

1. In `gradle.properties` next to the `build.gradle` file:

    ```Config
    gpr.user = your github username
    gpr.key = your PAT
    ```
