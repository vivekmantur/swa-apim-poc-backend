namespace SwaApimPoc.Application.Graph;

public sealed record GraphProfile(
    string Id,
    string? DisplayName,
    string? UserPrincipalName,
    string? Mail);
