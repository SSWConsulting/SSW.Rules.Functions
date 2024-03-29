using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OidcApiAuthorization.Abstractions;
using OidcApiAuthorization.Models;

namespace OidcApiAuthorization;

 /// <summary>
    /// Encapsulates checks of OpenID Connect (OIDC) Authorization tokens in HTTP request headers.
    /// </summary>
    public class OidcApiAuthorizationService : IApiAuthorization
    {
        private readonly IAuthorizationHeaderBearerTokenExtractor _authorizationHeaderBearerTokenExractor;

        private readonly IJwtSecurityTokenHandlerWrapper _jwtSecurityTokenHandlerWrapper;

        private readonly IOidcConfigurationManager _oidcConfigurationManager;

        private readonly string _issuerUrl = null;
        private readonly string _audience = null;

        public OidcApiAuthorizationService(
            IOptions<OidcApiAuthorizationSettings> apiAuthorizationSettingsOptions,
            IAuthorizationHeaderBearerTokenExtractor authorizationHeaderBearerTokenExractor,
            IJwtSecurityTokenHandlerWrapper jwtSecurityTokenHandlerWrapper,
            IOidcConfigurationManager oidcConfigurationManager)
        {
            _issuerUrl = apiAuthorizationSettingsOptions?.Value?.IssuerUrl;
            _audience = apiAuthorizationSettingsOptions?.Value?.Audience;

            _authorizationHeaderBearerTokenExractor = authorizationHeaderBearerTokenExractor;

            _jwtSecurityTokenHandlerWrapper = jwtSecurityTokenHandlerWrapper;

            _oidcConfigurationManager = oidcConfigurationManager;
        }

        /// <summary>
        /// Checks the given HTTP request headers for a valid OpenID Connect (OIDC) Authorization token.
        /// </summary>
        /// <param name="httpRequestHeaders">
        /// The HTTP request headers to check.
        /// </param>
        /// <returns>
        /// Informatoin about the success or failure of the authorization.
        /// </returns>
        public async Task<ApiAuthorizationResult> AuthorizeAsync(
            IHeaderDictionary httpRequestHeaders)
        {
            string authorizationBearerToken = _authorizationHeaderBearerTokenExractor.GetToken(
                httpRequestHeaders);
            if (authorizationBearerToken == null)
            {
                return new ApiAuthorizationResult(
                    "Authorization header is missing, invalid format, or is not a Bearer token.");
            }

            bool isTokenValid = false;

            int validationRetryCount = 0;

            do
            {
                IEnumerable<SecurityKey> issuerSigningKeys;
                try
                {
                    // Get the cached signing keys if they were retrieved previously.
                    // If they haven't been retrieved, or the cached keys are stale,
                    // then a fresh set of signing keys are retrieved from the OpenID Connect provider
                    // (issuer) cached and returned.
                    // This method will throw if the configuration cannot be retrieved, instead of returning null.
                    issuerSigningKeys = await _oidcConfigurationManager.GetIssuerSigningKeysAsync();
                }
                catch (Exception ex)
                {
                    return new ApiAuthorizationResult(
                        "Problem getting signing keys from Open ID Connect provider (issuer)."
                        + $" ConfigurationManager threw {ex.GetType()} Message: {ex.Message}");
                }

                try
                {
                    // Try to validate the token.

                    var tokenValidationParameters = new TokenValidationParameters
                    {
                        RequireSignedTokens = true,
                        ValidAudience = _audience,
                        ValidateAudience = true,
                        ValidIssuer = _issuerUrl,
                        ValidateIssuer = true,
                        ValidateIssuerSigningKey = true,
                        ValidateLifetime = true,
                        IssuerSigningKeys = issuerSigningKeys
                    };

                    try
                    {
                        // Throws if the the token cannot be validated.
                        _jwtSecurityTokenHandlerWrapper.ValidateToken(
                            authorizationBearerToken,
                            tokenValidationParameters);

                        isTokenValid = true;
                    }
                    catch (SecurityTokenSignatureKeyNotFoundException)
                    {
                        // A SecurityTokenSignatureKeyNotFoundException is thrown if the signing keys for
                        // validating the JWT could not be found. This could happen if the issuer has
                        // changed the signing keys since the last time they were retrieved by the
                        // ConfigurationManager.
                        if (validationRetryCount == 0)
                        {
                            // To handle the SecurityTokenSignatureKeyNotFoundException we ask the
                            // ConfigurationManger to refresh which will cause it to retrieve the keys again
                            // the next time we ask for them.
                            // Then we retry by asking for the signing keys and validating the token again.
                            // We only retry once.
                            _oidcConfigurationManager.RequestRefresh();
                            validationRetryCount++;
                        }
                        else
                        {
                            // We've already re-tried after the first SecurityTokenSignatureKeyNotFoundException,
                            // and we caught the exception again.
                            // This time we rethrow the exception so that we will fail the authorization.
                            throw;
                        }
                    }
                }
                catch (Exception ex)
                {
                    return new ApiAuthorizationResult(
                        $"Authorization Failed. {ex.GetType()} caught while validating JWT token."
                        + $"Message: {ex.Message}");
                }

            } while (!isTokenValid);

            // Success result.
            return new ApiAuthorizationResult();
        }

        public async Task<HealthCheckResult> HealthCheckAsync()
        {
            if (string.IsNullOrWhiteSpace(_audience)
                || string.IsNullOrWhiteSpace(_issuerUrl))
            {
                return new HealthCheckResult(
                    $"Some or all {nameof(OidcApiAuthorizationSettings)} are missing.");
            }

            try
            {
                // Get the singing keys fresh. Not from the cache.
                _oidcConfigurationManager.RequestRefresh();

                await _oidcConfigurationManager.GetIssuerSigningKeysAsync();
            }
            catch (Exception ex)
            {
                return new HealthCheckResult(
                    "Problem getting signing keys from Open ID Connect provider (issuer)."
                    + $" ConfigurationManager threw {ex.GetType()} Message: {ex.Message}");
            }

            return new HealthCheckResult(); // Good health.
        }
    }