using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.DedicatedCustomServer.WebPanel;

public class Program
{
	public static void Main(string[] args)
	{
		ApplicationPlatform.Initialize(EngineType.Standalone, Platform.Web, Runtime.DotNetCore);
		Debug.DebugManager = new DiamondDebugManager();
		BuildWebHost(args).Run();
	}

	public static IWebHost BuildWebHost(string[] args)
	{
		return WebHost.CreateDefaultBuilder(args).UseStartup<Startup>().UseUrls("http://*:5000")
			.Build();
	}
}
