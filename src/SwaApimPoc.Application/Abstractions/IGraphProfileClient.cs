using SwaApimPoc.Application.Graph;

namespace SwaApimPoc.Application.Abstractions;

public interface IGraphProfileClient
{
    Task<GraphProfile> GetMeAsync(string graphToken, CancellationToken cancellationToken);
}
