using System.Collections.Generic;
using System.Threading.Tasks;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Diamond;

namespace TaleWorlds.MountAndBlade.DedicatedCommunityServer;

public class ConnectNewPlayerJob : Job
{
	private bool _initialTick;

	public PlayerJoinGameData PlayerJoinData { get; private set; }

	public PlayerJoinGameResponseDataFromHost Response { get; private set; }

	public ConnectNewPlayerJob(PlayerJoinGameData playerJoinData)
	{
		PlayerJoinData = playerJoinData;
		Response = new PlayerJoinGameResponseDataFromHost();
		_initialTick = true;
	}

	public override void DoJob(float dt)
	{
		base.DoJob(dt);
		if (_initialTick)
		{
			_initialTick = false;
			DoJobAux();
		}
	}

	private async void DoJobAux()
	{
		int num = 1;
		bool isAdmin = false;
		bool authorized = true;
		bool isFull = MultiplayerOptions.OptionType.MaxNumberOfPlayers.GetIntValue() < GameNetwork.NetworkPeerCount + num;
		bool spectatorRequestUnsupported = PlayerJoinData.JoinType == CustomGameJoinType.Spectator;
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
		CustomGameJoinResponse customGameJoinResponse;
		if (authorized && !isFull && !spectatorRequestUnsupported)
		{
			PlayerConnectionInfo playerConnectionInfo = new PlayerConnectionInfo(PlayerJoinData.PlayerId);
			Dictionary<int, List<int>> usedIndicesFromIds = CosmeticsManagerHelper.GetUsedIndicesFromIds(PlayerJoinData.UsedCosmetics);
			playerConnectionInfo.AddParameter("PlayerData", PlayerJoinData.PlayerData);
			playerConnectionInfo.AddParameter("UsedCosmetics", usedIndicesFromIds);
			playerConnectionInfo.AddParameter("PlayerId", PlayerJoinData.PlayerId);
			playerConnectionInfo.Name = PlayerJoinData.Name;
			GameNetwork.AddPlayersResult addPlayersResult = GameNetwork.HandleNewClientsConnect(new PlayerConnectionInfo[1] { playerConnectionInfo }, isAdmin);
			if (addPlayersResult.Success)
			{
				Response = new PlayerJoinGameResponseDataFromHost
				{
					PlayerId = PlayerJoinData.PlayerId,
					PeerIndex = addPlayersResult.NetworkPeers[0].Index,
					SessionKey = addPlayersResult.NetworkPeers[0].SessionKey,
					JoinType = PlayerJoinData.JoinType
				};
				customGameJoinResponse = CustomGameJoinResponse.Success;
			}
			else
			{
				customGameJoinResponse = CustomGameJoinResponse.ErrorOnGameServer;
			}
		}
		else if (!authorized)
		{
			customGameJoinResponse = CustomGameJoinResponse.IncorrectPassword;
		}
		else if (isFull)
		{
			customGameJoinResponse = CustomGameJoinResponse.ServerCapacityIsFull;
		}
		else if (spectatorRequestUnsupported)
		{
			Debug.Print("Rejected a spectator join request: community servers do not support spectators.");
			customGameJoinResponse = CustomGameJoinResponse.UnspecifiedError;
		}
		else
		{
			customGameJoinResponse = CustomGameJoinResponse.UnspecifiedError;
		}
		if (customGameJoinResponse != 0)
		{
			Response = new PlayerJoinGameResponseDataFromHost
			{
				PlayerId = PlayerJoinData.PlayerId,
				PeerIndex = -1,
				SessionKey = -1,
				JoinType = PlayerJoinData.JoinType
			};
		}
		Response.CustomGameJoinResponse = customGameJoinResponse;
		base.Finished = true;
	}
}
