using System.Collections.Concurrent;
using SwaApimPoc.Application.Abstractions;
using SwaApimPoc.Domain.Identity;

namespace SwaApimPoc.Infrastructure.Identity;

/// <summary>
/// POC only: the mapping resets when the App Service restarts. The real app would keep it in Azure SQL.
/// </summary>
public sealed class InMemoryVerifiedIdentityStore : IVerifiedIdentityStore
{
    private readonly ConcurrentDictionary<string, VerifiedIdentity> identities = new();

    public VerifiedIdentity? Find(string swaUserId) =>
        identities.TryGetValue(swaUserId, out VerifiedIdentity? identity) ? identity : null;

    public void Save(VerifiedIdentity identity) => identities[identity.SwaUserId] = identity;
}
