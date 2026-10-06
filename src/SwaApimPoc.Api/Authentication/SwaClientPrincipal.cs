using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SwaApimPoc.Api.Authentication;

/// <summary>
/// Shape of the base64 JSON that Static Web Apps sends in the x-ms-client-principal header.
/// </summary>
public sealed class SwaClientPrincipal
{
    public const string HeaderName = "x-ms-client-principal";

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public string? IdentityProvider { get; init; }

    public string? UserId { get; init; }

    public string? UserDetails { get; init; }

    public List<string> UserRoles { get; init; } = [];

    // SWA documents that claims are not sent to APIs. The POC keeps the property to prove it.
    public List<SwaClaim>? Claims { get; init; }

    public static bool TryDecode(string? headerValue, out SwaClientPrincipal? principal, out string? rawJson)
    {
        principal = null;
        rawJson = null;

        if (string.IsNullOrWhiteSpace(headerValue))
        {
            return false;
        }

        try
        {
            rawJson = Encoding.UTF8.GetString(Convert.FromBase64String(headerValue));
            principal = JsonSerializer.Deserialize<SwaClientPrincipal>(rawJson, JsonOptions);
            return principal is not null;
        }
        catch (Exception exception) when (exception is FormatException or JsonException)
        {
            return false;
        }
    }
}

public sealed record SwaClaim(
    [property: JsonPropertyName("typ")] string Typ,
    [property: JsonPropertyName("val")] string Val);
