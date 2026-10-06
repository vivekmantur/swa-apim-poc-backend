using SwaApimPoc.Application.Abstractions;

namespace SwaApimPoc.Api.Services;

public sealed class HeaderGraphTokenAccessor(IHttpContextAccessor httpContextAccessor) : IGraphTokenAccessor
{
    public const string HeaderName = "X-Graph-Token";

    public string? GetForwardedToken()
    {
        string? token = httpContextAccessor.HttpContext?.Request.Headers[HeaderName].FirstOrDefault();
        return string.IsNullOrWhiteSpace(token) ? null : token.Trim();
    }
}
