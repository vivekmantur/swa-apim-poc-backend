using SwaApimPoc.Domain.Identity;

namespace SwaApimPoc.Application.Abstractions;

public interface IVerifiedIdentityStore
{
    VerifiedIdentity? Find(string swaUserId);

    void Save(VerifiedIdentity identity);
}
