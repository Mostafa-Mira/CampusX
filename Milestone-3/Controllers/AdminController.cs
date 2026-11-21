using CampusX.Models;
using CampusX.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusX.Controllers;

[Authorize(Policy = MarketplaceAccessPolicies.Admin)]
public class AdminController : Controller
{
    private readonly AdminDashboardBL _dashboardBL;

    public AdminController(AdminDashboardBL dashboardBL)
    {
        _dashboardBL = dashboardBL;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var viewModel = await _dashboardBL.GetOverviewAsync();
        return View(viewModel);
    }
}