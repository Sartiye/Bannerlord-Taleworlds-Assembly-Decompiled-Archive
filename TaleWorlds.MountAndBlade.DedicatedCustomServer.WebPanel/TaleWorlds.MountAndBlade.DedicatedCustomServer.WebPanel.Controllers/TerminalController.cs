using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace TaleWorlds.MountAndBlade.DedicatedCustomServer.WebPanel.Controllers;

[Authorize]
public class TerminalController : Controller
{
	private IMemoryCache _memCache;

	public TerminalController(IMemoryCache memCache)
	{
		_memCache = memCache;
	}

	[HttpGet]
	public IActionResult Index()
	{
		return View();
	}

	[HttpGet]
	public List<string> Get()
	{
		return _memCache.Get<ConcurrentQueue<string>>("queue").ToList();
	}
}
