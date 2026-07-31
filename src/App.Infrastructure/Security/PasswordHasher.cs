using App.Application.Abstractions.Security;
using Microsoft.AspNetCore.Identity;

namespace App.Infrastructure.Security;

public sealed class PasswordHasher : IPasswordHasher
{
    // TUser is never inspected by PasswordHasher<TUser> — it only affects the generic
    // signature, so a neutral placeholder type avoids coupling this to App.Domain.User.
    private static readonly PasswordHasher<object> InnerHasher = new();
    private static readonly object HashingContext = new();

    public string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        return InnerHasher.HashPassword(HashingContext, password);
    }

    public bool Verify(string password, string passwordHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);

        var result = InnerHasher.VerifyHashedPassword(HashingContext, passwordHash, password);
        return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }
}
