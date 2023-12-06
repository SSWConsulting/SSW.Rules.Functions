using Microsoft.IdentityModel.Tokens;

namespace OidcApiAuthorization.Abstractions
{
    public interface IJwtSecurityTokenHandlerWrapper
    {
        void ValidateToken(string token, TokenValidationParameters tokenValidationParameters);
    }
}
