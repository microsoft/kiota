package com.microsoft.kiota;

import org.junit.jupiter.api.Test;
import static org.junit.jupiter.api.Assertions.*;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.ArgumentMatchers.anyString;
import static org.mockito.Mockito.mock;
import static org.mockito.Mockito.when;

import java.io.ByteArrayInputStream;
import java.io.IOException;
import java.io.InputStream;
import java.nio.charset.StandardCharsets;

import com.microsoft.kiota.serialization.ParseNodeFactoryRegistry;
import com.microsoft.kiota.serialization.ParseNode;
import com.microsoft.kiota.serialization.ParseNodeFactory;

class ParseNodeFactoryRegistryTest {
    @Test
    void getsVendorSpecificParseNodeFactory() throws IOException {
        final var registry = new ParseNodeFactoryRegistry();
        final var parseNodeFactoryMock = mock(ParseNodeFactory.class);
        final var parseNodeMock = mock(ParseNode.class);
        when(parseNodeFactoryMock.getValidContentType()).thenReturn("application/json");
        when(parseNodeFactoryMock.getParseNode(anyString(), any(InputStream.class))).thenReturn(parseNodeMock);
        final var str = "{\"test\":\"test\"}";
        try (final var payloadMock = new ByteArrayInputStream(str.getBytes(StandardCharsets.UTF_8))) {
            registry.contentTypeAssociatedFactories.put("application/json", parseNodeFactoryMock);
            final var parseNode = registry.getParseNode("application/vnd+json", payloadMock);
            assertNotNull(parseNode);
        }
    }
}
