package com.microsoft.kiota.http.middleware;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.Mockito.mock;
import static org.mockito.Mockito.when;

import java.io.IOException;

import com.microsoft.kiota.authentication.AuthenticationProvider;
import com.microsoft.kiota.http.OkHttpRequestAdapter;

import org.junit.jupiter.params.ParameterizedTest;
import org.junit.jupiter.params.provider.CsvSource;
import org.mockito.InjectMocks;
import org.mockito.invocation.InvocationOnMock;
import org.mockito.stubbing.Answer;

import okhttp3.Protocol;
import okhttp3.Request;
import okhttp3.Response;
import okhttp3.Interceptor.Chain;

class ParametersNameDecodingHandlerTests {
  @InjectMocks
  public OkHttpRequestAdapter adapter = new OkHttpRequestAdapter(mock(AuthenticationProvider.class));
  private String resultUrl = "";

  @ParameterizedTest
  @CsvSource({"http://localhost?%24select=diplayName&api%2Dversion=2,http://localhost/?$select=diplayName&api-version=2",
  "http://localhost?%24select=diplayName&api%7Eversion=2,http://localhost/?$select=diplayName&api~version=2",
  "http://localhost?%24select=diplayName&api%2Eversion=2,http://localhost/?$select=diplayName&api.version=2",
  "http://localhost?%24select=diplayName&api%2Eversion=2,http://localhost/?$select=diplayName&api.version=2",
  "http://localhost:888?%24select=diplayName&api%2Dversion=2,http://localhost:888/?$select=diplayName&api-version=2",
  "http://localhost,http://localhost/"})
  public void testDecode(final String input, final String expected) throws IOException {
    final var handler = new ParametersNameDecodingHandler();
    final var mockChain = mock(Chain.class);
    when(mockChain.request()).thenAnswer(new Answer<Request>() {

        @Override
        public Request answer(InvocationOnMock invocation) throws Throwable {
            return new Request.Builder().url(input).build();
        }
        
    });
    when(mockChain.proceed(any(Request.class))).thenAnswer(answer -> {
      final Request resultRequest = answer.getArgument(0);
      resultUrl = resultRequest.url().toString();
      final var response = new Response.Builder().request(resultRequest).code(200).protocol(Protocol.HTTP_1_1).message("OK").build();
      return response;
    });
    handler.intercept(mockChain);
    assertEquals(expected, resultUrl);
  }
}