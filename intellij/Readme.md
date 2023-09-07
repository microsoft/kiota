# Kiota IntelliJ PlugIn
<!-- Plugin description -->
Kiota IntelliJ plugin is used to generate an API client to call any OpenAPI described API you are interested in from an OpenAPI specification.
One of the goals of the project is to provide the best code generator support possible for OpenAPI and JSON Schema features from IntelliJ platform.
<!-- Plugin description end -->

This plugin is built upon[`intellij-platform-plugin-template`](https://github.com/JetBrains/intellij-platform-plugin-template). This readme documents how to get started with the Kiota Intellij plugin
# Getting Started
## Installation ##
**Gradle** : Visit the [Gradle Webside](https://gradle.org/install/) and install Gradle `version 7.4`   
**DotNet**: Visit the [official .NET website](https://dotnet.microsoft.com/en-us/download) and install .NET SDK  
**Kiota** : Install [kiota](https://learn.microsoft.com/en-us/openapi/kiota/install#install-as-net-tool) as a dotnet tool  
**IntelliJ IDEA Community Edition :** Install [IntelliJ IDEA Community Edition](https://www.jetbrains.com/idea/download)  
## Clone the Repo
1. Clone the [Kiota repository](https://github.com/microsoft/kiota) 
2. Checkout the **intellijplugin** branch: `git checkout intellijplugin` or

## Open the plugin from IntelliJ IDEA ##

1. From **IntelliJ IDEA** click on **Open**
2. Navigate to the **kiota** root directory
3. Navigate to **intellij directory**

## Configure Project Dependencies: ##
- In your Intellijplugin project go to **File** > **Project Structure**
- On the left panel, click on `Modules`
- Under `Dependencies`, click on **Add SDK** from the **Module SDK** dropdown
- click on **Download SDK**
- Select `Corretto-17 (Amazon Corretto version 17.0.8)`   

## Run plugin: ##
- Navigate to [`intellij/intellij/src/main/resources/META-INF/plugin.xml`](https://github.com/microsoft/kiota/blob/intellijplugin/intellij/src/main/resources/META-INF/plugin.xml) in your project's to access the `plugin.xml` file
- select **run plugin** from the top dropdown menu
- This starts a new instance of **Intelli IDEA** with the **intellijTestPlugin Template plugin** installed

## Open Plugin : ##
1. When IntelliJ IDEA opens, select **New Project**.
2. Provide a **Name** for the project.
3. Select **Groovy** for the **Language** input.
4. Select **Gradle** for the **Build system** input.
5. Leave the other default selections and click on **Create**.

## Use the Kiota IntelliJ Plugin ##  
Wait for the environment to get indexed before you can use the plugin.
   1. Select the **KiotaToolFactory** tab on the left side of the environment. This is the Kiota IntelliJ Plugin.
   2. Set a path to an OpenAPI description file.
   3. Set an output path. We suggest that you set a path to the root of the blank project you created in the previous step.
   4. Leave Java as the default language.
   5. Set a client class name. If you don't provide a value, *ApiClient* will be used.
   6. Set a namespace for the generated client classes.
   7. Select the **Generate** button. You can find your generated files at the specified output location.