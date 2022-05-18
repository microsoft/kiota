package com.microsoft.kiota.authentication;

import java.net.URI;
import java.util.List;
import java.util.Map;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.Base64;
import java.util.Objects;
import java.util.concurrent.CompletableFuture;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;

import com.azure.core.credential.TokenCredential;
import com.azure.core.credential.TokenRequestContext;

import com.microsoft.kiota.authentication.AccessTokenProvider;

/** Implementation of AccessTokenProvider that supports implementations of TokenCredential from Azure.Identity. */
public class AzureIdentityAccessTokenProvider implements AccessTokenProvider {
    private final TokenCredential creds;
    private final List<String> _scopes;
    private final AllowedHostsValidator _hostValidator;
    /**
     * Creates a new instance of AzureIdentityAccessTokenProvider.
     * @param tokenCredential The Azure.Identity.TokenCredential implementation to use.
     * @param allowedHosts The list of allowed hosts for which to request access tokens.
     * @param scopes The scopes to request access tokens for.
     */
    public AzureIdentityAccessTokenProvider(@Nonnull final TokenCredential tokenCredential, @Nonnull final String[] allowedHosts, @Nonnull final String... scopes) {
        creds = Objects.requireNonNull(tokenCredential, "parameter tokenCredential cannot be null");

        if(scopes == null) {
            _scopes = new ArrayList<String>();
        } else if(scopes.length == 0) {
            _scopes = Arrays.asList(new String[] { "https://graph.microsoft.com/.default" });
        } else {
            _scopes = Arrays.asList(scopes);
        }
        if (allowedHosts == null || allowedHosts.length == 0) {
            _hostValidator = new AllowedHostsValidator(new String[] { "graph.microsoft.com", "graph.microsoft.us", "dod-graph.microsoft.us", "graph.microsoft.de", "microsoftgraph.chinacloudapi.cn", "canary.graph.microsoft.com" });
        } else {
            _hostValidator = new AllowedHostsValidator(allowedHosts);
        }
    }
    private final static String ClaimsKey = "claims";
    @Nonnull
    public CompletableFuture<String> getAuthorizationToken(@Nonnull final URI uri, @Nullable final Map<String, Object> additionalAuthenticationContext) {
        if(!_hostValidator.isUrlHostValid(uri)) {
            return CompletableFuture.completedFuture("");
        }
        if(!uri.getScheme().equalsIgnoreCase("https")) {
            return CompletableFuture.failedFuture(new IllegalArgumentException("Only https is supported"));
        }

        String decodedClaim = null;

        if(additionalAuthenticationContext != null && additionalAuthenticationContext.containsKey(ClaimsKey) && additionalAuthenticationContext.get(ClaimsKey) instanceof String) {
            final String rawClaim = (String) additionalAuthenticationContext.get(ClaimsKey);
            decodedClaim = new String(Base64.getDecoder().decode(rawClaim));
        }

        final TokenRequestContext context = new TokenRequestContext() {{
            this.setScopes(_scopes);
            
        }};
        if(decodedClaim != null) {
            context.setClaims(decodedClaim);
        }
        return this.creds.getToken(context).toFuture().thenApply(r -> r.getToken());
    }
    @Nonnull
    public AllowedHostsValidator getAllowedHostsValidator() {
        return _hostValidator;
    }
}
