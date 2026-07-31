using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using App.Infrastructure.Security;
using Microsoft.IdentityModel.Tokens;

namespace App.Infrastructure.Tests.Security;

public class JwtAccessTokenGeneratorTests
{
    private static JwtOptions CreateOptions() => new()
    {
        Secret = "test-only-signing-key-at-least-32-characters-long",
        Issuer = "erp-delivery-prediction-tests",
        Audience = "erp-delivery-prediction-tests-clients",
        ExpirationMinutes = 30
    };

    [Fact]
    public void GenerateToken_ProducesAReadableJwt()
    {
        var generator = new JwtAccessTokenGenerator(CreateOptions());

        var token = generator.GenerateToken(42, "eren", ["Admin", "Planner"]);

        var handler = new JwtSecurityTokenHandler();
        Assert.True(handler.CanReadToken(token));
    }

    [Fact]
    public void GenerateToken_IncludesUserIdUsernameAndRoles()
    {
        var generator = new JwtAccessTokenGenerator(CreateOptions());

        var token = generator.GenerateToken(42, "eren", ["Admin", "Planner"]);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal("42", jwt.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.Equal("eren", jwt.Claims.Single(c => c.Type == "username").Value);
        Assert.Equal(
            ["Admin", "Planner"],
            jwt.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToArray());
    }

    [Fact]
    public void GenerateToken_IncludesIssuedAtAndExpiration()
    {
        var generator = new JwtAccessTokenGenerator(CreateOptions());

        var token = generator.GenerateToken(42, "eren", ["Admin"]);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Contains(jwt.Claims, c => c.Type == JwtRegisteredClaimNames.Iat);
        Assert.True(jwt.ValidTo > jwt.ValidFrom);
        Assert.True(jwt.ValidTo <= DateTime.UtcNow.AddMinutes(30).AddSeconds(5));
    }

    [Fact]
    public void GenerateToken_UsesHmacSha256()
    {
        var generator = new JwtAccessTokenGenerator(CreateOptions());

        var token = generator.GenerateToken(42, "eren", ["Admin"]);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal(SecurityAlgorithms.HmacSha256, jwt.Header.Alg);
    }

    [Fact]
    public void GenerateToken_SetsConfiguredIssuerAndAudience()
    {
        var options = CreateOptions();
        var generator = new JwtAccessTokenGenerator(options);

        var token = generator.GenerateToken(42, "eren", ["Admin"]);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal(options.Issuer, jwt.Issuer);
        Assert.Equal(options.Audience, jwt.Audiences.Single());
    }
}
