<?php

namespace Microsoft\Kiota\Authentication\Test;

use GuzzleHttp\Client;
use GuzzleHttp\Handler\MockHandler;
use GuzzleHttp\Psr7\Request;
use GuzzleHttp\Psr7\Response;
use Microsoft\Kiota\Authentication\Oauth\AuthorizationCodeCertificateContext;
use Microsoft\Kiota\Authentication\Oauth\AuthorizationCodeContext;
use Microsoft\Kiota\Authentication\Oauth\ClientCredentialCertificateContext;
use Microsoft\Kiota\Authentication\Oauth\ClientCredentialContext;
use Microsoft\Kiota\Authentication\Oauth\OnBehalfOfCertificateContext;
use Microsoft\Kiota\Authentication\Oauth\OnBehalfOfContext;
use Microsoft\Kiota\Authentication\PhpLeagueAccessTokenProvider;
use PHPUnit\Framework\TestCase;

class PhpLeagueAccessTokenProviderTest extends TestCase
{
    public function testPassingEmptyScopesThrowsException(): void
    {
        $this->expectException(\InvalidArgumentException::class);
        $tokenProvider = new PhpLeagueAccessTokenProvider(new ClientCredentialContext('', '', ''), []);
    }

    public function testGetAuthorizationTokenWithSuccessfulTokenResponse(): void
    {
        $oauthContexts = $this->getOauthContexts();
        foreach ($oauthContexts as $tokenRequestContext) {
            $tokenProvider = new PhpLeagueAccessTokenProvider($tokenRequestContext, ['https://graph.microsoft.com/.default']);
            $mockResponses = [
                function (Request $request) use ($tokenRequestContext) {
                    $expectedUrl = 'https://login.microsoftonline.com/tenantId/oauth2/v2.0/token';
                    $this->assertEquals($expectedUrl, strval($request->getUri()));
                    $expectedBody = array_merge($tokenRequestContext->getParams(), [
                        'scope' => 'https://graph.microsoft.com/.default'
                    ]);
                    parse_str($request->getBody()->getContents(), $requestBodyMap);
                    $this->assertEquals($expectedBody, $requestBodyMap);
                    return new Response(200, [], json_encode(['access_token' => 'abc']));
                }
            ];
            $tokenProvider->getOauthProvider()->setHttpClient($this->getMockHttpClient($mockResponses));
            $this->assertEquals('abc', $tokenProvider->getAuthorizationTokenAsync('https://example.com/users')->wait());
        }
    }

    public function testGetAuthorizationTokenCachesInMemory(): void
    {
        $oauthContexts = $this->getOauthContexts();
        foreach ($oauthContexts as $tokenRequestContext) {
            $tokenProvider = new PhpLeagueAccessTokenProvider($tokenRequestContext, ['https://graph.microsoft.com/.default']);
            $mockResponses = [
                new Response(200, [], json_encode(['access_token' => 'abc', 'expires_in' => 5])),
                new Response(200, [], json_encode(['access_token' => 'xyz', 'expires_in' => 5]))
            ];
            $tokenProvider->getOauthProvider()->setHttpClient($this->getMockHttpClient($mockResponses));
            $this->assertEquals('abc', $tokenProvider->getAuthorizationTokenAsync('https://example.com')->wait());
            // Second call happens before token expires. We should get the existing access token
            $this->assertEquals('abc', $tokenProvider->getAuthorizationTokenAsync('https://example.com')->wait());
        }
    }

    public function testGetAuthorizationTokenRefreshesTokenIfExpired(): void
    {
        $oauthContexts = $this->getOauthContexts();
        foreach ($oauthContexts as $tokenRequestContext) {
            $tokenProvider = new PhpLeagueAccessTokenProvider($tokenRequestContext, ['https://graph.microsoft.com/.default']);
            $mockResponses = [
                new Response(200, [], json_encode(['access_token' => 'abc', 'expires_in' => 0.1, 'refresh_token' => 'refresh'])),
                function (Request $request) {
                    parse_str($request->getBody()->getContents(), $requestBodyMap);
                    $this->assertArrayHasKey('refresh_token', $requestBodyMap);
                    $this->assertEquals('refresh', $requestBodyMap['refresh_token']);
                    return new Response(200, [], json_encode(['access_token' => 'xyz', 'expires_in' => 1]));
                },
            ];
            $tokenProvider->getOauthProvider()->setHttpClient($this->getMockHttpClient($mockResponses));
            $this->assertEquals('abc', $tokenProvider->getAuthorizationTokenAsync('https://example.com')->wait());
            sleep(1);
            // Second call happens when token has already expired
            $this->assertEquals('xyz', $tokenProvider->getAuthorizationTokenAsync('https://example.com')->wait());
        }
    }

    public function testGetAuthorizationTokenFetchesNewTokenIfNoRefreshTokenExists(): void
    {
        $oauthContexts = $this->getOauthContexts();
        foreach ($oauthContexts as $tokenRequestContext) {
            $tokenProvider = new PhpLeagueAccessTokenProvider($tokenRequestContext, ['https://graph.microsoft.com/.default']);
            $mockResponses = [
                new Response(200, [], json_encode(['access_token' => 'abc', 'expires_in' => 0.1])),
                function (Request $request) use ($tokenRequestContext) {
                    parse_str($request->getBody()->getContents(), $requestBodyMap);
                    $expectedBody = array_merge($tokenRequestContext->getParams(), [
                        'scope' => 'https://graph.microsoft.com/.default'
                    ]);
                    $this->assertEquals($expectedBody, $requestBodyMap);
                    return new Response(200, [], json_encode(['access_token' => 'xyz', 'expires_in' => 1]));
                },
            ];
            $tokenProvider->getOauthProvider()->setHttpClient($this->getMockHttpClient($mockResponses));
            $this->assertEquals('abc', $tokenProvider->getAuthorizationTokenAsync('https://example.com')->wait());
            sleep(1);
            // Second call happens when token has already expired
            $this->assertEquals('xyz', $tokenProvider->getAuthorizationTokenAsync('https://example.com')->wait());
        }
    }

    private function getMockHttpClient(array $mockResponses): Client
    {
        return new Client(['handler' => new MockHandler($mockResponses)]);
    }

    private function getOauthContexts(string $tenantId = 'tenantId'): array
    {
        $clientId = 'clientId';
        $clientSecret = 'clientSecret';
        $certificatePath = __DIR__. DIRECTORY_SEPARATOR .'sample_cert.pem';
        $privateKeyPath = __DIR__ . DIRECTORY_SEPARATOR . 'sample_private_key.pem';
        $privateKeyPassphrase = 'pass';
        $authCode = '123';
        $redirectUri = 'http://localhost:1234';
        $assertion = 'jwtToken';

        return [
            new ClientCredentialContext($tenantId, $clientId, $clientSecret),
            new ClientCredentialCertificateContext($tenantId, $clientId, $certificatePath, $privateKeyPath, $privateKeyPassphrase),
            new AuthorizationCodeContext($tenantId, $clientId, $clientSecret, $authCode, $redirectUri),
            new AuthorizationCodeCertificateContext($tenantId, $clientId, $authCode, $redirectUri, $certificatePath, $privateKeyPath, $privateKeyPassphrase),
            new OnBehalfOfContext($tenantId, $clientId, $tenantId, $assertion),
            new OnBehalfOfCertificateContext($tenantId, $clientId, $assertion, $certificatePath, $privateKeyPath, $privateKeyPassphrase),
        ];
    }
}
