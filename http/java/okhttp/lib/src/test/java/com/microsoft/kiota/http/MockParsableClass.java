package com.microsoft.kiota.http;

import java.util.Map;
import java.util.function.BiConsumer;

import com.microsoft.kiota.serialization.Parsable;
import com.microsoft.kiota.serialization.ParseNode;
import com.microsoft.kiota.serialization.SerializationWriter;

public class MockParsableClass implements Parsable{

    @Override
    public Map<String, Object> getAdditionalData() {
        //Map<String, Object> mockMap = Map.of("One", 1, "Two", 2, "Three", 3);
        return null;
    }

    @Override
    public <T> Map<String, BiConsumer<T, ParseNode>> getFieldDeserializers() {
        return null;
    }

    @Override
    public void serialize(SerializationWriter arg0) {        
    }
    
    
}
