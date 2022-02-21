<?php

namespace Microsoft\Kiota\Authentication\Test;

use Microsoft\Kiota\Authentication\Oauth\ClientCredentialContext;
use Microsoft\Kiota\Authentication\PhpLeagueAuthenticationProvider;
use PHPUnit\Framework\TestCase;

class PhpLeagueAuthenticationProviderTest extends TestCase
{
    private PhpLeagueAuthenticationProvider $defaultAuthProvider;

    protected function setUp(): void
    {
        $this->defaultAuthProvider = new PhpLeagueAuthenticationProvider(
            new ClientCredentialContext('tenantId', 'clientId', 'secret'),
            ['https://graph.microsoft.com/.default']
        );
    }

    public function testCorrectOauthProviderEndpointsExposed(): void
    {
        $expected = "https://login.microsoftonline.com/tenantId/oauth2/v2.0/authorize";
        $this->assertEquals($expected, $this->defaultAuthProvider->getAccessTokenProvider()->getOauthProvider()->getBaseAuthorizationUrl());
    }
}
