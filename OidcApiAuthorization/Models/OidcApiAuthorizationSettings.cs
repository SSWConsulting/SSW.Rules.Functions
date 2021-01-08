namespace OidcApiAuthorization
{
    /// <summary>
    /// Encapsulates settings used in OpenID Connect (OIDC) API authorization.
    /// </summary>
    public class OidcApiAuthorizationSettings
    {
        private string _issuerUrl;
        private string _openIdConfigUrl;


        /// <summary>
        /// Identifies the API to be authorized by the Open ID Connect provider (issuer).
        /// </summary>
        /// <remarks>
        /// The "Audience" is the indentifer used by the authorization provider to indentify
        /// the API (HTTP triggered Azure Function) being protected. This is often a URL but
        /// it is not used as a URL is is simply used as an identifier.
        /// 
        /// For Auth0 the Audience setting set here should match the API's Identifier
        /// in the Auth0 Dashboard.
        /// </remarks>
        public string Audience { get; set; }

        /// <summary>
        /// The URL of the Open ID Connect provider (issuer) that will perform API authorization.
        /// </summary>
        /// <remarks>
        /// The "Issuer" is the URL for the authorization provider's end-point. This URL will be
        /// used as part of the OpenID Connect protocol to obtain the the signing keys
        /// that will be used to validate the JWT Bearer tokens in incoming HTTP request headers.
        /// 
        /// For Auth0 the URL format is:  https://{Auth0-tenant-domain}.auth0.com 
        /// </remarks>
        public string IssuerUrl
        {
            get
            {
                return _issuerUrl;
            }
            set
            {
                _issuerUrl = FormatUrl(value);
            }
        }


        /// <summary>
        /// The URL of the Open ID Connect metadata document that will perform API authorization.
        /// </summary>
        /// <remarks>
        /// This URL will be
        /// used as part of the OpenID Connect protocol to obtain the the signing keys
        /// that will be used to validate the JWT Bearer tokens in incoming HTTP request headers.
        /// 
        /// For Auth0 the URL format is:  https://{Auth0-tenant-domain}.auth0.com 
        /// </remarks>
        public string OpenIdConfigUrl
        {
            get
            {
                return _openIdConfigUrl;
            }
            set
            {
                _openIdConfigUrl = FormatUrl(value);
            }
        }

        private string FormatUrl(string url)
        {
            if (!string.IsNullOrWhiteSpace(url) && !url.EndsWith("/"))
            {
                return url + "/";
            }
            else
            {
                return url;
            }
        }
    }
}
