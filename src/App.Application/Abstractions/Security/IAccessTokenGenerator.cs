namespace App.Application.Abstractions.Security;

public interface IAccessTokenGenerator
{
    string GenerateToken(long userId, string username, IReadOnlyCollection<string> roles);
}
