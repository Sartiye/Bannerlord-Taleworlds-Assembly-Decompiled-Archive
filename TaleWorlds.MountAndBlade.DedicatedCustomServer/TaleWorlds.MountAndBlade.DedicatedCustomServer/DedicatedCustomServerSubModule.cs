using TaleWorlds.MountAndBlade.Diamond;
using TaleWorlds.MountAndBlade.ListedServer;

namespace TaleWorlds.MountAndBlade.DedicatedCustomServer;

public class DedicatedCustomServerSubModule : MBSubModuleBase
{
	public static DedicatedCustomServerSubModule Instance { get; private set; }

	public ServerSideIntermissionManager ServerSideIntermissionManager { get; private set; }

	public CustomBattleServer DedicatedCustomGameServer { get; private set; }

	protected override void OnSubModuleLoad()
	{
		base.OnSubModuleLoad();
		Instance = this;
		DedicatedCustomServerIntermissionManagerHandler dedicatedCustomServerIntermissionManagerHandler = new DedicatedCustomServerIntermissionManagerHandler();
		DedicatedCustomGameServer = dedicatedCustomServerIntermissionManagerHandler.DedicatedCustomGameServer;
		MissionLobbyComponent.AddLobbyComponentType(typeof(MissionCustomGameServerComponent), LobbyMissionType.Custom, isSeverComponent: true);
		ServerSideIntermissionManager = ServerSideIntermissionManager.Instantiate(dedicatedCustomServerIntermissionManagerHandler);
		ListedServerCommandManager.Initialize(ServerSideIntermissionManager);
	}

	protected override void OnApplicationTick(float dt)
	{
		base.OnApplicationTick(dt);
		ServerSideIntermissionManager.Tick(dt);
	}
}
