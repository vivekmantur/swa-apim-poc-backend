using System.Net.Http.Headers;
using System.Net.Http.Json;
using SwaApimPoc.Application.Abstractions;
using SwaApimPoc.Application.Common;
using SwaApimPoc.Application.Graph;

namespace SwaApimPoc.Infrastructure.Graph;

public sealed class GraphProfileClient(HttpClient httpClient) : IGraphProfileClient
{
    public async Task<GraphProfile> GetMeAsync(string graphToken, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get, "me?$select=id,displayName,userPrincipalName,mail");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", graphToken);

        using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new GraphCallFailedException((int)response.StatusCode);
        }

        return await response.Content.ReadFromJsonAsync<GraphProfile>(cancellationToken)
            ?? throw new GraphCallFailedException((int)response.StatusCode);
    }
}
