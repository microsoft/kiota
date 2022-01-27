<?php

namespace Microsoft\Kiota\Tests\Authentication;

use Microsoft\Kiota\Abstractions\Authentication\AllowedHostsValidator;
use PHPUnit\Framework\TestCase;

class AllowedHostsValidatorTest extends TestCase
{
    private AllowedHostsValidator $defaultValidator;

    protected function setUp(): void
    {
        $hosts = ["http://abc.com", "HTTPS://ABC.COM", "https://abc.com "];
        $this->defaultValidator = new AllowedHostsValidator($hosts);
        parent::setUp();
    }

    public function testConstructorSetsLowercaseTrimmedDeduplicatedHosts(): void
    {
        $expected = ["http://abc.com", "https://abc.com"]; //duplicates should not be added to allowed hosts
        $this->assertEquals($expected, $this->defaultValidator->getAllowedHosts());
    }

    public function testSetAllowedHostsSetLowercaseTrimmedDeduplicatedHosts(): void
    {
        $hosts = ["http://abc.com", "HTTPS://ABC.COM", "https://abc.com "];
        $validator = new AllowedHostsValidator();
        $validator->setAllowedHosts($hosts);
        $expected = ["http://abc.com", "https://abc.com"]; //duplicates should not be added to allowed hosts
        $this->assertEquals($expected, $validator->getAllowedHosts());
    }

    public function testIsUrlHostValidWithValidHost(): void
    {
        $this->assertTrue($this->defaultValidator->isUrlHostValid("https://abc.com"));
        $this->assertTrue($this->defaultValidator->isUrlHostValid("HTTPS://ABC.COM"));
        $this->assertTrue($this->defaultValidator->isUrlHostValid("https://abc.com  "));
    }

    public function testIsUrlHostValidWithEmptyAllowedHostsReturnsTrue(): void
    {
        $validator = new AllowedHostsValidator();
        $this->assertTrue($validator->isUrlHostValid("https://abc.com"));
    }

    public function testIsUrlHostValidThrowsExceptionWithInvalidUrl(): void
    {
        $this->expectException(\InvalidArgumentException::class);
        $this->defaultValidator->isUrlHostValid("http/abc?%#:8080");
    }

    public function testIsUrlHostValidThrowsExceptionIfUrlHasNoScheme(): void
    {
        $this->expectException(\InvalidArgumentException::class);
        $this->defaultValidator->isUrlHostValid("abc.com");
    }
}
