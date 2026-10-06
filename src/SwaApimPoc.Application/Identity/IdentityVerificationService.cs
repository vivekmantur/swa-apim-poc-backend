using SwaApimPoc.Application.Abstractions;
using SwaApimPoc.Application.Common;
using SwaApimPoc.Application.Graph;
using SwaApimPoc.Domain.Identity;

namespace SwaApimPoc.Application.Identity;

/// <summary>
/// Gets a trusted Entra oid for the SWA user. Graph validates the forwarded token and returns
/// the user's id, which is stored against SWA's userId for later requests.
/// </summary>
public sealed class IdentityVerificationService(
    ICurrentUser currentUser,
    IGraphTokenAccessor tokenAccessor,
    IGraphProfileClient graphClient,
    IVerifiedIdentityStore store,
    TimeProvider timeProvider)
{
    public async Task<VerifiedIdentity> VerifyAsync(CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.SwaUserId is null)
        {
            throw new UnauthenticatedException();
        }

        string token = tokenAccessor.GetForwardedToken() ?? throw new GraphTokenMissingException();
        GraphProfile profile = await graphClient.GetMeAsync(token, cancellationToken);

        IdentityMatch.EnsureSameUser(currentUser, profile);

        var identity = new VerifiedIdentity(
            currentUser.SwaUserId,
            profile.Id,
            profile.UserPrincipalName ?? currentUser.UserDetails ?? string.Empty,
            profile.DisplayName ?? string.Empty,
            timeProvider.GetUtcNow());

        store.Save(identity);
        return identity;
    }
}
