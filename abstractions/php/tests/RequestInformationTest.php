<?php

namespace Microsoft\Kiota\Tests;
use DateTime;
use Microsoft\Kiota\Abstractions\RequestInformation;
use PHPUnit\Framework\TestCase;

class RequestInformationTest extends TestCase {
    private RequestInformation $requestInformation;

    protected function setUp(): void {
        $this->requestInformation = new RequestInformation();
    }

    public function testSetUri(): void{
        $pathParameters = [
            'baseUrl' => 'https://google.com',
            'user' => 'silas',
        ];
        $queryParameters = ['startDate' => (new DateTime('now'))->format('Y-m-d')];
        $this->requestInformation->urlTemplate = '{+baseUrl}/{user}/mails/?startDate=\'{startDate}\'';
        $this->requestInformation->pathParameters = $pathParameters;
        $this->requestInformation->queryParameters = $queryParameters;
        $this->assertEquals("https://google.com/silas/mails/?startDate='2022-01-17'", $this->requestInformation->getUri());
    }
}