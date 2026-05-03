using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace TaleWorlds.MountAndBlade.Diamond.Dashboard.Controllers;

[Route("[controller]")]
[Authorize]
public class DataController : Controller
{
	private class RequiredPrivilege : ActionFilterAttribute
	{
	}
}
