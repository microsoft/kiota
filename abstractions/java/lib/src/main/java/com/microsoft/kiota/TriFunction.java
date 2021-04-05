package com.microsoft.kiota;

public interface TriFunction<T, U, V, R> {
    R apply(T t, U u, V v);
}
