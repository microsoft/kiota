package com.microsoft.kiota.core.serialization;

import com.google.gson.JsonElement;
import com.google.gson.JsonObject;
import com.google.gson.JsonArray;

import java.lang.reflect.Constructor;
import java.lang.reflect.InvocationTargetException;
import java.time.OffsetDateTime;
import java.util.Iterator;
import java.util.List;
import java.util.Map;
import java.util.Objects;
import java.util.UUID;
import java.util.function.BiConsumer;

import com.microsoft.kiota.serialization.ParseNode;

import com.microsoft.kiota.serialization.Parsable;

import javax.annotation.Nonnull;

public class JsonParseNode implements ParseNode {
    private final JsonElement currentNode;
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
    public UUID getGuidValue() {
        return UUID.fromString(currentNode.getAsString());
    }
    public OffsetDateTime getOffsetDateTimeValue() {
        return OffsetDateTime.parse(currentNode.getAsString());
    }
    public <T extends Parsable<T>> Iterable<T> getCollectionOfObjectValues(final Class<T> targetClass) {
        Objects.requireNonNull(targetClass, "parameter targetClass cannot be null");
        if(currentNode.isJsonArray()) {
            final JsonArray array = currentNode.getAsJsonArray();
            final Iterator<JsonElement> sourceIterator = array.iterator();
            return new Iterable<T>() {
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

            };
        } else throw new RuntimeException("invalid state expected to have an array node");
    }
    public <T extends Parsable<T>> T getObjectValue(final Class<T> targetClass) {
        Objects.requireNonNull(targetClass, "parameter targetClass cannot be null");
        try {
            final Constructor<T> constructor = targetClass.getConstructor();
            final T item = constructor.newInstance();
            assignFieldValues(item, item.getDeserializeFields());
            return item;
        } catch (NoSuchMethodException | InstantiationException | IllegalAccessException | InvocationTargetException ex) {
            throw new RuntimeException("Error during deserialization", ex);
        }
    }
    private <T extends Parsable<T>> void assignFieldValues(final T item, final Map<String, BiConsumer<T, ParseNode>> fieldDeserializers) {
        if(currentNode.isJsonObject()) {
            for (final Map.Entry<String, JsonElement> fieldEntry : currentNode.getAsJsonObject().entrySet()) {
                final BiConsumer<? super T, ParseNode> fieldDeserializer = fieldDeserializers.get(fieldEntry.getKey());
                fieldDeserializer.accept(item, new JsonParseNode(fieldEntry.getValue()));
            }
        }
    }
}
