using App.Infrastructure.Security;

namespace App.Infrastructure.Tests.Security;

public class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void Hash_DoesNotReturnThePlainTextPassword()
    {
        var hash = _hasher.Hash("Correct-Password-123!");

        Assert.NotEqual("Correct-Password-123!", hash);
    }

    [Fact]
    public void Verify_WithCorrectPassword_ReturnsTrue()
    {
        var hash = _hasher.Hash("Correct-Password-123!");

        var result = _hasher.Verify("Correct-Password-123!", hash);

        Assert.True(result);
    }

    [Fact]
    public void Verify_WithIncorrectPassword_ReturnsFalse()
    {
        var hash = _hasher.Hash("Correct-Password-123!");

        var result = _hasher.Verify("Wrong-Password-123!", hash);

        Assert.False(result);
    }

    [Fact]
    public void Hash_SamePasswordTwice_ProducesDifferentHashes()
    {
        var first = _hasher.Hash("Correct-Password-123!");
        var second = _hasher.Hash("Correct-Password-123!");

        Assert.NotEqual(first, second);
    }
}
