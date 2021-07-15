package com.microsoft.kiota.serialization;

import java.io.InputStream;
import java.util.HashMap;
import java.util.Objects;

import javax.annotation.Nonnull;

/**
 * This factory holds a list of all the registered factories for the various types of nodes.
 */
public class ParseNodeFactoryRegistry implements ParseNodeFactory {
    /** Default singleton instance of the registry to be used when registring new factories that should be available by default. */
    public static final ParseNodeFactoryRegistry defaultInstance = new ParseNodeFactoryRegistry();
    /** List of factories that are registered by content type. */
    public HashMap<String, ParseNodeFactory> contentTypeAssociatedFactories = new HashMap<>();
    public String getValidContentType() {
        throw new UnsupportedOperationException("The registry supports multiple content types. Get the registered factory instead.");
    }
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
