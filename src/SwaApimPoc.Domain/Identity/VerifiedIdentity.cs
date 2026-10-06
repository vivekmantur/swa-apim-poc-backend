namespace SwaApimPoc.Domain.Identity;

/// <summary>
/// Links the Static Web Apps user to the Entra object ID that Microsoft Graph confirmed.
/// SWA's userId is per-app and is not the Entra oid, so this mapping is what lets
/// OwnerObjectId-style filtering keep working.
/// </summary>
public sealed record VerifiedIdentity(
    string SwaUserId,
    string EntraObjectId,
    string UserPrincipalName,
    string DisplayName,
    DateTimeOffset VerifiedAtUtc);
