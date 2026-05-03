using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Diamond;
using TaleWorlds.MountAndBlade.ListedServer;

namespace TaleWorlds.MountAndBlade.DedicatedCustomServer;

public sealed class LobbyGameStateDedicatedCustomServer : LobbyGameState, IIntermissionState
{
	private CustomBattleServer _dedicatedCustomGameServer;

	public void SetStartingParameters(CustomBattleServer dedicatedCustomGameServer)
	{
		_dedicatedCustomGameServer = dedicatedCustomGameServer;
	}

	protected override void OnActivate()
	{
		base.OnActivate();
		if (_dedicatedCustomGameServer.Finished)
		{
			base.GameStateManager.PopState();
		}
	}

	protected override void StartMultiplayer()
	{
		HandleServerStartMultiplayer();
	}

	private void HandleServerStartMultiplayer()
	{
		GameNetwork.PreStartMultiplayerOnServer();
		BannerlordNetwork.StartMultiplayerLobbyMission(LobbyMissionType.Custom);
		GameNetwork.StartMultiplayerOnServer(_dedicatedCustomGameServer.Port);
		MBDebug.Print("Server: I finished loading and I am now visible to clients in the server list.", 0, Debug.DebugColor.White, 17179869184uL);
	}
}
