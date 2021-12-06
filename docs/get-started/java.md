---
parent: Get started with Kiota
---

# Required tools for Java

- [JDK 16](https://adoptopenjdk.net/)
- [Gradle 7](https://gradle.org/install/)

## Initializing target projects

Before you can compile and run the target project, you will need to initialize it. After initializing the test project, you will need to add references to the [abstraction](https://github.com/microsoft/kiota/tree/main/abstractions/java), [authentication](https://github.com/microsoft/kiota/tree/main/authentication/java/azure), [http](https://github.com/microsoft/kiota/tree/main/http/java/okhttp), and [serialization](https://github.com/microsoft/kiota/tree/main/serialization/java/json) packages from the GitHub feed.

Execute the following command in the directory you want to initialize the project in.

```shell
gradle init
# Select a console application
```

Edit `utilities/build.gradle` to add the following dependencies.

```groovy
api 'com.google.code.findbugs:jsr305:3.0.2'
api 'com.azure:azure-identity:1.2.5'
api 'com.squareup.okhttp3:okhttp:4.9.1'
api 'com.google.code.gson:gson:2.8.6'
```
