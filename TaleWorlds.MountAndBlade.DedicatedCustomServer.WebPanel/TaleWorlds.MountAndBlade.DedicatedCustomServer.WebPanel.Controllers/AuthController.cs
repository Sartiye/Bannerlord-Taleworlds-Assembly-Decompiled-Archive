using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.DedicatedCustomServer.WebPanel.Controllers;

[AllowAnonymous]
public class AuthController : Controller
{
	public static readonly Guid SecurityGuid = Guid.NewGuid();

	public static string AdminPassword => MultiplayerOptions.OptionType.AdminPassword.GetStrValue();

	public static string HashedAdminPassword
	{
		get
		{
			if (string.IsNullOrEmpty(AdminPassword))
			{
				return string.Empty;
			}
			using MD5 mD = MD5.Create();
			byte[] bytes = Encoding.UTF8.GetBytes(AdminPassword);
			return BitConverter.ToString(mD.ComputeHash(bytes));
		}
	}

	public static IEnumerable<Claim> Claims => new Claim[2]
	{
		new Claim("SecurityGuid", SecurityGuid.ToString()),
		new Claim("HashedAdminPassword", HashedAdminPassword)
	};

	public override void OnActionExecuting(ActionExecutingContext context)
	{
		base.ViewData["Port"] = DedicatedCustomServerWebPanelSubModule.Instance.Port;
		base.OnActionExecuting(context);
	}

	public IActionResult Index()
	{
		return View();
	}

	[HttpPost]
	[ActionName("Index")]
	public IActionResult Login([FromForm] string password, [FromQuery] string returnUrl)
	{
		string text = ((!string.IsNullOrEmpty(password)) ? Common.CalculateMD5Hash(password) : null);
		Debug.Print($"Attempted sign in from IP {base.HttpContext.Connection.RemoteIpAddress.MapToIPv4()}", 0, Debug.DebugColor.Green);
		if (!string.IsNullOrEmpty(AdminPassword) && text == AdminPassword)
		{
			ClaimsPrincipal principal = new ClaimsPrincipal(new ClaimsIdentity(Claims, "Cookies"));
			base.HttpContext.SignInAsync("Cookies", principal);
			Debug.Print("Web Panel :: Admin signed in", 0, Debug.DebugColor.Green);
			return LocalRedirect(returnUrl ?? "~/");
		}
		base.ViewData["Error"] = "Incorrect password given.";
		return View("Index");
	}
}
