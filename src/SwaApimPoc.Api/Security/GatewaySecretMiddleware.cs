using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace SwaApimPoc.Api.Security;

/// <summary>
/// Rejects any /api request that did not come through APIM. Without this, anyone who can reach
/// the App Service URL could send a forged x-ms-client-principal header.
/// </summary>
public sealed class GatewaySecretMiddleware(RequestDelegate next, IOptions<GatewayOptions> options)
{
    private readonly byte[]? expected = string.IsNullOrEmpty(options.Value.Secret)
        ? null
        : Encoding.UTF8.GetBytes(options.Value.Secret);

    public async Task InvokeAsync(HttpContext context)
    {
        bool isProtectedPath =
            context.Request.Path.StartsWithSegments("/api")
            && !context.Request.Path.StartsWithSegments("/api/health");

        if (expected is not null && isProtectedPath)
        {
            string? provided = context.Request.Headers[GatewayOptions.HeaderName].FirstOrDefault();
            if (provided is null
                || !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(provided), expected))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new { error = "Requests must come through API Management." });
                return;
            }
        }

        await next(context);
    }
}
