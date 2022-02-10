package com.microsoft.kiota.http.middleware.options;

import java.util.Objects;
import java.util.UUID;
import java.lang.reflect.Field;

import javax.annotation.Nonnull;

public class TelemetryUtilities {
    
    /**
     * The Client Request ID for use
     */
    private String clientRequestId;
    /**
     * Http request header to send the telemetry infromation with
     */
    public static final String SDK_VERSION = "SdkVersion";
    /**
     * Current SDK version
     */
    public static final String VERSION = ""; //Set This Upon Release
    /**
     * Verion prefix
     */
    public static final String GRAPH_VERSION_PREFIX = "graph-java-core";
    /**
     * Java version prefix
     */
    public static final String JAVA_VERSION_PREFIX = "java";
    /**
     * Android version prefix
     */
    public static final String ANDROID_VERSION_PREFIX = "android";
    /**
     * The client request ID header
     */
    public static final String CLIENT_REQUEST_ID = "client-request-id";
    private static final String DEFAULT_VERSION_VALUE = "0";

    private String androidAPILevel;

    /**
     * Sets the client request id
     * @param clientRequestId the client request id to set, preferably the string representation of a GUID
     */
    public void setClientRequestId(@Nonnull final String clientRequestId) {
        this.clientRequestId = Objects.requireNonNull(clientRequestId, "parameter clientRequestId cannot be null");
    }
    /**
     * Gets the client request id
     * @return the client request id
     */
    @Nonnull
    public String getClientRequestId() {
        if(clientRequestId == null) {
            clientRequestId = UUID.randomUUID().toString();
        }
        return clientRequestId;
    }
    
    public String getAndroidAPILevel() {
        if(androidAPILevel == null) {
            androidAPILevel = getAndroidAPILevelInternal();
        }
        return androidAPILevel;
    }
    private String getAndroidAPILevelInternal() {
        try {
            final Class<?> buildClass = Class.forName("android.os.Build");
            final Class<?>[] subclasses = buildClass.getDeclaredClasses();
            Class<?> versionClass = null;
            for(final Class<?> subclass : subclasses) {
                if(subclass.getName().endsWith("VERSION")) {
                    versionClass = subclass;
                    break;
                }
            }
            if(versionClass == null)
                return DEFAULT_VERSION_VALUE;
            else {
                final Field sdkVersionField = versionClass.getField("SDK_INT");
                final Object value = sdkVersionField.get(null);
                final String valueStr = String.valueOf(value);
                return valueStr == null || valueStr.equals("") ? DEFAULT_VERSION_VALUE : valueStr;
            }
        } catch (IllegalAccessException | ClassNotFoundException | NoSuchFieldException ex) {
            // we're not on android and return "0" to align with java version which returns "0" when running on android
            return DEFAULT_VERSION_VALUE;
        }
    }



}
