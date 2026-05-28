namespace SimPle.Application.Common.Interfaces;

public interface ITokenService
{
    (string Token, DateTime ExpiresAt) GenerateAccessToken(Guid userId, string username, string role);
    string GenerateRawRefreshToken();
    string HashToken(string rawToken);
    Guid? ValidateAccessToken(string token);
}
