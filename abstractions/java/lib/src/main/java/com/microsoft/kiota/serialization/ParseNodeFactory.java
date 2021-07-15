package com.microsoft.kiota.serialization;

import java.io.InputStream;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;
/**
 * Defines the contract for a factory that is used to create {@link ParseNode}s.
 */
public interface ParseNodeFactory {
    /**
     * Returns the content type this factory's parse nodes can deserialize.
     */
    @Nonnull
    String getValidContentType();
    /**
     * Creates a {@link ParseNode} from the given {@link InputStream} and content type.
     * @param inputStream the {@link InputStream} to read from.
     * @param contentType the content type of the {@link InputStream}.
     * @return a {@link ParseNode} that can deserialize the given {@link InputStream}.
     */
    @Nonnull
    ParseNode getParseNode(@Nonnull final String contentType, @Nonnull final InputStream rawResponse);
}