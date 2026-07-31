using System.IdentityModel.Tokens.Jwt;
using App.Api.Security;
using App.Infrastructure.Security;
using Microsoft.IdentityModel.Tokens;

namespace App.Api.Tests.Security;

public class JwtBearerOptionsFactoryTests
{
    private static JwtOptions CreateOptions(string? secretOverride = null, string? issuerOverride = null, string? audienceOverride = null) => new()
    {
        Secret = secretOverride ?? "test-only-signing-key-at-least-32-characters-long",
        Issuer = issuerOverride ?? "erp-delivery-prediction-tests",
        Audience = audienceOverride ?? "erp-delivery-prediction-tests-clients",
        ExpirationMinutes = 30
    };

    private static string GenerateToken(JwtOptions options, int expirationMinutes = 30)
    {
        var generator = new JwtAccessTokenGenerator(new JwtOptions
        {
            Secret = options.Secret,
            Issuer = options.Issuer,
            Audience = options.Audience,
            ExpirationMinutes = expirationMinutes
        });
        return generator.GenerateToken(1, "eren", ["Admin"]);
    }

    private static string GenerateAlreadyExpiredToken(JwtOptions options)
    {
        var signingKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(options.Secret));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var expired = DateTime.UtcNow.AddMinutes(-10);

        var token = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            notBefore: expired.AddMinutes(-1),
            expires: expired,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    [Fact]
    public void ValidToken_IsAccepted()
    {
        var options = CreateOptions();
        var token = GenerateToken(options);
        var parameters = JwtBearerOptionsFactory.BuildTokenValidationParameters(options);

        var principal = new JwtSecurityTokenHandler().ValidateToken(token, parameters, out _);

        Assert.NotNull(principal);
    }

    [Fact]
    public void InvalidSigningKey_IsRejected()
    {
        var generatingOptions = CreateOptions();
        var token = GenerateToken(generatingOptions);

        var validatingOptions = CreateOptions(secretOverride: "a-completely-different-signing-key-32-chars-min");
        var parameters = JwtBearerOptionsFactory.BuildTokenValidationParameters(validatingOptions);

        Assert.Throws<SecurityTokenSignatureKeyNotFoundException>(
            () => new JwtSecurityTokenHandler().ValidateToken(token, parameters, out _));
    }

    [Fact]
    public void ExpiredToken_IsRejected()
    {
        var options = CreateOptions();
        var token = GenerateAlreadyExpiredToken(options);
        var parameters = JwtBearerOptionsFactory.BuildTokenValidationParameters(options);

        Assert.Throws<SecurityTokenExpiredException>(
            () => new JwtSecurityTokenHandler().ValidateToken(token, parameters, out _));
    }

    [Fact]
    public void WrongIssuer_IsRejected()
    {
        var generatingOptions = CreateOptions();
        var token = GenerateToken(generatingOptions);

        var validatingOptions = CreateOptions(issuerOverride: "someone-else");
        var parameters = JwtBearerOptionsFactory.BuildTokenValidationParameters(validatingOptions);

        Assert.Throws<SecurityTokenInvalidIssuerException>(
            () => new JwtSecurityTokenHandler().ValidateToken(token, parameters, out _));
    }

    [Fact]
    public void WrongAudience_IsRejected()
    {
        var generatingOptions = CreateOptions();
        var token = GenerateToken(generatingOptions);

        var validatingOptions = CreateOptions(audienceOverride: "someone-elses-clients");
        var parameters = JwtBearerOptionsFactory.BuildTokenValidationParameters(validatingOptions);

        Assert.Throws<SecurityTokenInvalidAudienceException>(
            () => new JwtSecurityTokenHandler().ValidateToken(token, parameters, out _));
    }
}
