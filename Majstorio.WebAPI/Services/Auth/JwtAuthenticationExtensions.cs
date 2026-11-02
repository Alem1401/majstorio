using System.Globalization;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Majstorio.WebAPI.Services.Auth;

public static class JwtAuthenticationExtensions
{
    // Registers token creation and validation from the Jwt__* configuration in one place.
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations();
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<ITokenService, JwtTokenService>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, jwtOptions) =>
            {
                var jwt = jwtOptions.Value;

                // Keep the claim names exactly as JwtTokenService writes them.
                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                    NameClaimType = JwtClaimNames.UserName,
                    RoleClaimType = JwtClaimNames.Role
                };
                bearer.Events = new JwtBearerEvents { OnTokenValidated = RejectOutdatedTokenAsync };
            });

        services.AddAuthorization();
        return services;
    }

    // A signed, unexpired token is still refused after logout or deactivation.
    private static async Task RejectOutdatedTokenAsync(TokenValidatedContext context)
    {
        var principal = context.Principal;
        if (!TryReadInt(principal, JwtClaimNames.UserId, out var userId)
            || !TryReadInt(principal, JwtClaimNames.TokenVersion, out var tokenVersion))
        {
            context.Fail("The token is missing required claims.");
            return;
        }

        var authService = context.HttpContext.RequestServices.GetRequiredService<IAuthService>();
        if (!await authService.IsTokenVersionCurrentAsync(userId, tokenVersion))
            context.Fail("The token is no longer valid.");
    }

    private static bool TryReadInt(ClaimsPrincipal? principal, string claimName, out int value) =>
        int.TryParse(principal?.FindFirstValue(claimName), NumberStyles.None, CultureInfo.InvariantCulture, out value);
}
