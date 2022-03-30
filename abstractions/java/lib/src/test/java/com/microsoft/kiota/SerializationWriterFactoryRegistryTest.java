package com.microsoft.kiota;

import org.junit.jupiter.api.Test;
import static org.junit.jupiter.api.Assertions.*;
import static org.mockito.ArgumentMatchers.anyString;
import static org.mockito.Mockito.mock;
import static org.mockito.Mockito.when;

import java.io.IOException;

import com.microsoft.kiota.serialization.SerializationWriterFactoryRegistry;
import com.microsoft.kiota.serialization.SerializationWriter;
import com.microsoft.kiota.serialization.SerializationWriterFactory;

class SerializationWriterFactoryRegistryTest {
    @Test
    void getsVendorSpecificSerializationWriterFactory() throws IOException {
        final var registry = new SerializationWriterFactoryRegistry();
        final var serializationWriterFactoryMock = mock(SerializationWriterFactory.class);
        final var serializationWriterMock = mock(SerializationWriter.class);
        when(serializationWriterFactoryMock.getValidContentType()).thenReturn("application/json");
        when(serializationWriterFactoryMock.getSerializationWriter(anyString())).thenReturn(serializationWriterMock);
        registry.contentTypeAssociatedFactories.put("application/json", serializationWriterFactoryMock);
        final var parseNode = registry.getSerializationWriter("application/vnd+json");
        assertNotNull(parseNode);
    }
}
