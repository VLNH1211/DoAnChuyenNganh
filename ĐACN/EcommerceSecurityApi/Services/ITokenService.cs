using EcommerceSecurityApi.Entities;

namespace EcommerceSecurityApi.Services;

public interface ITokenService
{
    string GenerateJwtToken(User user, IList<string> roles, out DateTime expiresAt);
}
