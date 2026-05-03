using TaleWorlds.Core;
using TaleWorlds.MountAndBlade.Diamond;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.MountAndBlade.DedicatedCustomServer;

public class CustomGameServerNetworkHandler : IUdpNetworkHandler
{
	private readonly CustomBattleServer _customBattleServer;

	private readonly MultiplayerGameLogger _gameLogger;

	public CustomGameServerNetworkHandler(CustomBattleServer customBattleServer)
	{
		_customBattleServer = customBattleServer;
		_gameLogger = Game.Current.GetGameHandler<MultiplayerGameLogger>();
	}

	void IUdpNetworkHandler.OnUdpNetworkHandlerClose()
	{
	}

	void IUdpNetworkHandler.OnUdpNetworkHandlerTick(float dt)
	{
	}

	void IUdpNetworkHandler.HandleNewClientConnect(PlayerConnectionInfo clientConnectionInfo)
	{
	}

	void IUdpNetworkHandler.HandleEarlyNewClientAfterLoadingFinished(NetworkCommunicator networkPeer)
	{
	}

	void IUdpNetworkHandler.HandleNewClientAfterLoadingFinished(NetworkCommunicator networkPeer)
	{
	}

	void IUdpNetworkHandler.HandleLateNewClientAfterLoadingFinished(NetworkCommunicator networkPeer)
	{
	}

	void IUdpNetworkHandler.HandleNewClientAfterSynchronized(NetworkCommunicator networkPeer)
	{
	}

	void IUdpNetworkHandler.HandleLateNewClientAfterSynchronized(NetworkCommunicator networkPeer)
	{
	}

	void IUdpNetworkHandler.HandleEarlyPlayerDisconnect(NetworkCommunicator networkPeer)
	{
	}

	void IUdpNetworkHandler.HandlePlayerDisconnect(NetworkCommunicator networkPeer)
	{
		if (_customBattleServer != null && _customBattleServer.IsRegistered)
		{
			PlayerId playerId = networkPeer.PlayerConnectionInfo.GetParameter<PlayerData>("PlayerData")?.PlayerId ?? PlayerId.Empty;
			if (networkPeer.QuitFromMission)
			{
				GameLog gameLog = new GameLog(GameLogType.PlayerDisconnect, playerId, MBCommon.GetTotalMissionTime());
				gameLog.Data.Add("Reason", DisconnectType.QuitFromGame.ToString());
				_gameLogger.Log(gameLog);
			}
			else
			{
				DisconnectType disconnectType = networkPeer.PlayerConnectionInfo.GetParameter<DisconnectInfo>("DisconnectInfo")?.Type ?? DisconnectType.Unknown;
				GameLog gameLog2 = new GameLog(GameLogType.PlayerDisconnect, playerId, MBCommon.GetTotalMissionTime());
				gameLog2.Data.Add("Reason", DisconnectType.QuitFromGame.ToString());
				_gameLogger.Log(gameLog2);
				_customBattleServer.HandlePlayerDisconnect(playerId, disconnectType);
			}
		}
	}

	void IUdpNetworkHandler.OnEveryoneUnSynchronized()
	{
	}

	void IUdpNetworkHandler.OnPlayerDisconnectedFromServer(NetworkCommunicator networkPeer)
	{
	}

	void IUdpNetworkHandler.OnDisconnectedFromServer()
	{
	}
}
