using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TaleWorlds.MountAndBlade.Diamond.Dashboard.Controllers;

[Authorize]
public class HomeController : Controller
{
	public IActionResult Index()
	{
		return View();
	}
}
