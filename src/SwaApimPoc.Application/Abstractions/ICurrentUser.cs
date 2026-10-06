namespace SwaApimPoc.Application.Abstractions;

/// <summary>
/// The signed-in user as described by the x-ms-client-principal header.
/// </summary>
public interface ICurrentUser
{
    bool IsAuthenticated { get; }

    string? SwaUserId { get; }

    string? UserDetails { get; }

    string? IdentityProvider { get; }

    IReadOnlyCollection<string> Roles { get; }
}
