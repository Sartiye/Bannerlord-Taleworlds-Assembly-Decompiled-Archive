using System;
using TaleWorlds.Library.Http;

namespace TaleWorlds.MountAndBlade.DotNetCore;

public class DotNetCoreSubModule : MBSubModuleBase
{
	protected override void OnSubModuleLoad()
	{
		base.OnSubModuleLoad();
		AppContext.TryGetSwitch("System.Net.SocketsHttpHandler.Http3Support", out var _);
		AppContext.SetSwitch("System.Net.SocketsHttpHandler.Http3Support", isEnabled: true);
		HttpDriverManager.AddHttpDriver("DotNetCore", new DotNetCoreHttpDriver());
		HttpDriverManager.SetDefault("DotNetCore");
	}
}
