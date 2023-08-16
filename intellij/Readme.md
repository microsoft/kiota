# Kiota IntelliJ PlugIn
<!-- Plugin description -->
This is a description for the Kiota IntelliJ plugin. This plugin is used to generate an API client to call any OpenAPI described API you are interested in from a Microsoft Graph OpenAPI specification.
<!-- Plugin description end -->
This readme documents how to get started with the kiota Intellij plugin
# Getting Started
## Installation
**Gradle** : Visit the [Gradle Webside](https://gradle.org/install/) and install Gradle `version 7.4`   
**DotNet**: Visit the [official .NET website](https://dotnet.microsoft.com/en-us/download) and install .NET SDK  
**kiota** : Install [kiota](https://learn.microsoft.com/en-us/openapi/kiota/install#install-as-net-tool) as a dotnet tool  
**IntelliJ IDEA Community Edition :** Install [IntelliJ IDEA Community Edition](https://www.jetbrains.com/idea/download)  
## Clone the Repo

1. Clone the [kiota repository](https://www.jetbrains.com/idea/download) 
2. Checkout the `intellijplugin branch`  (git checkout intellijplugin)

## The Plugin
**Configure Project Dependencies:**
- In the intellijplugin project Go to `File` > `Project Structure`.
- On the left panel, click on `Modules`.
- Under `Dependencies`, select `Corretto-17 (Amazon Corretto version 17.0.7)`.

**Run plugin.xml:**  
    - Navigate to the `META-INF` directory in your project.  
    - Find and run the `plugin.xml` file.

**Open Plugin Project:**  
    - When IntelliJ IDEA pops up, create a new blank project.
    - Select `Gradle` and `Groovy` as the project options.

**Access kiotaToolFactory:**  
    - On the left-hand side, find and click on the `kiotaToolFactory` tab.






