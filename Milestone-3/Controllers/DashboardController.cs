using CampusX.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusX.Controllers;

[AllowAnonymous]
public class DashboardController : Controller
{
    private readonly AdminDashboardBL _dashboardBL;

    public DashboardController(AdminDashboardBL dashboardBL)
    {
        _dashboardBL = dashboardBL;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var viewModel = await _dashboardBL.GetPublicOverviewAsync();
        return View(viewModel);
    }
}
