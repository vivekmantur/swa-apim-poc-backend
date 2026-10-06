using System.Security.Claims;
using SwaApimPoc.Api.Authentication;
using SwaApimPoc.Application.Abstractions;

namespace SwaApimPoc.Api.Services;

public sealed class HttpCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;

    public string? SwaUserId => User?.FindFirstValue(ClaimTypes.NameIdentifier);

    public string? UserDetails => User?.FindFirstValue(ClaimTypes.Name);

    public string? IdentityProvider => User?.FindFirstValue(SwaAuthenticationHandler.IdentityProviderClaim);

    public IReadOnlyCollection<string> Roles =>
        User?.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToArray() ?? [];
}
