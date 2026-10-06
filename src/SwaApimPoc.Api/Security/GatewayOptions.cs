namespace SwaApimPoc.Api.Security;

public sealed class GatewayOptions
{
    public const string SectionName = "Gateway";

    public const string HeaderName = "X-Gateway-Secret";

    /// <summary>
    /// Shared secret that the APIM policy adds to every request. Empty disables the check
    /// (local development only).
    /// </summary>
    public string? Secret { get; init; }
}
