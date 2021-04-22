package com.microsoft.kiota.core.serialization;

import com.google.gson.JsonElement;
import com.google.gson.JsonObject;
import com.google.common.collect.Lists;
import com.google.gson.JsonArray;
import com.google.gson.JsonParser;
import com.google.gson.JsonPrimitive;

import java.lang.reflect.Constructor;
import java.lang.reflect.InvocationTargetException;
import java.time.OffsetDateTime;
import java.util.EnumSet;
import java.util.Iterator;
import java.util.List;
import java.util.Map;
import java.util.Objects;
import java.util.UUID;
import java.util.function.BiConsumer;

import com.microsoft.kiota.serialization.ParseNode;

import com.microsoft.kiota.serialization.Parsable;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;

public class JsonParseNode implements ParseNode {
    private final JsonElement currentNode;
    public JsonParseNode(@Nonnull final String rawJson) {
        Objects.requireNonNull(rawJson, "parameter node cannot be null");
        currentNode = JsonParser.parseString(rawJson);
    }
    public JsonParseNode(@Nonnull final JsonElement node) {
        currentNode = Objects.requireNonNull(node, "parameter node cannot be null");
    }
    public ParseNode getChildNode(final String identifier) {
        Objects.requireNonNull(identifier, "identifier parameter is required");
        if(currentNode.isJsonObject()) {
            final JsonObject object = currentNode.getAsJsonObject();
            return new JsonParseNode(object.get(identifier));
        } else throw new RuntimeException("invalid state expected to have an object node");
    }
    public String getStringValue() {
        return currentNode.getAsString();
    }
    public Boolean getBooleanValue() {
        return currentNode.getAsBoolean();
    }
    public Integer getIntegerValue() {
        return currentNode.getAsInt();
    }
    public Float getFloatValue() {
        return currentNode.getAsFloat();
    }
    public Long getLongValue() {
        return currentNode.getAsLong();
    }
    public UUID getUUIDValue() {
        return UUID.fromString(currentNode.getAsString());
    }
    public OffsetDateTime getOffsetDateTimeValue() {
        return OffsetDateTime.parse(currentNode.getAsString());
    }
    public <T> List<T> getCollectionOfPrimitiveValues(final Class<T> targetClass) {
        Objects.requireNonNull(targetClass, "parameter targetClass cannot be null");
        if(currentNode.isJsonArray()) {
            final JsonArray array = currentNode.getAsJsonArray();
            final Iterator<JsonElement> sourceIterator = array.iterator();
            return Lists.newArrayList(new Iterable<T>() {
                @Override
                public Iterator<T> iterator() {
                    return new Iterator<T>(){
                        @Override
                        public boolean hasNext() {
                            return sourceIterator.hasNext();
                        }
                        @Override
                        @SuppressWarnings("unchecked")
                        public T next() {
                            final JsonElement item = sourceIterator.next();
                            final JsonParseNode itemNode = new JsonParseNode(item);
                            if(targetClass == Boolean.class) {
                                return (T)itemNode.getBooleanValue();
                            } else if(targetClass == String.class) {
                                return (T)itemNode.getStringValue();
                            } else if(targetClass == Integer.class) {
                                return (T)itemNode.getIntegerValue();
                            } else if(targetClass == Float.class) {
                                return (T)itemNode.getFloatValue();
                            } else if(targetClass == Long.class) {
                                return (T)itemNode.getLongValue();
                            } else if(targetClass == UUID.class) {
                                return (T)itemNode.getUUIDValue();
                            } else if(targetClass == OffsetDateTime.class) {
                                return (T)itemNode.getOffsetDateTimeValue();
                            } else {
                                throw new RuntimeException("unknown type to deserialize " + targetClass.getName());
                            }
                        }
                    };
                }
            });
        } else throw new RuntimeException("invalid state expected to have an array node");
    }
    public <T extends Parsable> List<T> getCollectionOfObjectValues(final Class<T> targetClass) {
        Objects.requireNonNull(targetClass, "parameter targetClass cannot be null");
        if(currentNode.isJsonArray()) {
            final JsonArray array = currentNode.getAsJsonArray();
            final Iterator<JsonElement> sourceIterator = array.iterator();
            return Lists.newArrayList(new Iterable<T>() {
                @Override
                public Iterator<T> iterator() {
                    return new Iterator<T>(){
                        @Override
                        public boolean hasNext() {
                            return sourceIterator.hasNext();
                        }
                        @Override
                        public T next() {
                            final JsonElement item = sourceIterator.next();
                            final JsonParseNode itemNode = new JsonParseNode(item);
                            return itemNode.getObjectValue(targetClass);
                        }
                    };
                }

            });
        } else throw new RuntimeException("invalid state expected to have an array node");
    }
    public <T extends Parsable> T getObjectValue(final Class<T> targetClass) {
        Objects.requireNonNull(targetClass, "parameter targetClass cannot be null");
        try {
            final Constructor<T> constructor = targetClass.getConstructor();
            final T item = constructor.newInstance();
            assignFieldValues(item, item.getDeserializeFields());
            //TODO additional properties when the strucutre is available
            return item;
        } catch (NoSuchMethodException | InstantiationException | IllegalAccessException | InvocationTargetException ex) {
            throw new RuntimeException("Error during deserialization", ex);
        }
    }
    @Nullable
    public <T extends Enum<T>> T getEnumValue(@Nonnull final Class<T> targetEnum) {
        final String rawValue = this.getStringValue();
        if(rawValue == null || rawValue.isEmpty()) {
            return null;
        }
        return getEnumValueInt(rawValue, targetEnum);
    }
    @SuppressWarnings("unchecked")
    private <T extends Enum<T>> T getEnumValueInt(@Nonnull final String rawValue, @Nonnull final Class<T> targetEnum) {
        try {
            return (T)targetEnum.getMethod("forValue", String.class).invoke(null, rawValue);
        } catch (Exception ex) {
            return null;
        }
    }
    @Nullable
    public <T extends Enum<T>> EnumSet<T> getEnumSetValue(@Nonnull final Class<T> targetEnum) {
        final String rawValue = this.getStringValue();
        if(rawValue == null || rawValue.isEmpty()) {
            return null;
        }
        final EnumSet<T> result = EnumSet.noneOf(targetEnum);
        final String[] rawValues = rawValue.split(",");
        for (final String rawValueItem : rawValues) {
            final T value = getEnumValueInt(rawValueItem, targetEnum);
            if(value != null) {
                result.add(value);
            }
        }
        return result;
    }
    private <T extends Parsable> void assignFieldValues(final T item, final Map<String, BiConsumer<T, ParseNode>> fieldDeserializers) {
        if(currentNode.isJsonObject()) {
            for (final Map.Entry<String, JsonElement> fieldEntry : currentNode.getAsJsonObject().entrySet()) {
                final String fieldKey = fieldEntry.getKey();
                final BiConsumer<? super T, ParseNode> fieldDeserializer = fieldDeserializers.get(fieldKey);
                final JsonElement fieldValue = fieldEntry.getValue();
                if(fieldDeserializer != null && !fieldValue.isJsonNull())
                    fieldDeserializer.accept(item, new JsonParseNode(fieldValue));
                else
                    item.getAdditionalData().put(fieldKey, this.tryGetAnything(fieldValue));
            }
        }
    }
    private Object tryGetAnything(final JsonElement element) {
        if(element.isJsonPrimitive()) {
            final JsonPrimitive primitive = element.getAsJsonPrimitive();
            if(primitive.isBoolean())
                return primitive.getAsBoolean();
            else if (primitive.isString())
                return primitive.getAsString();
            else if (primitive.isNumber())
                return primitive.getAsFloat();
            else
                throw new RuntimeException("Could not get the value during deserialization, unknown primitive type");
        } else if(element.isJsonNull())
            return null;
        else if (element.isJsonObject() || element.isJsonArray())
            return element;
        else
            throw new RuntimeException("Could not get the value during deserialization, unknown primitive type");
    }
}
