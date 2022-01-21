---
parent: Kiota deep dive
---

# Authentication with Kiota clients

Most REST APIs are protected through some kind of authentication and authorization scheme. The default HTTP core services provided by Kiota require an Authentication Provider to be passed to handle authentication concerns.

## Authentication Provider interface

The authentication providers are required to implement the following contract/interface defined in the abstraction library.

```csharp
public interface IAuthenticationProvider
{
    Task AuthenticateRequest(RequestInformation request);
}
```

Where the request parameter is the abstract request to be executed. And the return value must be a Task that completes whenever the authentication sequence has completed and the request object has been updates with the additional authentication/authorization information.

## Access Token Provider interface

Implementations of this interface can be used in combination with the BaseBearerTokenAuthentication provider class to provide the access token to the request before it's executed. They can also be used to retrieve an access token to build and execute arbitrary requests with a native http client.

```csharp
public interface IAccessTokenProvider
{
    Task<string> GetAuthorizationToken(Uri uri);
}
```

## Base bearer token authentication provider

A common practice in the industry for APIs is to implement authentication and authorization via the `Authorization` request header with a bearer token value.

Should you want to add support for additional authentication providers for that scheme, Kiota abstractions already offer a class to use in combination with available or your own implementation of Access Token Provider so you only need to implement the access token acquisition sequence and not the header composition/addition.

```csharp
public class BaseBearerTokenAuthenticationProvider
{
    public BaseBearerTokenAuthenticationProvider(IAccessTokenProvider tokenProvider) {

    }
    public IAccessTokenProvider AccessTokenProvider {get; private set;};
}
```

Where the request parameter is the abstract request to be executed. And the return value is a Task that holds the access token, or null if the request could/should not be authenticated.

> **Note:** Please leverage the same approach if you want to add support for new authentication schemes where the authentication scheme composition logic is implemented in a base class so it can be reused across multiple providers.

## Azure Identity Authentication Provider

The additional azure authentication package contains an authentication provider that relies on Azure Identity to get access tokens and implements bearer authentication. It can effectively be used for any client making requests to APIs secured by the Microsoft/Azure Identity Platform.

> **Note:** Please leverage an approach of a one authentication provider per package should you submit pull requests adding new authentication providers. This approach helps keep the code modular and avoids mixing dependencies.

## Anonymous Authentication Provider

Some APIs do not require any authentication and can be queried anonymously. For this reasons the abstraction package also provide an `AnonymousAuthenticationProvider` which serves as a placeholder and performs no operation.

## Choose your authentication provider

1. Does the target API require authentication? No - Use the Anonymous Authentication Provider, Yes - keep reading
1. Is the authentication implemented that Authorization bearer token header? No - Implement a custom authentication provider, Yes - keep reading
1. Is the API protected by Microsoft Identity Platform? No - Implement a custom authentication provider, Yes - Use the Azure Identity Authentication Provider

> **Note:** if you end up implementing a custom authentication provider and you think this provider could be useful to others, do not hesitate to send us a pull request!
