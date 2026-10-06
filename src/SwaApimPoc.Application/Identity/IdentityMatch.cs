using SwaApimPoc.Application.Abstractions;
using SwaApimPoc.Application.Common;
using SwaApimPoc.Application.Graph;

namespace SwaApimPoc.Application.Identity;

internal static class IdentityMatch
{
    /// <summary>
    /// The SWA session and the Graph token must describe the same person.
    /// SWA's userDetails is the sign-in name, so it is compared with the UPN and mail from Graph.
    /// </summary>
    public static void EnsureSameUser(ICurrentUser currentUser, GraphProfile profile)
    {
        bool matches =
            string.Equals(currentUser.UserDetails, profile.UserPrincipalName, StringComparison.OrdinalIgnoreCase)
            || string.Equals(currentUser.UserDetails, profile.Mail, StringComparison.OrdinalIgnoreCase);

        if (!matches)
        {
            throw new IdentityMismatchException();
        }
    }
}
