<?php

namespace Microsoft\Kiota\Abstractions\Tests;
use Microsoft\Kiota\Abstractions\RequestInformation;
use PHPUnit\Framework\TestCase;
use Microsoft\Kiota\Abstractions\QueryParameter;
use Rize\UriTemplate;

class RequestInformationTest extends TestCase {
    private RequestInformation $requestInformation;

    protected function setUp(): void {
        $this->requestInformation = new RequestInformation();
    }

    public function testSetUri(): void{
        $pathParameters = [
            'baseUrl' => 'https://google.com',
            'user%2Did' => 'silas',
        ];
        $queryParameters = ['%24select' => ['subject', 'importance']];
        $this->requestInformation->urlTemplate = '{+baseUrl}/{user%2Did}/mails{?%24select}';
        $this->requestInformation->pathParameters = $pathParameters;
        $this->requestInformation->queryParameters = $queryParameters;
        $this->assertEquals("https://google.com/silas/mails?%24select=subject,importance", $this->requestInformation->getUri());
    }

    public function testSetQueryParameters(): void {
        $this->requestInformation->urlTemplate = '{?%24select,top}';

        $queryParam = new TestQueryParameter();
        $this->requestInformation->setQueryParameters($queryParam);
        $this->assertEquals('?top=10', $this->requestInformation->getUri());
        $this->assertTrue(sizeof($this->requestInformation->queryParameters) == 1);
        $queryParam->select = ['displayName', 'age'];
        $this->requestInformation->setQueryParameters($queryParam);
        $this->assertTrue(sizeof($this->requestInformation->queryParameters) == 2);
        $this->assertArrayHasKey('%24select', $this->requestInformation->queryParameters);
        $this->assertEquals(['displayName', 'age'], $this->requestInformation->queryParameters['%24select']);
        $this->assertArrayHasKey('top', $this->requestInformation->queryParameters);
        $this->assertEquals('?%24select=displayName,age&top=10', $this->requestInformation->getUri());
    }
}

class TestQueryParameter {
    /**
     * @QueryParameter("%24select")
     */
    public ?array $select = null;
    public int $top = 10; // no annotation
}