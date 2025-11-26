using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.HUDExtensions;

public class MissionMultiplayerHUDExtensionVM : ViewModel
{
	private const float RemainingTimeWarningThreshold = 5f;

	private readonly Mission _mission;

	private readonly Dictionary<MissionPeer, MPPlayerVM> _teammateDictionary;

	private readonly Dictionary<MissionPeer, MPPlayerVM> _enemyDictionary;

	private readonly MissionScoreboardComponent _missionScoreboardComponent;

	private readonly MissionLobbyEquipmentNetworkComponent _missionLobbyEquipmentNetworkComponent;

	private readonly MissionMultiplayerGameModeBaseClient _gameMode;

	private readonly bool _isTeamsEnabled;

	private bool _isAttackerTeamAlly;

	private bool _isTeammateAndEnemiesRelevant;

	private bool _isTeamScoresEnabled;

	private bool _isTeamScoresDirty;

	private bool _isOrderActive;

	private CommanderInfoVM _commanderInfo;

	private MissionMultiplayerSpectatorHUDVM _spectatorControls;

	private bool _warnRemainingTime;

	private bool _isRoundCountdownAvailable;

	private bool _isRoundCountdownSuspended;

	private bool _showTeamScores;

	private string _remainingRoundTime;

	private string _allyTeamColor;

	private string _allyTeamColor2;

	private string _enemyTeamColor;

	private string _enemyTeamColor2;

	private string _warmupInfoText;

	private int _allyTeamScore = -1;

	private int _enemyTeamScore = -1;

	private MBBindingList<MPPlayerVM> _teammatesList;

	private MBBindingList<MPPlayerVM> _enemiesList;

	private bool _showHUD;

	private bool _showCommanderInfo;

	private bool _showPowerLevels;

	private bool _isInWarmup;

	private int _generalWarningCountdown;

	private bool _isGeneralWarningCountdownActive;

	private BannerImageIdentifierVM _defenderBanner;

	private BannerImageIdentifierVM _attackerBanner;

	private Team _playerTeam
	{
		get
		{
			if (!GameNetwork.IsMyPeerReady)
			{
				return null;
			}
			MissionPeer component = GameNetwork.MyPeer.GetComponent<MissionPeer>();
			if (component == null)
			{
				return null;
			}
			if (component == null)
			{
				return null;
			}
			if (component.Team == null || component.Team.Side == BattleSideEnum.None)
			{
				return null;
			}
			return component.Team;
		}
	}

	[DataSourceProperty]
	public bool IsOrderActive
	{
		get
		{
			return _isOrderActive;
		}
		set
		{
			if (value != _isOrderActive)
			{
				_isOrderActive = value;
				OnPropertyChangedWithValue(value, "IsOrderActive");
			}
		}
	}

	[DataSourceProperty]
	public CommanderInfoVM CommanderInfo
	{
		get
		{
			return _commanderInfo;
		}
		set
		{
			if (value != _commanderInfo)
			{
				_commanderInfo = value;
				OnPropertyChangedWithValue(value, "CommanderInfo");
			}
		}
	}

	[DataSourceProperty]
	public MissionMultiplayerSpectatorHUDVM SpectatorControls
	{
		get
		{
			return _spectatorControls;
		}
		set
		{
			if (value != _spectatorControls)
			{
				_spectatorControls = value;
				OnPropertyChangedWithValue(value, "SpectatorControls");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<MPPlayerVM> Teammates
	{
		get
		{
			return _teammatesList;
		}
		set
		{
			if (value != _teammatesList)
			{
				_teammatesList = value;
				OnPropertyChangedWithValue(value, "Teammates");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<MPPlayerVM> Enemies
	{
		get
		{
			return _enemiesList;
		}
		set
		{
			if (value != _enemiesList)
			{
				_enemiesList = value;
				OnPropertyChangedWithValue(value, "Enemies");
			}
		}
	}

	[DataSourceProperty]
	public BannerImageIdentifierVM AllyBanner
	{
		get
		{
			return _defenderBanner;
		}
		set
		{
			if (value != _defenderBanner)
			{
				_defenderBanner = value;
				OnPropertyChangedWithValue(value, "AllyBanner");
			}
		}
	}

	[DataSourceProperty]
	public BannerImageIdentifierVM EnemyBanner
	{
		get
		{
			return _attackerBanner;
		}
		set
		{
			if (value != _attackerBanner)
			{
				_attackerBanner = value;
				OnPropertyChangedWithValue(value, "EnemyBanner");
			}
		}
	}

	[DataSourceProperty]
	public bool IsRoundCountdownAvailable
	{
		get
		{
			return _isRoundCountdownAvailable;
		}
		set
		{
			if (value != _isRoundCountdownAvailable)
			{
				_isRoundCountdownAvailable = value;
				OnPropertyChangedWithValue(value, "IsRoundCountdownAvailable");
			}
		}
	}

	[DataSourceProperty]
	public bool IsRoundCountdownSuspended
	{
		get
		{
			return _isRoundCountdownSuspended;
		}
		set
		{
			if (value != _isRoundCountdownSuspended)
			{
				_isRoundCountdownSuspended = value;
				OnPropertyChangedWithValue(value, "IsRoundCountdownSuspended");
			}
		}
	}

	[DataSourceProperty]
	public bool ShowTeamScores
	{
		get
		{
			return _showTeamScores;
		}
		set
		{
			if (value != _showTeamScores)
			{
				_showTeamScores = value;
				OnPropertyChangedWithValue(value, "ShowTeamScores");
			}
		}
	}

	[DataSourceProperty]
	public string RemainingRoundTime
	{
		get
		{
			return _remainingRoundTime;
		}
		set
		{
			if (value != _remainingRoundTime)
			{
				_remainingRoundTime = value;
				OnPropertyChangedWithValue(value, "RemainingRoundTime");
			}
		}
	}

	[DataSourceProperty]
	public bool WarnRemainingTime
	{
		get
		{
			return _warnRemainingTime;
		}
		set
		{
			if (value != _warnRemainingTime)
			{
				_warnRemainingTime = value;
				OnPropertyChangedWithValue(value, "WarnRemainingTime");
			}
		}
	}

	[DataSourceProperty]
	public int AllyTeamScore
	{
		get
		{
			return _allyTeamScore;
		}
		set
		{
			if (value != _allyTeamScore)
			{
				_allyTeamScore = value;
				OnPropertyChangedWithValue(value, "AllyTeamScore");
			}
		}
	}

	[DataSourceProperty]
	public int EnemyTeamScore
	{
		get
		{
			return _enemyTeamScore;
		}
		set
		{
			if (value != _enemyTeamScore)
			{
				_enemyTeamScore = value;
				OnPropertyChangedWithValue(value, "EnemyTeamScore");
			}
		}
	}

	[DataSourceProperty]
	public string AllyTeamColor
	{
		get
		{
			return _allyTeamColor;
		}
		set
		{
			if (value != _allyTeamColor)
			{
				_allyTeamColor = value;
				OnPropertyChangedWithValue(value, "AllyTeamColor");
			}
		}
	}

	[DataSourceProperty]
	public string AllyTeamColor2
	{
		get
		{
			return _allyTeamColor2;
		}
		set
		{
			if (value != _allyTeamColor2)
			{
				_allyTeamColor2 = value;
				OnPropertyChangedWithValue(value, "AllyTeamColor2");
			}
		}
	}

	[DataSourceProperty]
	public string EnemyTeamColor
	{
		get
		{
			return _enemyTeamColor;
		}
		set
		{
			if (value != _enemyTeamColor)
			{
				_enemyTeamColor = value;
				OnPropertyChangedWithValue(value, "EnemyTeamColor");
			}
		}
	}

	[DataSourceProperty]
	public string EnemyTeamColor2
	{
		get
		{
			return _enemyTeamColor2;
		}
		set
		{
			if (value != _enemyTeamColor2)
			{
				_enemyTeamColor2 = value;
				OnPropertyChangedWithValue(value, "EnemyTeamColor2");
			}
		}
	}

	[DataSourceProperty]
	public bool ShowHud
	{
		get
		{
			return _showHUD;
		}
		set
		{
			if (value != _showHUD)
			{
				_showHUD = value;
				OnPropertyChangedWithValue(value, "ShowHud");
			}
		}
	}

	[DataSourceProperty]
	public bool ShowCommanderInfo
	{
		get
		{
			return _showCommanderInfo;
		}
		set
		{
			if (value != _showCommanderInfo)
			{
				_showCommanderInfo = value;
				OnPropertyChangedWithValue(value, "ShowCommanderInfo");
				UpdateShowTeamScores();
			}
		}
	}

	[DataSourceProperty]
	public bool ShowPowerLevels
	{
		get
		{
			return _showPowerLevels;
		}
		set
		{
			if (value != _showPowerLevels)
			{
				_showPowerLevels = value;
				OnPropertyChangedWithValue(value, "ShowPowerLevels");
			}
		}
	}

	[DataSourceProperty]
	public bool IsInWarmup
	{
		get
		{
			return _isInWarmup;
		}
		set
		{
			if (value != _isInWarmup)
			{
				_isInWarmup = value;
				OnPropertyChangedWithValue(value, "IsInWarmup");
				UpdateShowTeamScores();
				CommanderInfo?.UpdateWarmupDependentFlags(_isInWarmup);
			}
		}
	}

	[DataSourceProperty]
	public string WarmupInfoText
	{
		get
		{
			return _warmupInfoText;
		}
		set
		{
			if (value != _warmupInfoText)
			{
				_warmupInfoText = value;
				OnPropertyChangedWithValue(value, "WarmupInfoText");
			}
		}
	}

	[DataSourceProperty]
	public int GeneralWarningCountdown
	{
		get
		{
			return _generalWarningCountdown;
		}
		set
		{
			if (value != _generalWarningCountdown)
			{
				_generalWarningCountdown = value;
				OnPropertyChangedWithValue(value, "GeneralWarningCountdown");
			}
		}
	}

	[DataSourceProperty]
	public bool IsGeneralWarningCountdownActive
	{
		get
		{
			return _isGeneralWarningCountdownActive;
		}
		set
		{
			if (value != _isGeneralWarningCountdownActive)
			{
				_isGeneralWarningCountdownActive = value;
				OnPropertyChangedWithValue(value, "IsGeneralWarningCountdownActive");
			}
		}
	}

	public MissionMultiplayerHUDExtensionVM(Mission mission)
	{
		_mission = mission;
		_missionScoreboardComponent = mission.GetMissionBehavior<MissionScoreboardComponent>();
		_gameMode = _mission.GetMissionBehavior<MissionMultiplayerGameModeBaseClient>();
		SpectatorControls = new MissionMultiplayerSpectatorHUDVM(_mission);
		if (_gameMode.RoundComponent != null)
		{
			_gameMode.RoundComponent.OnCurrentRoundStateChanged += OnCurrentGameModeStateChanged;
		}
		if (_gameMode.WarmupComponent != null)
		{
			_gameMode.WarmupComponent.OnWarmupEnded += OnCurrentGameModeStateChanged;
		}
		_missionScoreboardComponent.OnRoundPropertiesChanged += SetTeamScoresDirty;
		MissionPeer.OnTeamChanged += OnTeamChanged;
		NetworkCommunicator.OnPeerComponentAdded += OnPeerComponentAdded;
		Mission.Current.OnMissionReset += OnMissionReset;
		MissionLobbyComponent missionBehavior = mission.GetMissionBehavior<MissionLobbyComponent>();
		_isTeamsEnabled = missionBehavior.MissionType != MultiplayerGameType.Duel;
		_missionLobbyEquipmentNetworkComponent = mission.GetMissionBehavior<MissionLobbyEquipmentNetworkComponent>();
		IsRoundCountdownAvailable = _gameMode.IsGameModeUsingRoundCountdown;
		IsRoundCountdownSuspended = false;
		_isTeamScoresEnabled = _isTeamsEnabled;
		UpdateShowTeamScores();
		Teammates = new MBBindingList<MPPlayerVM>();
		Enemies = new MBBindingList<MPPlayerVM>();
		_teammateDictionary = new Dictionary<MissionPeer, MPPlayerVM>();
		_enemyDictionary = new Dictionary<MissionPeer, MPPlayerVM>();
		ShowHud = true;
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		string strValue = MultiplayerOptions.OptionType.GameType.GetStrValue();
		TextObject textObject = new TextObject("{=XJTX8w8M}Warmup Phase - {GAME_MODE}{newline}Waiting for players to join");
		textObject.SetTextVariable("GAME_MODE", GameTexts.FindText("str_multiplayer_official_game_type_name", strValue));
		WarmupInfoText = textObject.ToString();
		SpectatorControls.RefreshValues();
	}

	private void OnMissionReset(object sender, PropertyChangedEventArgs e)
	{
		IsGeneralWarningCountdownActive = false;
	}

	private void OnPeerComponentAdded(PeerComponent component)
	{
		if (component.IsMine && component is MissionRepresentativeBase)
		{
			MissionRepresentativeBase missionRepresentative = GameNetwork.MyPeer?.VirtualPlayer.GetComponent<MissionRepresentativeBase>();
			AllyTeamScore = _missionScoreboardComponent.GetRoundScore(BattleSideEnum.Attacker);
			EnemyTeamScore = _missionScoreboardComponent.GetRoundScore(BattleSideEnum.Defender);
			_isTeammateAndEnemiesRelevant = Mission.Current.GetMissionBehavior<MissionMultiplayerGameModeBaseClient>().IsGameModeTactical && !Mission.Current.HasMissionBehavior<MissionMultiplayerSiegeClient>() && _gameMode.GameType != MultiplayerGameType.Battle;
			CommanderInfo = new CommanderInfoVM(missionRepresentative);
			ShowCommanderInfo = true;
			if (_isTeammateAndEnemiesRelevant)
			{
				OnRefreshTeamMembers();
				OnRefreshEnemyMembers();
			}
			ShowPowerLevels = _gameMode.GameType == MultiplayerGameType.Battle;
		}
	}

	public override void OnFinalize()
	{
		MissionPeer.OnTeamChanged -= OnTeamChanged;
		if (_gameMode.RoundComponent != null)
		{
			_gameMode.RoundComponent.OnCurrentRoundStateChanged -= OnCurrentGameModeStateChanged;
		}
		if (_gameMode.WarmupComponent != null)
		{
			_gameMode.WarmupComponent.OnWarmupEnded -= OnCurrentGameModeStateChanged;
		}
		_missionScoreboardComponent.OnRoundPropertiesChanged -= SetTeamScoresDirty;
		NetworkCommunicator.OnPeerComponentAdded -= OnPeerComponentAdded;
		CommanderInfo?.OnFinalize();
		CommanderInfo = null;
		SpectatorControls?.OnFinalize();
		SpectatorControls = null;
		base.OnFinalize();
	}

	public void Tick(float dt)
	{
		IsInWarmup = _gameMode.IsInWarmup;
		CheckTimers();
		if (_isTeammateAndEnemiesRelevant)
		{
			OnRefreshTeamMembers();
			OnRefreshEnemyMembers();
		}
		if (_isTeamScoresDirty)
		{
			UpdateTeamScores();
			_isTeamScoresDirty = false;
		}
		_commanderInfo?.Tick(dt);
		_spectatorControls?.Tick(dt);
	}

	private void CheckTimers(bool forceUpdate = false)
	{
		if (_gameMode.CheckTimer(out var remainingTime, out var remainingWarningTime, forceUpdate))
		{
			RemainingRoundTime = TimeSpan.FromSeconds(remainingTime).ToString("mm':'ss");
			WarnRemainingTime = (float)remainingTime <= 5f;
			if (GeneralWarningCountdown != remainingWarningTime)
			{
				IsGeneralWarningCountdownActive = remainingWarningTime > 0;
				GeneralWarningCountdown = remainingWarningTime;
			}
		}
	}

	private void OnToggleLoadout(bool isActive)
	{
		ShowHud = !isActive;
	}

	public void OnSpectatedAgentFocusIn(Agent followedAgent)
	{
		_spectatorControls?.OnSpectatedAgentFocusIn(followedAgent);
	}

	public void OnSpectatedAgentFocusOut(Agent followedPeer)
	{
		_spectatorControls?.OnSpectatedAgentFocusOut(followedPeer);
	}

	private void OnCurrentGameModeStateChanged()
	{
		CheckTimers(forceUpdate: true);
	}

	private void SetTeamScoresDirty()
	{
		_isTeamScoresDirty = true;
	}

	private void UpdateTeamScores()
	{
		if (_isTeamScoresEnabled)
		{
			int roundScore = _missionScoreboardComponent.GetRoundScore(BattleSideEnum.Attacker);
			int roundScore2 = _missionScoreboardComponent.GetRoundScore(BattleSideEnum.Defender);
			AllyTeamScore = (_isAttackerTeamAlly ? roundScore : roundScore2);
			EnemyTeamScore = (_isAttackerTeamAlly ? roundScore2 : roundScore);
		}
	}

	private void UpdateTeamBanners()
	{
		BannerImageIdentifierVM bannerImageIdentifierVM = new BannerImageIdentifierVM(Mission.Current.AttackerTeam?.Banner, nineGrid: true);
		BannerImageIdentifierVM bannerImageIdentifierVM2 = new BannerImageIdentifierVM(Mission.Current.DefenderTeam?.Banner, nineGrid: true);
		AllyBanner = (_isAttackerTeamAlly ? bannerImageIdentifierVM : bannerImageIdentifierVM2);
		EnemyBanner = (_isAttackerTeamAlly ? bannerImageIdentifierVM2 : bannerImageIdentifierVM);
	}

	private void OnTeamChanged(NetworkCommunicator peer, Team previousTeam, Team newTeam)
	{
		if (peer.IsMine)
		{
			if (_isTeamScoresEnabled || _gameMode.GameType == MultiplayerGameType.Battle)
			{
				_isAttackerTeamAlly = newTeam.Side == BattleSideEnum.Attacker;
				SetTeamScoresDirty();
			}
			CommanderInfo?.OnTeamChanged();
		}
		if (CommanderInfo == null)
		{
			return;
		}
		Teammates.SingleOrDefault((MPPlayerVM x) => x.Peer.GetNetworkPeer() == peer)?.RefreshTeam();
		GetTeamColors(Mission.Current.AttackerTeam, out var color, out var color2);
		if (_isTeamScoresEnabled || _gameMode.GameType == MultiplayerGameType.Battle)
		{
			GetTeamColors(Mission.Current.DefenderTeam, out var color3, out var color4);
			if (_isAttackerTeamAlly)
			{
				AllyTeamColor = color;
				AllyTeamColor2 = color2;
				EnemyTeamColor = color3;
				EnemyTeamColor2 = color4;
			}
			else
			{
				AllyTeamColor = color3;
				AllyTeamColor2 = color4;
				EnemyTeamColor = color;
				EnemyTeamColor2 = color2;
			}
			CommanderInfo.RefreshColors(AllyTeamColor, AllyTeamColor2, EnemyTeamColor, EnemyTeamColor2);
		}
		else
		{
			AllyTeamColor = color;
			AllyTeamColor2 = color2;
			CommanderInfo.RefreshColors(AllyTeamColor, AllyTeamColor2, EnemyTeamColor, EnemyTeamColor2);
		}
		UpdateTeamBanners();
	}

	private void GetTeamColors(Team team, out string color, out string color2)
	{
		color = team.Color.ToString("X");
		color = color.Remove(0, 2);
		color = "#" + color + "FF";
		color2 = team.Color2.ToString("X");
		color2 = color2.Remove(0, 2);
		color2 = "#" + color2 + "FF";
	}

	private void OnRefreshTeamMembers()
	{
		List<MPPlayerVM> list = Teammates.ToList();
		foreach (MissionPeer item in VirtualPlayer.Peers<MissionPeer>())
		{
			if (item.GetNetworkPeer().GetComponent<MissionPeer>() != null && _playerTeam != null && item.Team != null && item.Team == _playerTeam && item.Team != Mission.Current.SpectatorTeam)
			{
				if (_teammateDictionary.TryGetValue(item, out var value))
				{
					list.Remove(value);
					continue;
				}
				MPPlayerVM mPPlayerVM = new MPPlayerVM(item);
				Teammates.Add(mPPlayerVM);
				_teammateDictionary.Add(item, mPPlayerVM);
			}
		}
		foreach (MPPlayerVM item2 in list)
		{
			Teammates.Remove(item2);
			_teammateDictionary.Remove(item2.Peer);
		}
		foreach (MPPlayerVM teammate in Teammates)
		{
			teammate.RefreshDivision();
			teammate.RefreshGold();
			teammate.RefreshProperties();
			teammate.UpdateDisabled();
		}
	}

	private void OnRefreshEnemyMembers()
	{
		List<MPPlayerVM> list = Enemies.ToList();
		foreach (MissionPeer item in VirtualPlayer.Peers<MissionPeer>())
		{
			if (item.GetNetworkPeer().GetComponent<MissionPeer>() != null && _playerTeam != null && item.Team != null && item.Team != _playerTeam && item.Team != Mission.Current.SpectatorTeam)
			{
				if (_enemyDictionary.TryGetValue(item, out var value))
				{
					list.Remove(value);
					continue;
				}
				MPPlayerVM mPPlayerVM = new MPPlayerVM(item);
				Enemies.Add(mPPlayerVM);
				_enemyDictionary.Add(item, mPPlayerVM);
			}
		}
		foreach (MPPlayerVM item2 in list)
		{
			Enemies.Remove(item2);
			_enemyDictionary.Remove(item2.Peer);
		}
		foreach (MPPlayerVM enemy in Enemies)
		{
			enemy.RefreshDivision();
			enemy.UpdateDisabled();
		}
	}

	private void UpdateShowTeamScores()
	{
		ShowTeamScores = !_gameMode.IsInWarmup && ShowCommanderInfo && _gameMode.GameType != MultiplayerGameType.Siege;
	}
}
