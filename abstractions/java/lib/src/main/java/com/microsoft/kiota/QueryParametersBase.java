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
                var value = field.get(this);
                if(value != null) {
                    if(value.getClass().isArray()) {
                        target.put(field.getName(), Arrays.asList((Object[])value));
                    } else {
                        target.put(field.getName(), value);
                    }
                }
            } catch (IllegalAccessException ex) {
                //TODO log
            }
        }
    }
}