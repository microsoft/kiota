<?php

namespace Microsoft\Kiota\Authentication\Test;

use GuzzleHttp\Client;
use GuzzleHttp\Handler\MockHandler;
use GuzzleHttp\Psr7\Request;
use GuzzleHttp\Psr7\Response;
use Microsoft\Kiota\Authentication\ClientCredentialSecretContext;
use Microsoft\Kiota\Authentication\PhpLeagueAccessTokenProvider;
use PHPUnit\Framework\TestCase;

class PhpLeagueAccessTokenProviderTest extends TestCase
{
    public function testPassingEmptyScopesThrowsException(): void
    {
        $this->expectException(\InvalidArgumentException::class);
        $tokenProvider = new PhpLeagueAccessTokenProvider(new ClientCredentialSecretContext('', '', ''), []);
    }

    public function testClientCredentialsSecretFlow(): void
    {
        $requestContext = new ClientCredentialSecretContext('tenantId', 'clientId', 'clientSecret');
        $tokenProvider = new PhpLeagueAccessTokenProvider($requestContext, ['https://graph.microsoft.com/.default']);
        $mockResponses = [
            function (Request $request) {
                $expectedUrl = 'https://login.microsoftonline.com/tenantId/oauth2/v2.0/token';
                $this->assertEquals($expectedUrl, strval($request->getUri()));
                $expectedBody = [
                    'client_id' => 'clientId',
                    'client_secret' => 'clientSecret',
                    'grant_type' => 'client_credentials',
                    'scope' => rawurlencode('https://graph.microsoft.com/.default')
                ];
                $requestBodyMap = $this->formUrlEncodedBodyToMap($request->getBody()->getContents());
                $this->assertEquals($expectedBody, $requestBodyMap);
                return new Response(200, [], json_encode(['access_token' => 'abc']));
            }
        ];
        $tokenProvider->getOauthProvider()->setHttpClient($this->getMockHttpClient($mockResponses));
        $tokenProvider->getAuthorizationTokenAsync('https://example.com/users')->wait();
    }

    private function getMockHttpClient(array $mockResponses): Client
    {
        return new Client(['handler' => new MockHandler($mockResponses)]);
    }

    private function formUrlEncodedBodyToMap(string $body): array
    {
        $result = [];
        foreach (explode('&', $body) as $item) {
            $pair = explode('=', $item);
            $result[$pair[0]] = $pair[1];
        }
        return $result;
    }
}
