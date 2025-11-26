using TaleWorlds.Core;
using TaleWorlds.MountAndBlade.Diamond;

namespace TaleWorlds.MountAndBlade.Multiplayer;

public class MissionBasedMultiplayerGameMode : MultiplayerGameMode
{
	public MissionBasedMultiplayerGameMode(string name)
		: base(name)
	{
	}

	public override void JoinCustomGame(JoinGameData joinGameData)
	{
		LobbyGameStateCustomGameClient lobbyGameStateCustomGameClient = Game.Current.GameStateManager.CreateState<LobbyGameStateCustomGameClient>();
		lobbyGameStateCustomGameClient.SetStartingParameters(NetworkMain.GameClient, joinGameData.GameServerProperties.Address, joinGameData.GameServerProperties.Port, joinGameData.PeerIndex, joinGameData.SessionKey);
		Game.Current.GameStateManager.PushState(lobbyGameStateCustomGameClient);
	}

	public override void StartMultiplayerGame(string scene)
	{
		if (base.Name == "TeamDeathmatch")
		{
			MultiplayerMissions.OpenTeamDeathmatchMission(scene);
		}
		else if (base.Name == "Duel")
		{
			MultiplayerMissions.OpenDuelMission(scene);
		}
		else if (base.Name == "Siege")
		{
			MultiplayerMissions.OpenSiegeMission(scene);
		}
		else if (base.Name == "Battle")
		{
			MultiplayerMissions.OpenBattleMission(scene);
		}
		else if (base.Name == "Captain")
		{
			MultiplayerMissions.OpenCaptainMission(scene);
		}
		else if (base.Name == "Skirmish")
		{
			MultiplayerMissions.OpenSkirmishMission(scene);
		}
	}
}
