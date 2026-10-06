using SwaApimPoc.Application.Abstractions;
using SwaApimPoc.Application.Common;
using SwaApimPoc.Application.Identity;

namespace SwaApimPoc.Application.Graph;

/// <summary>
/// Calls Microsoft Graph with the forwarded user token. This is the pattern the real app's
/// mail and calendar services would use instead of OBO.
/// </summary>
public sealed class GraphProbeService(
    ICurrentUser currentUser,
    IGraphTokenAccessor tokenAccessor,
    IGraphProfileClient graphClient)
{
    public async Task<GraphProfile> GetMyProfileAsync(CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
        {
            throw new UnauthenticatedException();
        }

        string token = tokenAccessor.GetForwardedToken() ?? throw new GraphTokenMissingException();
        GraphProfile profile = await graphClient.GetMeAsync(token, cancellationToken);

        IdentityMatch.EnsureSameUser(currentUser, profile);
        return profile;
    }
}
