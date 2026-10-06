using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace SwaApimPoc.Api.Authentication;

/// <summary>
/// Builds HttpContext.User from x-ms-client-principal. The header is only trustworthy because
/// GatewaySecretMiddleware has already confirmed the request came through APIM, and APIM's
/// validate-jwt policy confirmed it came from the linked Static Web App.
/// </summary>
public sealed class SwaAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "StaticWebApps";

    public const string IdentityProviderClaim = "swa_idp";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        string? header = Request.Headers[SwaClientPrincipal.HeaderName].FirstOrDefault();
        if (!SwaClientPrincipal.TryDecode(header, out SwaClientPrincipal? principal, out _)
            || string.IsNullOrWhiteSpace(principal!.UserId))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, principal.UserId),
            new(ClaimTypes.Name, principal.UserDetails ?? string.Empty),
            new(IdentityProviderClaim, principal.IdentityProvider ?? string.Empty),
        };

        claims.AddRange(principal.UserRoles
            .Where(role => !string.Equals(role, "anonymous", StringComparison.OrdinalIgnoreCase))
            .Select(role => new Claim(ClaimTypes.Role, role)));

        var identity = new ClaimsIdentity(claims, SchemeName);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
