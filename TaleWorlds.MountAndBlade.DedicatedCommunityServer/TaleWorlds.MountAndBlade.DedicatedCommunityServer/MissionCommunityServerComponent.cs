using NetworkMessages.FromServer;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.DedicatedCommunityServer;

public class MissionCommunityServerComponent : MissionLobbyComponent
{
	protected override void EndGameAsServer()
	{
		base.EndGameAsServer();
		Debug.Print("EndGameAsServer called");
		GameNetwork.BeginBroadcastModuleEvent();
		GameNetwork.WriteMessage(new UnloadMission());
		GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
		GameNetwork.UnSynchronizeEveryone();
		BannerlordNetwork.EndMultiplayerLobbyMission();
	}
}
