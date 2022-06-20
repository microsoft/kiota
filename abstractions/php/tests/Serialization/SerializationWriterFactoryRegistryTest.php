<?php

namespace Microsoft\Kiota\Abstractions\Tests\Serialization;

use Microsoft\Kiota\Abstractions\Serialization\SerializationWriter;
use Microsoft\Kiota\Abstractions\Serialization\SerializationWriterFactory;
use Microsoft\Kiota\Abstractions\Serialization\SerializationWriterFactoryRegistry;
use PHPUnit\Framework\TestCase;

class SerializationWriterFactoryRegistryTest extends TestCase
{
    private SerializationWriterFactoryRegistry $factoryRegistry;

    protected function setUp(): void
    {
       $this->factoryRegistry = new SerializationWriterFactoryRegistry();
    }

    public function testVendorSpecificContentType(): void {
        $this->factoryRegistry->contentTypeAssociatedFactories['application/json'] = $this->createMock(SerializationWriterFactory::class);
        $contentType = $this->factoryRegistry->getSerializationWriter("application/vnd+json");
        $this->assertNotNull($contentType);
    }
}
