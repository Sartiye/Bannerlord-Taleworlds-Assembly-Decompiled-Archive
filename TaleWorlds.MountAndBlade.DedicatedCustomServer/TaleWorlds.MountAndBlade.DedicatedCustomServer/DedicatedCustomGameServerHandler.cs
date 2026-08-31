using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Diamond;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.MountAndBlade.DedicatedCustomServer;

public class DedicatedCustomGameServerHandler : ICustomBattleServerSessionHandler
{
	private CustomBattleServer _customBattleServer;

	public DedicatedCustomGameServerHandler(CustomBattleServer customBattleServer)
	{
		_customBattleServer = customBattleServer;
	}

	void ICustomBattleServerSessionHandler.OnConnected()
	{
	}

	void ICustomBattleServerSessionHandler.OnCantConnect()
	{
		Module.CurrentModule.ShutDownWithDelay("Couldn't connect to custom battle server manager", 20);
	}

	void ICustomBattleServerSessionHandler.OnDisconnected()
	{
		Module.CurrentModule.ShutDownWithDelay("Disconnected from custom battle server manager", 20);
	}

	void ICustomBattleServerSessionHandler.OnStateChanged(CustomBattleServer.State state)
	{
	}

	void ICustomBattleServerSessionHandler.OnSuccessfulGameRegister()
	{
		GameNetwork.AddNetworkHandler(new CustomGameServerNetworkHandler(_customBattleServer));
		LobbyGameStateDedicatedCustomServer lobbyGameStateDedicatedCustomServer = Game.Current.GameStateManager.CreateState<LobbyGameStateDedicatedCustomServer>();
		lobbyGameStateDedicatedCustomServer.SetStartingParameters(_customBattleServer);
		Game.Current.GameStateManager.PushState(lobbyGameStateDedicatedCustomServer);
	}

	async Task<PlayerJoinGameResponseDataFromHost[]> ICustomBattleServerSessionHandler.OnClientWantsToConnectCustomGame(PlayerJoinGameData[] playerJoinData)
	{
		bool isFull = MultiplayerOptions.OptionType.CultureTeam2.GetIntValue() < GameNetwork.NetworkPeerCount + playerJoinData.Length;
		List<PlayerJoinGameResponseDataFromHost> playerJoinResponses = new List<PlayerJoinGameResponseDataFromHost>();
		bool peerTriedToJoinDuringLoading = false;
		while (Mission.Current != null && Mission.Current.CurrentState != Mission.State.Continuing)
		{
			peerTriedToJoinDuringLoading = true;
			await Task.Delay(1);
		}
		if (peerTriedToJoinDuringLoading)
		{
			Debug.Print("Peers tried to join the custom game during loading...");
		}
		bool flag = false;
		PlayerJoinGameData[] array = playerJoinData;
		for (int i = 0; i < array.Length; i++)
		{
			if (CustomGameBannedPlayerManager.IsUserBanned(array[i].PlayerId))
			{
				flag = true;
				break;
			}
		}
		CustomGameJoinResponse customGameJoinResponse;
		if (isFull || flag)
		{
			customGameJoinResponse = (isFull ? CustomGameJoinResponse.ServerCapacityIsFull : ((!flag) ? CustomGameJoinResponse.UnspecifiedError : CustomGameJoinResponse.PlayerBanned));
		}
		else
		{
			List<PlayerConnectionInfo> list = new List<PlayerConnectionInfo>();
			array = playerJoinData;
			foreach (PlayerJoinGameData playerJoinGameData in array)
			{
				PlayerConnectionInfo playerConnectionInfo = new PlayerConnectionInfo(playerJoinGameData.PlayerId);
				Dictionary<int, List<int>> usedIndicesFromIds = CosmeticsManagerHelper.GetUsedIndicesFromIds(playerJoinGameData.UsedCosmetics);
				playerConnectionInfo.AddParameter("PlayerData", playerJoinGameData.PlayerData);
				playerConnectionInfo.AddParameter("UsedCosmetics", usedIndicesFromIds);
				playerConnectionInfo.AddParameter("PlayerId", playerJoinGameData.PlayerId);
				playerConnectionInfo.AddParameter("IsAdmin", playerJoinGameData.IsAdmin);
				playerConnectionInfo.AddParameter("IpAddress", playerJoinGameData.IpAddress);
				playerConnectionInfo.Name = playerJoinGameData.Name;
				list.Add(playerConnectionInfo);
			}
			GameNetwork.AddPlayersResult addPlayersResult = GameNetwork.HandleNewClientsConnect(list.ToArray(), isAdmin: false);
			if (addPlayersResult.Success)
			{
				for (int j = 0; j < playerJoinData.Length; j++)
				{
					PlayerJoinGameResponseDataFromHost item = new PlayerJoinGameResponseDataFromHost
					{
						PlayerId = playerJoinData[j].PlayerId,
						PeerIndex = addPlayersResult.NetworkPeers[j].Index,
						SessionKey = addPlayersResult.NetworkPeers[j].SessionKey,
						IsAdmin = addPlayersResult.NetworkPeers[j].IsAdmin
					};
					playerJoinResponses.Add(item);
				}
				customGameJoinResponse = CustomGameJoinResponse.Success;
			}
			else
			{
				customGameJoinResponse = CustomGameJoinResponse.ErrorOnGameServer;
			}
		}
		if (customGameJoinResponse != 0)
		{
			array = playerJoinData;
			foreach (PlayerJoinGameData playerJoinGameData2 in array)
			{
				PlayerJoinGameResponseDataFromHost item2 = new PlayerJoinGameResponseDataFromHost
				{
					PlayerId = playerJoinGameData2.PlayerId,
					PeerIndex = -1,
					SessionKey = -1,
					IsAdmin = false
				};
				playerJoinResponses.Add(item2);
			}
		}
		foreach (PlayerJoinGameResponseDataFromHost item3 in playerJoinResponses)
		{
			item3.CustomGameJoinResponse = customGameJoinResponse;
		}
		return playerJoinResponses.ToArray();
	}

	void ICustomBattleServerSessionHandler.OnGameFinished()
	{
	}

	void ICustomBattleServerSessionHandler.OnChatFilterListsReceived(string[] profanityList, string[] allowList)
	{
		(Game.Current?.GetGameHandler<ChatBox>())?.SetChatFilterLists(profanityList, allowList);
	}

	void ICustomBattleServerSessionHandler.OnClientQuitFromCustomGame(PlayerId playerId)
	{
		HandleClientQuitFromCustomGame(playerId);
	}

	void ICustomBattleServerSessionHandler.OnPlayerKickRequested(PlayerId playerID, bool isBanning)
	{
		NetworkCommunicator networkCommunicator = GameNetwork.NetworkPeers.FirstOrDefault((NetworkCommunicator p) => p.VirtualPlayer.Id == playerID);
		if (networkCommunicator != null)
		{
			DisconnectInfo disconnectInfo = networkCommunicator.PlayerConnectionInfo.GetParameter<DisconnectInfo>("DisconnectInfo") ?? new DisconnectInfo();
			disconnectInfo.Type = DisconnectType.KickedByHost;
			networkCommunicator.PlayerConnectionInfo.AddParameter("DisconnectInfo", disconnectInfo);
			GameNetwork.AddNetworkPeerToDisconnectAsServer(networkCommunicator);
		}
	}

	private async void HandleClientQuitFromCustomGame(PlayerId playerId)
	{
		while (Mission.Current != null && Mission.Current.CurrentState != Mission.State.Continuing)
		{
			await Task.Delay(1);
		}
		NetworkCommunicator networkCommunicator = GameNetwork.NetworkPeers.SingleOrDefault((NetworkCommunicator x) => x.VirtualPlayer.Id == playerId);
		if (networkCommunicator != null && !networkCommunicator.IsServerPeer)
		{
			networkCommunicator.QuitFromMission = true;
			GameNetwork.AddNetworkPeerToDisconnectAsServer(networkCommunicator);
			PlayerId playerId2 = playerId;
			MBDebug.Print("player with id " + playerId2.ToString() + " quit from game");
		}
	}
}
