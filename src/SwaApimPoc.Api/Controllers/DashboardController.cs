using Microsoft.AspNetCore.Mvc;
using SwaApimPoc.Application.Dashboard;

namespace SwaApimPoc.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
public sealed class DashboardController(DashboardService dashboardService) : ControllerBase
{
    [HttpGet]
    public ActionResult<DashboardDto> Get() => dashboardService.GetDashboard();
}
