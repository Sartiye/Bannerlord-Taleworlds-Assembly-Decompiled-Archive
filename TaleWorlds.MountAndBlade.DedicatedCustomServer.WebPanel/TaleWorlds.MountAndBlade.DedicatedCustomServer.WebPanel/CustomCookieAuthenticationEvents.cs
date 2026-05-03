using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using TaleWorlds.MountAndBlade.DedicatedCustomServer.WebPanel.Controllers;

namespace TaleWorlds.MountAndBlade.DedicatedCustomServer.WebPanel;

public class CustomCookieAuthenticationEvents : CookieAuthenticationEvents
{
	public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
	{
		string text = (from c in context.Principal.Claims
			where c.Type == "SecurityGuid"
			select c.Value).FirstOrDefault();
		string text2 = (from c in context.Principal.Claims
			where c.Type == "HashedAdminPassword"
			select c.Value).FirstOrDefault();
		if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(text2) || text != AuthController.SecurityGuid.ToString() || text2 != AuthController.HashedAdminPassword)
		{
			context.RejectPrincipal();
			await context.HttpContext.SignOutAsync("Cookies");
		}
	}
}
