namespace SwaApimPoc.Application.Abstractions;

/// <summary>
/// Returns the Microsoft Graph token the frontend forwarded for this request, if any.
/// In the real app this replaces ITokenAcquisition (OBO).
/// </summary>
public interface IGraphTokenAccessor
{
    string? GetForwardedToken();
}
