using TaleWorlds.MountAndBlade.Diamond;
using TaleWorlds.MountAndBlade.ListedServer;

namespace TaleWorlds.MountAndBlade.DedicatedCustomServer;

public class CustomServerListedServerAdapter : IListedServer
{
	public CustomBattleServer DedicatedCustomGameServer { get; private set; }

	bool IListedServer.Finished => DedicatedCustomGameServer.Finished;

	bool IListedServer.IsRegistered => DedicatedCustomGameServer.IsRegistered;

	bool IListedServer.IsPlaying => DedicatedCustomGameServer.IsPlaying;

	bool IListedServer.Connected => DedicatedCustomGameServer.Connected;

	string IListedServer.ServerTypeName => "'Custom Server'";

	public CustomServerListedServerAdapter(CustomBattleServer dedicatedCustomGameServer)
	{
		DedicatedCustomGameServer = dedicatedCustomGameServer;
	}
}
