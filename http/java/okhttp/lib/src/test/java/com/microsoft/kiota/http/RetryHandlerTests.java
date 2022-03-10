package com.microsoft.kiota.http;

import static org.junit.jupiter.api.Assertions.assertEquals;

import com.microsoft.kiota.http.middleware.RetryHandler;
import com.microsoft.kiota.http.middleware.options.RetryHandlerOption;

import org.junit.jupiter.api.Test;

public class RetryHandlerTests {
   
    @Test 
    public void RetryHandlerConstructorDefaults() {
        RetryHandler retryHandler = new RetryHandler();
        RetryHandlerOption retryHandlerOption = new RetryHandlerOption();
        
        assertEquals(retryHandler.getRetryOptions().delay(), retryHandlerOption.delay());
        assertEquals(retryHandler.getRetryOptions().maxRetries(), retryHandlerOption.maxRetries());
        //assertEquals(retryHandler.getRetryOptions()., actual);
        
    }



}
