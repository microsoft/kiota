<?php
    namespace Integration\Test;
    require_once 'src/client/ApiClient.php';
    use Microsoft\Kiota\Authentication\Oauth\ClientCredentialContext;
    use Microsoft\Graph\Core\Authentication\GraphPhpLeagueAuthenticationProvider;
    use  Microsoft\Kiota\Http\GuzzleRequestAdapter;
    $tokenRequestContext = new ClientCredentialContext(
        'tenantId',
        'clientId',
        'clientSecret'
    );
    $authProvider = new GraphPhpLeagueAuthenticationProvider($tokenRequestContext);
    $requestAdapter = new GuzzleRequestAdapter($authProvider);
    $client = new ApiClient($requestAdapter);
    echo "Hello {$client}";
?>