using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade.ListedServer;

namespace TaleWorlds.MountAndBlade.DedicatedCommunityServer;

public class CommunityServerIntermissionManagerHandler : IServerSideIntermissionManagerHandler
{
	private CommunityServer _communityServer;

	IListedServer IServerSideIntermissionManagerHandler.Server => _communityServer;

	public CommunityServerIntermissionManagerHandler()
	{
		_communityServer = new CommunityServer();
		_communityServer.IsRegistered = true;
		_communityServer.Connected = true;
	}

	void IServerSideIntermissionManagerHandler.OnBeforeStartingNextBattle()
	{
	}

	void IServerSideIntermissionManagerHandler.OnGameParametersChanged(string gameType, string mapID, string uniqueMapIdentifierString)
	{
	}

	void IServerSideIntermissionManagerHandler.OnShutDownAfterMission()
	{
	}

	void IServerSideIntermissionManagerHandler.OnEarlyTick(float dt)
	{
		Game current = Game.Current;
		if (current != null)
		{
			GameStateManager gameStateManager = current.GameStateManager;
			if (gameStateManager != null && gameStateManager.ActiveState is UnspecifiedDedicatedServerState)
			{
				InitialListedGameServerState gameState = Game.Current.GameStateManager.CreateState<InitialListedGameServerState>();
				gameStateManager.CleanAndPushState(gameState);
			}
		}
	}

	void IServerSideIntermissionManagerHandler.OnTick(float dt)
	{
	}

	async Task IServerSideIntermissionManagerHandler.OnGameStart(string selectedGameType, string selectedScene, string uniqueSceneId, int gameDefinitionId, string gameModule, int portToUse, string regionToUse)
	{
		await Task.Delay(10);
		_communityServer.Port = portToUse;
		_communityServer.IsPlaying = true;
		CommunityServerIntermissionState communityServerIntermissionState = Game.Current.GameStateManager.CreateState<CommunityServerIntermissionState>();
		communityServerIntermissionState.SetStartingParameters(_communityServer);
		Game.Current.GameStateManager.PushState(communityServerIntermissionState);
	}
}
