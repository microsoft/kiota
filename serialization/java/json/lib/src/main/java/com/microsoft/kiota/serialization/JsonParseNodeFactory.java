package com.microsoft.kiota.serialization;

import com.microsoft.kiota.serialization.ParseNode;
import com.microsoft.kiota.serialization.ParseNodeFactory;

import java.io.BufferedReader;
import java.io.IOException;
import java.io.InputStream;
import java.io.InputStreamReader;
import java.nio.charset.StandardCharsets;
import java.util.Objects;
import java.util.stream.Collectors;

import javax.annotation.Nonnull;

public class JsonParseNodeFactory implements ParseNodeFactory {
    public String getValidContentType() {
        return validContentType;
    }
    private final static String validContentType = "application/json";
    @Override
    @Nonnull
    public ParseNode getParseNode(@Nonnull final String contentType, @Nonnull final InputStream rawResponse) {
        Objects.requireNonNull(contentType, "parameter contentType cannot be null");
        Objects.requireNonNull(rawResponse, "parameter rawResponse cannot be null");
        if(contentType.isEmpty()) {
            throw new NullPointerException("contentType cannot be empty");
        } else if (!contentType.equals(validContentType)) {
            throw new IllegalArgumentException("expected a " + validContentType + " content type");
        }
        String rawText;
        try(final InputStreamReader reader = new InputStreamReader(rawResponse, StandardCharsets.UTF_8)) {
            try(final BufferedReader buff = new BufferedReader(reader)) {
                rawText = buff.lines()
                .collect(Collectors.joining("\n"));
            }
        } catch (IOException ex) {
            throw new RuntimeException("could not close the reader", ex);
        }
        return new JsonParseNode(rawText);
    }

}