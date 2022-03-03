---
parent: Get started
---

# Register an application for Microsoft identity platform authentication

To be able to authenticate with the Microsoft identity platform and get an access token for Microsoft Graph, you will need to create an application registration. You can install the [Microsoft Graph PowerShell SDK](https://github.com/microsoftgraph/msgraph-sdk-powershell) and use it to create the app registration, or register the app manually in the Azure Active Directory admin center.

The following instructions register an app and enable [device code flow](https://docs.microsoft.com/azure/active-directory/develop/v2-oauth2-device-code) for authentication. This is the authentication method used by the sample apps in this section.

## Use PowerShell

```powershell
$app = New-MgApplication -displayName "NativeGraphApp" -IsFallbackPublicClient
```

Save the value of the `AppId` property of the `$app` object.

```powershell
> $app.AppId
1cddd83e-eda6-4c65-bccf-920a86f220ab
```

## Register manually

1. Open a browser and navigate to the [Azure Active Directory admin center](https://aad.portal.azure.com). Login with your Azure account.
1. Select **Azure Active Directory** in the left-hand navigation, then select **App registrations** under **Manage**.
1. Select **New registration**. On the **Register an application** page, set the values as follows.

    - Set **Name** to `NativeGraphApp`.
    - Set **Supported account types** to **Accounts in any organizational directory and personal Microsoft accounts**.
    - Leave **Redirect URI** blank.

1. Select **Register**. On the **Overview** page, copy the value of the **Application (client) ID** and save it.
1. Select **Authentication** under **Manage**.
1. Locate the **Advanced settings** section. Set the **Allow public client flows** toggle to **Yes**, then select **Save**.
