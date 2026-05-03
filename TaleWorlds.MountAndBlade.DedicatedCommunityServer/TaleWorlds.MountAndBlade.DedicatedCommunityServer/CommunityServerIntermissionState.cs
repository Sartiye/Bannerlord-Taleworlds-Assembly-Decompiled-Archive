using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.ListedServer;

namespace TaleWorlds.MountAndBlade.DedicatedCommunityServer;

public class CommunityServerIntermissionState : GameState, IIntermissionState
{
	private CommunityServer _communityServer;

	public void SetStartingParameters(CommunityServer communityServer)
	{
		_communityServer = communityServer;
	}

	protected override void OnActivate()
	{
		base.OnActivate();
		if (_communityServer.Finished)
		{
			base.GameStateManager.PopState();
		}
	}

	protected override void OnInitialize()
	{
		base.OnInitialize();
		HandleServerStartMultiplayer();
	}

	private void HandleServerStartMultiplayer()
	{
		GameNetwork.PreStartMultiplayerOnServer();
		BannerlordNetwork.StartMultiplayerLobbyMission(LobbyMissionType.Community);
		GameNetwork.StartMultiplayerOnServer(_communityServer.Port);
		MBDebug.Print("Server: I finished loading and I am now visible to clients in the server list.", 0, Debug.DebugColor.White, 17179869184uL);
	}

	protected override void OnFinalize()
	{
		GameNetwork.EndMultiplayer();
		base.OnFinalize();
	}
}
