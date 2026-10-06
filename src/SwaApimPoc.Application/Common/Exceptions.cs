namespace SwaApimPoc.Application.Common;

public sealed class UnauthenticatedException()
    : Exception("The request has no signed-in Static Web Apps user.");

public sealed class GraphTokenMissingException()
    : Exception("This endpoint needs a Microsoft Graph token in the X-Graph-Token header.");

public sealed class IdentityMismatchException()
    : Exception("The Microsoft Graph token belongs to a different user than the Static Web Apps session.");

public sealed class GraphCallFailedException(int statusCode)
    : Exception($"Microsoft Graph returned status {statusCode}.")
{
    public int StatusCode { get; } = statusCode;
}
