using System;
using System.Collections.Generic;
using System.Linq;
using NetworkMessages.FromServer;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Diamond;
using TaleWorlds.MountAndBlade.MissionRepresentatives;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.MountAndBlade.DedicatedCustomServer;

public class MissionCustomGameServerComponent : MissionLobbyComponent
{
	private CustomBattleServer _customBattleServer;

	private IRoundComponent _roundComponent;

	private MultiplayerWarmupComponent _warmupComponent;

	private MissionScoreboardComponent _missionScoreboardComponent;

	private MissionMultiplayerGameModeBase _gameMode;

	private MultiplayerGameLogger _gameLogger;

	private MultipleBattleResult _battleResult;

	private int _currentMatchIndex = -1;

	private bool _warmupEnded;

	private bool _defenderHasAtLeastOnePlayer;

	private bool _attackerHasAtLeastOnePlayer;

	private bool _isBattleStartEventSent;

	private Dictionary<int, int> _teamScores;

	private Dictionary<PlayerId, int> _playerScores;

	public override void OnBehaviorInitialize()
	{
		base.OnBehaviorInitialize();
		_missionScoreboardComponent = base.Mission.GetMissionBehavior<MissionScoreboardComponent>();
		_customBattleServer = DedicatedCustomServerSubModule.Instance.DedicatedCustomGameServer;
		_gameMode = base.Mission.GetMissionBehavior<MissionMultiplayerGameModeBase>();
		_gameLogger = Game.Current.GetGameHandler<MultiplayerGameLogger>();
		if (GameNetwork.IsServer)
		{
			_battleResult = _customBattleServer.BattleResult;
		}
	}

	public override void AfterStart()
	{
		base.AfterStart();
		MissionMultiplayerGameModeBaseClient missionBehavior = Mission.Current.GetMissionBehavior<MissionMultiplayerGameModeBaseClient>();
		_roundComponent = missionBehavior?.RoundComponent;
		if (_roundComponent != null)
		{
			_roundComponent.OnPostRoundEnded += OnPostRoundEnded;
		}
		_warmupComponent = missionBehavior?.WarmupComponent;
		_warmupEnded = true;
		if (_warmupComponent != null)
		{
			_warmupComponent.OnWarmupEnded += OnWarmupEnded;
			_warmupEnded = false;
		}
		MissionMultiplayerGameModeBase missionBehavior2 = Mission.Current.GetMissionBehavior<MissionMultiplayerGameModeBase>();
		MultiplayerGameType missionType = _gameMode.GetMissionType();
		if (missionType == MultiplayerGameType.Siege && missionBehavior2 is MissionMultiplayerSiege)
		{
			(missionBehavior2 as MissionMultiplayerSiege).OnDestructableComponentDestroyed += OnDestructableComponentDestroyed;
			(missionBehavior2 as MissionMultiplayerSiege).OnObjectiveGoldGained += OnObjectiveGoldGained;
		}
		else if (missionType == MultiplayerGameType.Duel && missionBehavior2 is MissionMultiplayerDuel)
		{
			(missionBehavior2 as MissionMultiplayerDuel).OnDuelEnded += OnDuelEnded;
		}
		_battleResult.CreateNewBattleResult(missionType.ToString());
		_currentMatchIndex++;
		_missionScoreboardComponent.OnRoundPropertiesChanged += OnRoundPropertiesChanged;
		_teamScores = new Dictionary<int, int>();
		_playerScores = new Dictionary<PlayerId, int>();
		MissionPeer.OnTeamChanged += OnMissionPeerTeamChanged;
	}

	private void OnRoundPropertiesChanged()
	{
		_teamScores[0] = _missionScoreboardComponent.GetSideSafe(BattleSideEnum.Defender).SideScore;
		_teamScores[1] = _missionScoreboardComponent.GetSideSafe(BattleSideEnum.Attacker).SideScore;
		_customBattleServer?.UpdateBattleStats(_battleResult.GetCurrentBattleResult(), _teamScores, _playerScores);
	}

	protected override void OnEndMission()
	{
		if (_roundComponent != null)
		{
			_roundComponent.OnPostRoundEnded -= OnPostRoundEnded;
		}
		if (_warmupComponent != null)
		{
			_warmupComponent.OnWarmupEnded -= OnWarmupEnded;
		}
		MissionMultiplayerGameModeBase missionBehavior = Mission.Current.GetMissionBehavior<MissionMultiplayerGameModeBase>();
		MultiplayerGameType missionType = _gameMode.GetMissionType();
		if (missionType == MultiplayerGameType.Siege && missionBehavior is MissionMultiplayerSiege)
		{
			(missionBehavior as MissionMultiplayerSiege).OnDestructableComponentDestroyed -= OnDestructableComponentDestroyed;
			(missionBehavior as MissionMultiplayerSiege).OnObjectiveGoldGained -= OnObjectiveGoldGained;
		}
		else if (missionType == MultiplayerGameType.Duel && missionBehavior is MissionMultiplayerDuel)
		{
			(missionBehavior as MissionMultiplayerDuel).OnDuelEnded -= OnDuelEnded;
		}
		BattleSideEnum winnerTeamNo = BattleSideEnum.None;
		if (missionType != MultiplayerGameType.Duel)
		{
			winnerTeamNo = _missionScoreboardComponent.GetMatchWinnerSide();
		}
		_battleResult.GetCurrentBattleResult().SetBattleFinished((int)winnerTeamNo, isPremadeGame: false, PremadeGameType.Invalid);
		if (missionType == MultiplayerGameType.Siege || missionType == MultiplayerGameType.TeamDeathmatch)
		{
			MissionScoreboardComponent.MissionScoreboardSide sideSafe = _missionScoreboardComponent.GetSideSafe(BattleSideEnum.Attacker);
			if (sideSafe != null)
			{
				AddScoresToStats(missionType, sideSafe.Players);
			}
			MissionScoreboardComponent.MissionScoreboardSide sideSafe2 = _missionScoreboardComponent.GetSideSafe(BattleSideEnum.Defender);
			if (sideSafe2 != null)
			{
				AddScoresToStats(missionType, sideSafe2.Players);
			}
		}
		_missionScoreboardComponent.OnRoundPropertiesChanged -= OnRoundPropertiesChanged;
		MissionPeer.OnTeamChanged -= OnMissionPeerTeamChanged;
		base.OnEndMission();
		_customBattleServer.BattleFinished(_battleResult.GetCurrentBattleResult(), _teamScores, _playerScores);
	}

	protected override void HandleLateNewClientAfterSynchronized(NetworkCommunicator networkPeer)
	{
		base.HandleLateNewClientAfterSynchronized(networkPeer);
		string parameter = networkPeer.PlayerConnectionInfo.GetParameter<string>("TeamNo");
		int teamNo = -1;
		if (!string.IsNullOrEmpty(parameter))
		{
			teamNo = int.Parse(parameter);
		}
		BattleResult currentBattleResult = _battleResult.GetCurrentBattleResult();
		PlayerId id = networkPeer.GetComponent<MissionPeer>().Peer.Id;
		currentBattleResult.AddOrUpdatePlayerEntry(id, teamNo, _customBattleServer.CustomGameType, Guid.Empty);
		networkPeer.PlayerConnectionInfo.GetParameter<PlayerData>("PlayerData");
		_customBattleServer.UpdateBattleStats(_battleResult.GetCurrentBattleResult(), _teamScores, _playerScores);
	}

	private void OnMissionPeerTeamChanged(NetworkCommunicator peer, Team previousTeam, Team newTeam)
	{
		_battleResult.GetCurrentBattleResult().AddOrUpdatePlayerEntry(peer.GetComponent<MissionPeer>().Peer.Id, (int)newTeam.Side, _customBattleServer.CustomGameType, Guid.Empty);
		if (_missionScoreboardComponent.IsOneSided)
		{
			if (!_isBattleStartEventSent)
			{
				string strValue = MultiplayerOptions.OptionType.CultureTeam1.GetStrValue();
				string strValue2 = MultiplayerOptions.OptionType.CultureTeam2.GetStrValue();
				Dictionary<PlayerId, int> playerTeams = _battleResult.GetCurrentBattleResult().PlayerEntries.Select((KeyValuePair<string, BattlePlayerEntry> pe) => (Key: pe.Key, TeamNo: pe.Value.TeamNo)).ToDictionary(((string Key, int TeamNo) kv) => PlayerId.FromString(kv.Key), ((string Key, int TeamNo) kv) => kv.TeamNo);
				_customBattleServer.BattleStarted(playerTeams, strValue, strValue2);
				_isBattleStartEventSent = true;
			}
			return;
		}
		if (!_defenderHasAtLeastOnePlayer && newTeam.Side == BattleSideEnum.Defender)
		{
			_defenderHasAtLeastOnePlayer = true;
		}
		if (!_attackerHasAtLeastOnePlayer && newTeam.Side == BattleSideEnum.Attacker)
		{
			_attackerHasAtLeastOnePlayer = true;
		}
		if (_defenderHasAtLeastOnePlayer && _attackerHasAtLeastOnePlayer && !_isBattleStartEventSent)
		{
			string strValue3 = MultiplayerOptions.OptionType.CultureTeam1.GetStrValue();
			string strValue4 = MultiplayerOptions.OptionType.CultureTeam2.GetStrValue();
			Dictionary<PlayerId, int> playerTeams2 = _battleResult.GetCurrentBattleResult().PlayerEntries.Select((KeyValuePair<string, BattlePlayerEntry> pe) => (Key: pe.Key, TeamNo: pe.Value.TeamNo)).ToDictionary(((string Key, int TeamNo) kv) => PlayerId.FromString(kv.Key), ((string Key, int TeamNo) kv) => kv.TeamNo);
			_customBattleServer.BattleStarted(playerTeams2, strValue3, strValue4);
			_isBattleStartEventSent = true;
		}
	}

	protected override void HandlePlayerDisconnect(NetworkCommunicator networkPeer)
	{
		base.HandlePlayerDisconnect(networkPeer);
		_customBattleServer.UpdateBattleStats(_battleResult.GetCurrentBattleResult(), _teamScores, _playerScores);
	}

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

	private void AddScoresToStats(MultiplayerGameType gameType, IEnumerable<MissionPeer> peers)
	{
		foreach (MissionPeer peer in peers)
		{
			if (_battleResult.GetCurrentBattleResult().TryGetPlayerEntry(peer.Peer.Id, out var battlePlayerEntry))
			{
				if (battlePlayerEntry != null && gameType == MultiplayerGameType.TeamDeathmatch)
				{
					(battlePlayerEntry.PlayerStats as BattlePlayerStatsTeamDeathmatch).Score = peer.Score;
				}
				else if (gameType == MultiplayerGameType.Siege)
				{
					(battlePlayerEntry.PlayerStats as BattlePlayerStatsSiege).Score = peer.Score;
				}
			}
		}
	}

	public override void OnAgentBuild(Agent agent, Banner banner)
	{
		base.OnAgentBuild(agent, banner);
	}

	public override void OnScoreHit(Agent affectedAgent, Agent affectorAgent, WeaponComponentData attackerWeapon, bool isBlocked, bool isSiegeEngineHit, in Blow blow, in AttackCollisionData collisionData, float damagedHp, float hitDistance, float shotDifficulty)
	{
		base.OnScoreHit(affectedAgent, affectorAgent, attackerWeapon, isBlocked, isSiegeEngineHit, in blow, in collisionData, damagedHp, hitDistance, shotDifficulty);
		if (affectorAgent?.MissionPeer != null)
		{
			Team team = affectorAgent.Team;
			if (team != null)
			{
				_ = team.Side;
				if (true && affectorAgent.Team.Side == affectedAgent?.Team?.Side)
				{
					GameLog gameLog = new GameLog(GameLogType.FriendlyHit, affectorAgent.MissionPeer.Peer.Id, MBCommon.GetTotalMissionTime());
					MissionPeer missionPeer = affectedAgent?.MissionPeer ?? affectedAgent?.OwningAgentMissionPeer;
					if (missionPeer != null)
					{
						gameLog.Data.Add("Victim", missionPeer.Peer.Id.ToString());
					}
					_gameLogger.Log(gameLog);
				}
			}
		}
		_customBattleServer.UpdateBattleStats(_battleResult.GetCurrentBattleResult(), _teamScores, _playerScores);
	}

	protected override void OnPlayerKills(MissionPeer killerPeer, Agent killedAgent, MissionPeer assistorPeer)
	{
		base.OnPlayerKills(killerPeer, killedAgent, assistorPeer);
		PlayerId id = killerPeer.Peer.Id;
		BattleResult currentBattleResult = _battleResult.GetCurrentBattleResult();
		if (_warmupEnded && currentBattleResult.TryGetPlayerEntry(id, out var battlePlayerEntry))
		{
			battlePlayerEntry.PlayerStats.Kills = killerPeer.KillCount;
		}
		if (killerPeer != null && killedAgent != null && killedAgent.IsHuman)
		{
			GameLog gameLog = new GameLog(GameLogType.Kill, killerPeer.Peer.Id, MBCommon.GetTotalMissionTime());
			gameLog.Data.Add("IsFriendly", (killerPeer.Team?.Side == killedAgent?.Team?.Side).ToString());
			if (killedAgent.MissionPeer != null)
			{
				gameLog.Data.Add("Victim", killedAgent.MissionPeer.Peer.Id.ToString());
			}
			if (assistorPeer != null)
			{
				gameLog.Data.Add("Assist", assistorPeer.Peer.Id.ToString());
			}
			if (_warmupEnded && _gameMode.GetMissionType() == MultiplayerGameType.Siege && killerPeer.ControlledAgent?.CurrentlyUsedGameObject != null && currentBattleResult.TryGetPlayerEntry(killerPeer.Peer.Id, out var battlePlayerEntry2))
			{
				(battlePlayerEntry2.PlayerStats as BattlePlayerStatsSiege).SiegeEngineKills++;
			}
			_gameLogger.Log(gameLog);
		}
		_customBattleServer.UpdateBattleStats(_battleResult.GetCurrentBattleResult(), _teamScores, _playerScores);
	}

	protected override void OnBotKills(Agent botAgent, Agent killedAgent)
	{
		base.OnBotKills(botAgent, killedAgent);
		if (!_warmupEnded)
		{
			return;
		}
		if (botAgent != null)
		{
			MissionPeer owningAgentMissionPeer = botAgent.OwningAgentMissionPeer;
			if (owningAgentMissionPeer != null)
			{
				PlayerId id = owningAgentMissionPeer.Peer.Id;
				if (_battleResult.GetCurrentBattleResult().TryGetPlayerEntry(id, out var battlePlayerEntry))
				{
					battlePlayerEntry.PlayerStats.Kills = owningAgentMissionPeer.KillCount;
				}
			}
		}
		_customBattleServer.UpdateBattleStats(_battleResult.GetCurrentBattleResult(), _teamScores, _playerScores);
	}

	protected override void OnPlayerDies(MissionPeer peer, MissionPeer affectorPeer, MissionPeer assistorPeer)
	{
		base.OnPlayerDies(peer, affectorPeer, assistorPeer);
		if (!_warmupEnded)
		{
			return;
		}
		PlayerId id = peer.Peer.Id;
		BattleResult currentBattleResult = _battleResult.GetCurrentBattleResult();
		if (currentBattleResult.TryGetPlayerEntry(id, out var battlePlayerEntry))
		{
			battlePlayerEntry.PlayerStats.Deaths++;
		}
		if (assistorPeer != null)
		{
			PlayerId id2 = assistorPeer.Peer.Id;
			if (currentBattleResult.TryGetPlayerEntry(id2, out var battlePlayerEntry2))
			{
				battlePlayerEntry2.PlayerStats.Assists = assistorPeer.AssistCount;
			}
		}
		_customBattleServer.UpdateBattleStats(_battleResult.GetCurrentBattleResult(), _teamScores, _playerScores);
	}

	private void OnDestructableComponentDestroyed(DestructableComponent destructibleComponent, ScriptComponentBehavior attackerScriptComponentBehaviour, MissionPeer[] contributors)
	{
		if (!_warmupEnded)
		{
			return;
		}
		_gameMode.GetMissionType();
		foreach (MissionPeer missionPeer in contributors)
		{
			if (_battleResult.GetCurrentBattleResult().TryGetPlayerEntry(missionPeer.Peer.Id, out var battlePlayerEntry) && CheckForComponent<SiegeWeapon>(destructibleComponent))
			{
				((BattlePlayerStatsSiege)battlePlayerEntry.PlayerStats).SiegeEnginesDestroyed++;
			}
			_customBattleServer.UpdateBattleStats(_battleResult.GetCurrentBattleResult(), _teamScores, _playerScores);
		}
	}

	private void OnObjectiveGoldGained(MissionPeer peer, int goldGain)
	{
		if (_warmupEnded)
		{
			if (_battleResult.GetCurrentBattleResult().TryGetPlayerEntry(peer.Peer.Id, out var battlePlayerEntry))
			{
				(battlePlayerEntry.PlayerStats as BattlePlayerStatsSiege).ObjectiveGoldGained += goldGain;
			}
			_customBattleServer.UpdateBattleStats(_battleResult.GetCurrentBattleResult(), _teamScores, _playerScores);
		}
	}

	private void OnDuelEnded(MissionPeer winnerPeer, TroopType troopType)
	{
		if (!_warmupEnded || winnerPeer == null)
		{
			return;
		}
		_gameMode.GetMissionType();
		if (_battleResult.GetCurrentBattleResult().TryGetPlayerEntry(winnerPeer.Peer.Id, out var battlePlayerEntry))
		{
			BattlePlayerStatsBase playerStats = battlePlayerEntry.PlayerStats;
			(playerStats as BattlePlayerStatsDuel).DuelsWon++;
			switch (troopType)
			{
			case TroopType.Infantry:
				(playerStats as BattlePlayerStatsDuel).InfantryWins++;
				break;
			case TroopType.Ranged:
				(playerStats as BattlePlayerStatsDuel).ArcherWins++;
				break;
			case TroopType.Cavalry:
				(playerStats as BattlePlayerStatsDuel).CavalryWins++;
				break;
			}
		}
		_playerScores = (from p in GameNetwork.NetworkPeers
			select p.GetComponent<MissionPeer>() into mp
			where mp != null
			select mp).ToDictionary((MissionPeer mp) => mp.Peer.Id, (MissionPeer mp) => (mp.Representative as DuelMissionRepresentative).Score);
		_customBattleServer.UpdateBattleStats(_battleResult.GetCurrentBattleResult(), _teamScores, _playerScores);
	}

	private void OnPostRoundEnded()
	{
		_customBattleServer.UpdateBattleStats(_battleResult.GetCurrentBattleResult(), _teamScores, _playerScores);
	}

	private void OnWarmupEnded()
	{
		if (_warmupComponent != null)
		{
			_warmupComponent.OnWarmupEnded -= OnWarmupEnded;
			_warmupEnded = true;
		}
	}

	private bool CheckForComponent<T>(DestructableComponent destructibleComponent) where T : ScriptComponentBehavior
	{
		if (destructibleComponent != null)
		{
			_ = destructibleComponent.GameEntity;
			List<WeakGameEntity> children = new List<WeakGameEntity>();
			destructibleComponent.GameEntity.GetChildrenRecursive(ref children);
			foreach (WeakGameEntity item in children)
			{
				if (item.HasScriptOfType<T>())
				{
					return true;
				}
			}
		}
		return false;
	}
}
