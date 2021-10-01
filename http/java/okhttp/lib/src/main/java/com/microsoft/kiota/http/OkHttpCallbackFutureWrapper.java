package com.microsoft.kiota.http;

import java.io.IOException;

import java.util.concurrent.CompletableFuture;

import okhttp3.Call;
import okhttp3.Callback;
import okhttp3.Response;

/**
 * Wraps the HTTP execution in a future, not public by intention
 */
class OkHttpCallbackFutureWrapper implements Callback {
    final CompletableFuture<Response> future = new CompletableFuture<>();
	@Override
	public void onFailure(Call arg0, IOException arg1) {
		future.completeExceptionally(arg1);
	}

	@Override
	public void onResponse(Call arg0, Response arg1) throws IOException {
		future.complete(arg1);
	}

}
