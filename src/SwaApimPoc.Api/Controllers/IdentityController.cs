using Microsoft.AspNetCore.Mvc;
using SwaApimPoc.Application.Identity;
using SwaApimPoc.Domain.Identity;

namespace SwaApimPoc.Api.Controllers;

[ApiController]
[Route("api/identity")]
public sealed class IdentityController(IdentityVerificationService verificationService) : ControllerBase
{
    /// <summary>
    /// Needs X-Graph-Token (User.Read). Confirms the Entra oid through Graph and links it to SWA's userId.
    /// </summary>
    [HttpPost("verify")]
    public async Task<ActionResult<VerifiedIdentity>> Verify(CancellationToken cancellationToken) =>
        await verificationService.VerifyAsync(cancellationToken);
}
