package com.microsoft.kiota;

import java.lang.reflect.Field;
import java.lang.IllegalAccessException;
import java.util.Arrays;
import java.util.Map;
import java.util.Objects;
import javax.annotation.Nonnull;

public abstract class QueryParametersBase {
    public void AddQueryParameters(@Nonnull final Map<String, Object> target) {
        Objects.requireNonNull(target);
        final Field[] fields = this.getClass().getFields();
        for(final Field field : fields) {
            try {
                final var value = field.get(this);
                var name = field.getName();
                if (field.isAnnotationPresent(QueryParameter.class)) {
                    final var annotationName = field.getAnnotation(QueryParameter.class).name();
                    if(annotationName != null && !annotationName.isEmpty()) {
                        name = annotationName;
                    }
                }
                if(value != null) {
                    if(value.getClass().isArray()) {
                        target.put(name, Arrays.asList((Object[])value));
                    } else {
                        target.put(name, value);
                    }
                }
            } catch (IllegalAccessException ex) {
                //TODO log
            }
        }
    }
}