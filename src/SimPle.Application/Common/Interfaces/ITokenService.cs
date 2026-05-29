namespace SimPle.Application.Common.Interfaces;

public interface ITokenService
{
    // familyId is included as the 'sid' claim so a session can be immediately invalidated without a DB roundtrip.
    (string Token, DateTime ExpiresAt, string Jti) GenerateAccessToken(Guid userId, string username, string role, Guid familyId);
    string GenerateRawRefreshToken();
    string HashToken(string rawToken);
    Guid? ValidateAccessToken(string token);
}
