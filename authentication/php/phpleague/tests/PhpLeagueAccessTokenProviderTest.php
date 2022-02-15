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

    public function testGetAuthorizationCodeCachesInMemory(): void
    {
        $requestContext = new ClientCredentialSecretContext('tenantId', 'clientId', 'clientSecret');
        $tokenProvider = new PhpLeagueAccessTokenProvider($requestContext, ['https://graph.microsoft.com/.default']);
        $mockResponses = [
            new Response(200, [], json_encode(['access_token' => 'abc', 'expires_in' => 5])),
            new Response(200, [], json_encode(['access_token' => 'xyz', 'expires_in' => 5]))
        ];
        $tokenProvider->getOauthProvider()->setHttpClient($this->getMockHttpClient($mockResponses));
        $this->assertEquals('abc', $tokenProvider->getAuthorizationTokenAsync('https://example.com')->wait());
        // Second call happens before token expires. We should get the existing access token
        $this->assertEquals('abc', $tokenProvider->getAuthorizationTokenAsync('https://example.com')->wait());
    }

    public function testGetAuthorizationCodeRefreshesTokenIfExpired(): void
    {
        $requestContext = new ClientCredentialSecretContext('tenantId', 'clientId', 'clientSecret');
        $tokenProvider = new PhpLeagueAccessTokenProvider($requestContext, ['https://graph.microsoft.com/.default']);
        $mockResponses = [
            new Response(200, [], json_encode(['access_token' => 'abc', 'expires_in' => 1, 'refresh_token' => 'refresh'])),
            function (Request $request) {
                $requestBodyMap = $this->formUrlEncodedBodyToMap($request->getBody()->getContents());
                $this->assertArrayHasKey('refresh_token', $requestBodyMap);
                $this->assertEquals('refresh', $requestBodyMap['refresh_token']);
                return new Response(200, [], json_encode(['access_token' => 'xyz', 'expires_in' => 1]));
            },
        ];
        $tokenProvider->getOauthProvider()->setHttpClient($this->getMockHttpClient($mockResponses));
        $this->assertEquals('abc', $tokenProvider->getAuthorizationTokenAsync('https://example.com')->wait());
        sleep(2);
        // Second call happens when token has already expired
        $this->assertEquals('xyz', $tokenProvider->getAuthorizationTokenAsync('https://example.com')->wait());
    }

    public function testGetAuthorizationCodeFetchesNewTokenIfNoRefreshTokenExists(): void
    {
        $requestContext = new ClientCredentialSecretContext('tenantId', 'clientId', 'clientSecret');
        $tokenProvider = new PhpLeagueAccessTokenProvider($requestContext, ['https://graph.microsoft.com/.default']);
        $mockResponses = [
            new Response(200, [], json_encode(['access_token' => 'abc', 'expires_in' => 1])),
            function (Request $request) {
                $requestBodyMap = $this->formUrlEncodedBodyToMap($request->getBody()->getContents());
                $expectedBody = [
                    'client_id' => 'clientId',
                    'client_secret' => 'clientSecret',
                    'grant_type' => 'client_credentials',
                    'scope' => rawurlencode('https://graph.microsoft.com/.default')
                ];
                $this->assertEquals($expectedBody, $requestBodyMap);
                return new Response(200, [], json_encode(['access_token' => 'xyz', 'expires_in' => 1]));
            },
        ];
        $tokenProvider->getOauthProvider()->setHttpClient($this->getMockHttpClient($mockResponses));
        $this->assertEquals('abc', $tokenProvider->getAuthorizationTokenAsync('https://example.com')->wait());
        sleep(2);
        // Second call happens when token has already expired
        $this->assertEquals('xyz', $tokenProvider->getAuthorizationTokenAsync('https://example.com')->wait());
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
