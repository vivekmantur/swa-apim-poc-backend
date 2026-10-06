using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using SwaApimPoc.Api.Authentication;
using SwaApimPoc.Api.Services;

namespace SwaApimPoc.Api.Controllers;

/// <summary>
/// POC only. Shows what actually reaches the API through SWA and APIM so the team can answer
/// "what token does SWA send?" and "does the backend get the oid?" from evidence.
/// Never returns token or key values, only names and non-secret claims.
/// </summary>
[ApiController]
[Route("api/diagnostics")]
public sealed class DiagnosticsController : ControllerBase
{
    private static readonly string[] SafeTokenClaims =
        ["iss", "aud", "appid", "azp", "sub", "oid", "tid", "idtyp", "roles", "scp", "iat", "exp"];

    [HttpGet("request")]
    public IActionResult GetRequest()
    {
        SwaClientPrincipal.TryDecode(
            Request.Headers[SwaClientPrincipal.HeaderName].FirstOrDefault(),
            out SwaClientPrincipal? principal,
            out string? rawPrincipalJson);

        return Ok(new
        {
            clientPrincipal = rawPrincipalJson is null ? (JsonElement?)null : JsonDocument.Parse(rawPrincipalJson).RootElement,
            clientPrincipalHasClaims = principal?.Claims is { Count: > 0 },
            authorizationHeader = DescribeBearer(Request.Headers.Authorization.FirstOrDefault()),
            graphTokenHeaderPresent = Request.Headers.ContainsKey(HeaderGraphTokenAccessor.HeaderName),
            subscriptionKeyHeaderPresent = Request.Headers.ContainsKey("Ocp-Apim-Subscription-Key"),
            headerNames = Request.Headers.Keys.Order(StringComparer.OrdinalIgnoreCase).ToArray(),
        });
    }

    private static object DescribeBearer(string? authorization)
    {
        if (string.IsNullOrWhiteSpace(authorization)
            || !authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return new { present = !string.IsNullOrWhiteSpace(authorization), isJwt = false };
        }

        var handler = new JsonWebTokenHandler();
        string token = authorization["Bearer ".Length..].Trim();
        if (!handler.CanReadToken(token))
        {
            return new { present = true, isJwt = false };
        }

        // Decoded for inspection only; APIM's validate-jwt policy is what verified it.
        JsonWebToken jwt = handler.ReadJsonWebToken(token);
        Dictionary<string, string> claims = jwt.Claims
            .Where(claim => SafeTokenClaims.Contains(claim.Type))
            .GroupBy(claim => claim.Type)
            .ToDictionary(group => group.Key, group => string.Join(" ", group.Select(claim => claim.Value)));

        return new { present = true, isJwt = true, claims };
    }
}
