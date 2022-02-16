<?php

namespace Microsoft\Kiota\Authentication\Test;

use GuzzleHttp\Client;
use GuzzleHttp\Handler\MockHandler;
use GuzzleHttp\Psr7\Request;
use GuzzleHttp\Psr7\Response;
use Microsoft\Kiota\Authentication\Oauth\ClientCredentialContext;
use Microsoft\Kiota\Authentication\PhpLeagueAccessTokenProvider;
use PHPUnit\Framework\TestCase;

class PhpLeagueAccessTokenProviderTest extends TestCase
{
    public function testPassingEmptyScopesThrowsException(): void
    {
        $this->expectException(\InvalidArgumentException::class);
        $tokenProvider = new PhpLeagueAccessTokenProvider(new ClientCredentialContext('', '', ''), []);
    }

    public function testClientCredentialsSecretFlow(): void
    {
        $requestContext = new ClientCredentialContext('tenantId', 'clientId', 'clientSecret');
        $tokenProvider = new PhpLeagueAccessTokenProvider($requestContext, ['https://graph.microsoft.com/.default']);
        $mockResponses = [
            function (Request $request) {
                $expectedUrl = 'https://login.microsoftonline.com/tenantId/oauth2/v2.0/token';
                $this->assertEquals($expectedUrl, strval($request->getUri()));
                $expectedBody = [
                    'client_id' => 'clientId',
                    'client_secret' => 'clientSecret',
                    'grant_type' => 'client_credentials',
                    'scope' => 'https://graph.microsoft.com/.default'
                ];
                parse_str($request->getBody()->getContents(), $requestBodyMap);
                $this->assertEquals($expectedBody, $requestBodyMap);
                return new Response(200, [], json_encode(['access_token' => 'abc']));
            }
        ];
        $tokenProvider->getOauthProvider()->setHttpClient($this->getMockHttpClient($mockResponses));
        $tokenProvider->getAuthorizationTokenAsync('https://example.com/users')->wait();
    }

    public function testGetAuthorizationCodeCachesInMemory(): void
    {
        $requestContext = new ClientCredentialContext('tenantId', 'clientId', 'clientSecret');
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
        $requestContext = new ClientCredentialContext('tenantId', 'clientId', 'clientSecret');
        $tokenProvider = new PhpLeagueAccessTokenProvider($requestContext, ['https://graph.microsoft.com/.default']);
        $mockResponses = [
            new Response(200, [], json_encode(['access_token' => 'abc', 'expires_in' => 1, 'refresh_token' => 'refresh'])),
            function (Request $request) {
                parse_str($request->getBody()->getContents(), $requestBodyMap);
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
        $requestContext = new ClientCredentialContext('tenantId', 'clientId', 'clientSecret');
        $tokenProvider = new PhpLeagueAccessTokenProvider($requestContext, ['https://graph.microsoft.com/.default']);
        $mockResponses = [
            new Response(200, [], json_encode(['access_token' => 'abc', 'expires_in' => 1])),
            function (Request $request) {
                parse_str($request->getBody()->getContents(), $requestBodyMap);
                $expectedBody = [
                    'client_id' => 'clientId',
                    'client_secret' => 'clientSecret',
                    'grant_type' => 'client_credentials',
                    'scope' => 'https://graph.microsoft.com/.default'
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
}
