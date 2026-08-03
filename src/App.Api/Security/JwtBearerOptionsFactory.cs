using System.Text;
using App.Infrastructure.Security;
using Microsoft.IdentityModel.Tokens;

namespace App.Api.Security;

public static class JwtBearerOptionsFactory
{
    public static TokenValidationParameters BuildTokenValidationParameters(JwtOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = options.Issuer,
            ValidateAudience = true,
            ValidAudience = options.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Secret)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    }
}
