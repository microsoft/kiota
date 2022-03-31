package com.microsoft.kiota;

/** Represents a request option. */
public interface RequestOption {

    public <T extends RequestOption> Class<T> getType();
}