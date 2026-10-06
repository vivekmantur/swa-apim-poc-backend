using Microsoft.AspNetCore.Mvc;
using SwaApimPoc.Application.Graph;

namespace SwaApimPoc.Api.Controllers;

[ApiController]
[Route("api/graph")]
public sealed class GraphController(GraphProbeService graphProbeService) : ControllerBase
{
    /// <summary>
    /// The backend calls Graph /me with the forwarded token: the OBO replacement under test.
    /// </summary>
    [HttpGet("me")]
    public async Task<ActionResult<GraphProfile>> GetMe(CancellationToken cancellationToken) =>
        await graphProbeService.GetMyProfileAsync(cancellationToken);
}
