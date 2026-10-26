using Microsoft.AspNetCore.Mvc;

namespace CampusX.Controllers;

public class HomeController : Controller
{
    public IActionResult Index() => View();
    public IActionResult Error() => View();
}
