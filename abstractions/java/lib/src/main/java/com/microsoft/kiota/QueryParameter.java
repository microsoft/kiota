package com.microsoft.kiota;

import java.lang.annotation.ElementType;
import java.lang.annotation.Retention;
import java.lang.annotation.RetentionPolicy;
import java.lang.annotation.Target;

/*
 * This annotation allows mapping between the query parameter name in the template and the property name in the class.
 */
@Retention(RetentionPolicy.RUNTIME)
@Target(ElementType.FIELD)
public @interface QueryParameter {
    /** The name of the parameter in the template */
    public String name();
}