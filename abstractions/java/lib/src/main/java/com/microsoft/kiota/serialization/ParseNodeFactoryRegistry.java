package com.microsoft.kiota.serialization;

import java.io.InputStream;
import java.util.HashMap;
import java.util.Objects;

import javax.annotation.Nonnull;

public class ParseNodeFactoryRegistry implements ParseNodeFactory {
    public HashMap<String, ParseNodeFactory> contentTypeAssociatedFactories = new HashMap<>();
    @Override
    @Nonnull
    public ParseNode getParseNode(@Nonnull final String contentType, @Nonnull final InputStream rawResponse) {
        Objects.requireNonNull(contentType, "parameter contentType cannot be null");
        Objects.requireNonNull(rawResponse, "parameter rawResponse cannot be null");
        if(contentType.isEmpty()) {
            throw new NullPointerException("contentType cannot be empty");
        }
        if(contentTypeAssociatedFactories.containsKey(contentType)) {
            return contentTypeAssociatedFactories.get(contentType).getParseNode(contentType, rawResponse);
        } else {
            throw new RuntimeException("Content type " + contentType + " does not have a factory to be parsed");
        }
    }
}
