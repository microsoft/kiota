<?php
    namespace Integration\Test;
    use Integration\Test\Client\ApiClient;
    use Microsoft\Kiota\Authentication\Oauth\ClientCredentialContext;
    use Microsoft\Kiota\Authentication\PhpLeagueAuthenticationProvider;
    use Microsoft\Kiota\Http\GuzzleRequestAdapter;
    $tokenRequestContext = new ClientCredentialContext(
        'tenantId',
        'clientId',
        'clientSecret'
    );
    $authProvider = new PhpLeagueAuthenticationProvider($tokenRequestContext);
    $requestAdapter = new GuzzleRequestAdapter($authProvider);
    $client = new ApiClient($requestAdapter);
    echo "Hello {$client}";
?>