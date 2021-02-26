package com.microsoft.kiota;

import java.lang.reflect.Field;
import java.lang.IllegalAccessException;
import java.util.Map;
import java.util.Objects;
import javax.annotation.Nonnull;

public abstract class QueryParametersBase {
    public void AddQueryParameters(@Nonnull final Map<String, Object> target) {
        Objects.requireNonNull(target);
        final Field[] fields = this.getClass().getFields();
        for(final Field field : fields) {
            try {
                target.put(field.getName(), field.get(this));
            } catch (IllegalAccessException ex) {
                //TODO log
            }
        }
    }
}