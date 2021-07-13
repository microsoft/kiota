package com.microsoft.kiota.serialization;

import java.io.InputStream;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;

public interface ParseNodeFactory {
    @Nonnull
    String getValidContentType();
    @Nonnull
    ParseNode getParseNode(@Nonnull final String contentType, @Nonnull final InputStream rawResponse);
}