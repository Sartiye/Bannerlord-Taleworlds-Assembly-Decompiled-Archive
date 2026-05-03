using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NetworkMessages.FromServer;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.ModuleManager;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.MountAndBlade.ListedServer;

public class ServerSideIntermissionManager
{
	private GameStartupInfo _startupInfo;

	private ApplicationHealthChecker _applicationHealthChecker;

	private bool _shutTheProcessAfterMissionIsOver;

	private bool _shutTheProcessAfterMissionIsOverWaiting;

	private Task _currentTask;

	private const float AutomatedBattleStateClientInformDuration = 1f;

	public const int DefaultAutomatedBattleCount = 3;

	public const float DefaultAutomatedBattleWaitTimeInSeconds = 10f;

	public const float DefaultAutomatedBattleVoteTimeInSeconds = 15f;

	private float _automatedBattleVoteTimeInSeconds;

	private float _automatedBattleWaitTimeInSeconds;

	private int _automatedBattleCount;

	private bool _automatedBattleSwitchingEnabled;

	private bool _gameStarted;

	private int _remainedAutomatedBattleCount;

	private float _currentAutomatedBattleRemainingTime;

	private AutomatedBattleState _automatedBattleState;

	private float _passedTimeSinceLastAutomatedBattleStateClientInform;

	private float _currentVoteRemainingTime;

	private int _currentAutomatedBattleIndex;

	private List<string> _automatedMapPool;

	private List<CustomGameUsableMap> _usableMaps;

	private bool _portOverriden;

	private int _overridenPort;

	private IServerSideIntermissionManagerHandler _handler;

	private IListedServer _listedServer;

	public static ServerSideIntermissionManager Instance { get; private set; }

	public int AutomatedBattleCount => _automatedBattleCount;

	public int RemainedAutomatedBattleCount => _remainedAutomatedBattleCount;

	public AutomatedBattleState AutomatedBattleState => _automatedBattleState;

	public IReadOnlyList<string> AutomatedMapPool => _automatedMapPool.AsReadOnly();

	public IReadOnlyList<string> UsableMaps => _usableMaps.Select((CustomGameUsableMap item) => item.Map).ToList().AsReadOnly();

	public IListedServer ListedServer => _listedServer;

	private ServerSideIntermissionManager(IServerSideIntermissionManagerHandler handler)
	{
		_handler = handler;
		_listedServer = _handler.Server;
		InitializeBannerVisualManager();
		_startupInfo = Module.CurrentModule.StartupInfo;
		InitializeAutomatedBattleVariables();
		_usableMaps = new List<CustomGameUsableMap>();
		DedicatedServerConsoleCommandManager.AddType(typeof(ListedServerCommandManager));
		_applicationHealthChecker = new ApplicationHealthChecker();
		_applicationHealthChecker.Start();
		MultiplayerIntermissionVotingManager.Instance.OnMapItemAdded += OnIntermissionMapItemAdded;
		MultiplayerIntermissionVotingManager.Instance.OnCultureItemAdded += OnIntermissionCultureItemAdded;
		MultiplayerIntermissionVotingManager.Instance.OnMapItemVoteCountChanged += OnIntermissionMapItemVoteCountChanged;
		MultiplayerIntermissionVotingManager.Instance.OnCultureItemVoteCountChanged += OnIntermissionCultureItemVoteCountChanged;
	}

	public static ServerSideIntermissionManager Instantiate(IServerSideIntermissionManagerHandler handler)
	{
		Instance = new ServerSideIntermissionManager(handler);
		return Instance;
	}

	public static void Destroy()
	{
		Instance.Clear();
		Instance = null;
	}

	public void OnBaseGameStateInitialize()
	{
		foreach (BasicCultureObject objectType in MBObjectManager.Instance.GetObjectTypeList<BasicCultureObject>())
		{
			if (objectType.IsMainCulture)
			{
				MultiplayerIntermissionVotingManager.Instance.AddCultureItem(objectType.StringId);
			}
		}
	}

	private void OnIntermissionMapItemAdded(string mapId)
	{
		foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
		{
			if (!networkPeer.IsServerPeer)
			{
				GameNetwork.BeginModuleEventAsServer(networkPeer);
				GameNetwork.WriteMessage(new MultiplayerIntermissionMapItemAdded(mapId));
				GameNetwork.EndModuleEventAsServer();
			}
		}
	}

	private void OnIntermissionCultureItemAdded(string cultureId)
	{
		foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
		{
			if (!networkPeer.IsServerPeer)
			{
				GameNetwork.BeginModuleEventAsServer(networkPeer);
				GameNetwork.WriteMessage(new MultiplayerIntermissionCultureItemAdded(cultureId));
				GameNetwork.EndModuleEventAsServer();
			}
		}
	}

	private void OnIntermissionMapItemVoteCountChanged(int mapItemIndex, int voteCount)
	{
		foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
		{
			if (!networkPeer.IsServerPeer)
			{
				GameNetwork.BeginModuleEventAsServer(networkPeer);
				GameNetwork.WriteMessage(new MultiplayerIntermissionMapItemVoteCountChanged(mapItemIndex, voteCount));
				GameNetwork.EndModuleEventAsServer();
			}
		}
	}

	private void OnIntermissionCultureItemVoteCountChanged(int cultureItemIndex, int voteCount)
	{
		foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
		{
			if (!networkPeer.IsServerPeer)
			{
				GameNetwork.BeginModuleEventAsServer(networkPeer);
				GameNetwork.WriteMessage(new MultiplayerIntermissionCultureItemVoteCountChanged(cultureItemIndex, voteCount));
				GameNetwork.EndModuleEventAsServer();
			}
		}
	}

	public void EndGameAfterMissionIsOver()
	{
		_shutTheProcessAfterMissionIsOver = true;
	}

	public void AddMapToAutomatedBattlePool(string map)
	{
		_automatedMapPool.Add(map);
		MultiplayerIntermissionVotingManager.Instance.AddMapItem(map);
	}

	public void AddMapToUsableMaps(string usable_map_add_command)
	{
		usable_map_add_command = usable_map_add_command.Trim();
		CustomGameUsableMap usableMap;
		if (Enumerable.Contains(usable_map_add_command, ','))
		{
			int num = usable_map_add_command.IndexOf(' ');
			string map = usable_map_add_command.Substring(0, num);
			List<string> compatibleGameTypes = (from s in usable_map_add_command.Substring(num + 1).Split(new char[1] { ',' }, StringSplitOptions.RemoveEmptyEntries)
				select s.Trim()).ToList();
			usableMap = new CustomGameUsableMap(map, isCompatibleWithAllGameTypes: false, compatibleGameTypes);
		}
		else if (Enumerable.Contains(usable_map_add_command, ' '))
		{
			int num2 = usable_map_add_command.IndexOf(' ');
			string map2 = usable_map_add_command.Substring(0, num2);
			string item = usable_map_add_command.Substring(num2 + 1);
			usableMap = new CustomGameUsableMap(map2, isCompatibleWithAllGameTypes: false, new List<string> { item });
		}
		else
		{
			usableMap = new CustomGameUsableMap(usable_map_add_command, isCompatibleWithAllGameTypes: true, null);
		}
		AddMapToUsableMaps(usableMap);
	}

	private void AddMapToUsableMaps(CustomGameUsableMap usableMap)
	{
		if (!_usableMaps.Any((CustomGameUsableMap item) => item.Equals(usableMap)))
		{
			_usableMaps.Add(usableMap);
			MultiplayerIntermissionVotingManager.Instance.AddUsableMap(usableMap);
		}
		else
		{
			Debug.Print("Usable map " + usableMap.Map + " already exists. Not adding again.");
		}
	}

	private void Clear()
	{
		_applicationHealthChecker.Stop();
		MultiplayerIntermissionVotingManager.Instance.OnMapItemAdded -= OnIntermissionMapItemAdded;
		MultiplayerIntermissionVotingManager.Instance.OnCultureItemAdded -= OnIntermissionCultureItemAdded;
		MultiplayerIntermissionVotingManager.Instance.OnMapItemVoteCountChanged -= OnIntermissionMapItemVoteCountChanged;
		MultiplayerIntermissionVotingManager.Instance.OnCultureItemVoteCountChanged -= OnIntermissionCultureItemVoteCountChanged;
	}

	public void Tick(float dt)
	{
		_handler.OnEarlyTick(dt);
		_passedTimeSinceLastAutomatedBattleStateClientInform += dt;
		if (_applicationHealthChecker != null)
		{
			_applicationHealthChecker.Update();
		}
		TickAutomatedBattles(dt);
		TickShuttingDown();
		Task currentTask = _currentTask;
		if (currentTask != null && currentTask.IsCompleted)
		{
			if (currentTask.Exception != null)
			{
				throw new Exception("Couldn't start the game in time. Crashing intentionally so a new server can start.");
			}
			_currentTask = null;
		}
		_handler.OnTick(dt);
	}

	private void TickShuttingDown()
	{
		if (_shutTheProcessAfterMissionIsOver && _shutTheProcessAfterMissionIsOverWaiting && Mission.Current == null)
		{
			_handler.OnShutDownAfterMission();
			_shutTheProcessAfterMissionIsOverWaiting = false;
		}
	}

	private void InitializeAutomatedBattleVariables()
	{
		_automatedBattleWaitTimeInSeconds = 10f;
		_automatedBattleVoteTimeInSeconds = 15f;
		_automatedBattleCount = 3;
		_automatedBattleState = AutomatedBattleState.Idle;
		_automatedMapPool = new List<string>();
	}

	private void SetAutomatedBattleState(AutomatedBattleState newState)
	{
		_automatedBattleState = newState;
		InformClientsWithCurrentIntermissionState();
	}

	private void InformClientsWithCurrentIntermissionState()
	{
		MultiplayerIntermissionState multiplayerIntermissionState = MultiplayerIntermissionState.Idle;
		multiplayerIntermissionState = _automatedBattleState switch
		{
			AutomatedBattleState.Idle => MultiplayerIntermissionState.Idle, 
			AutomatedBattleState.CountingForMapVoting => MultiplayerIntermissionState.CountingForMapVote, 
			AutomatedBattleState.CountingForCultureVoting => MultiplayerIntermissionState.CountingForCultureVote, 
			AutomatedBattleState.CountingForNextBattle => (_remainedAutomatedBattleCount <= 0) ? MultiplayerIntermissionState.CountingForEnd : MultiplayerIntermissionState.CountingForMission, 
			AutomatedBattleState.StartingNextBattle => MultiplayerIntermissionState.Idle, 
			AutomatedBattleState.AtBattle => MultiplayerIntermissionState.Idle, 
			AutomatedBattleState.ShuttingDown => MultiplayerIntermissionState.CountingForEnd, 
			_ => throw new ArgumentOutOfRangeException(), 
		};
		float intermissionTimer = ((_automatedBattleState == AutomatedBattleState.CountingForNextBattle) ? _currentAutomatedBattleRemainingTime : _currentVoteRemainingTime);
		GameNetwork.BeginBroadcastModuleEvent();
		GameNetwork.WriteMessage(new MultiplayerIntermissionUpdate(multiplayerIntermissionState, intermissionTimer));
		GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.IncludeUnsynchronizedClients);
		_passedTimeSinceLastAutomatedBattleStateClientInform = 0f;
		MultiplayerIntermissionVotingManager.Instance.CurrentVoteState = multiplayerIntermissionState;
	}

	private void TickAutomatedBattles(float dt)
	{
		if (!_gameStarted || !_automatedBattleSwitchingEnabled)
		{
			return;
		}
		switch (_automatedBattleState)
		{
		case AutomatedBattleState.CountingForMapVoting:
		case AutomatedBattleState.CountingForCultureVoting:
			_currentVoteRemainingTime -= dt;
			if (_currentVoteRemainingTime <= 0f)
			{
				_currentVoteRemainingTime = 0f;
			}
			if (_passedTimeSinceLastAutomatedBattleStateClientInform >= 1f)
			{
				InformClientsWithCurrentIntermissionState();
			}
			if (!_currentVoteRemainingTime.ApproximatelyEqualsTo(0f))
			{
				break;
			}
			SelectMapAndFactions();
			if (_automatedBattleState == AutomatedBattleState.CountingForMapVoting)
			{
				if (MultiplayerIntermissionVotingManager.Instance.IsCultureVoteEnabled)
				{
					SetAutomatedBattleState(AutomatedBattleState.CountingForCultureVoting);
				}
				else
				{
					MultiplayerIntermissionVotingManager.Instance.ClearVotes();
					SetAutomatedBattleState(AutomatedBattleState.CountingForNextBattle);
					SyncOptionsToClients();
				}
				_currentVoteRemainingTime = 15f;
			}
			else if (_automatedBattleState == AutomatedBattleState.CountingForCultureVoting)
			{
				MultiplayerIntermissionVotingManager.Instance.ClearVotes();
				SetAutomatedBattleState(AutomatedBattleState.CountingForNextBattle);
				SyncOptionsToClients();
			}
			break;
		case AutomatedBattleState.CountingForNextBattle:
			_currentAutomatedBattleRemainingTime -= dt;
			if (_currentAutomatedBattleRemainingTime <= 0f)
			{
				_currentAutomatedBattleRemainingTime = 0f;
			}
			if (_passedTimeSinceLastAutomatedBattleStateClientInform >= 1f)
			{
				InformClientsWithCurrentIntermissionState();
			}
			if (_currentAutomatedBattleRemainingTime.ApproximatelyEqualsTo(0f))
			{
				if (_remainedAutomatedBattleCount > 0)
				{
					_remainedAutomatedBattleCount--;
					SetAutomatedBattleState(AutomatedBattleState.StartingNextBattle);
					_handler.OnBeforeStartingNextBattle();
					StartMission();
				}
				else
				{
					_handler.OnShutDownAfterMission();
					SetAutomatedBattleState(AutomatedBattleState.ShuttingDown);
				}
			}
			break;
		case AutomatedBattleState.StartingNextBattle:
			if (Mission.Current != null)
			{
				SetAutomatedBattleState(AutomatedBattleState.AtBattle);
			}
			break;
		case AutomatedBattleState.AtBattle:
			if (Mission.Current != null)
			{
				break;
			}
			_currentAutomatedBattleRemainingTime = _automatedBattleWaitTimeInSeconds;
			_currentVoteRemainingTime = _automatedBattleVoteTimeInSeconds;
			if (_remainedAutomatedBattleCount == 0)
			{
				SetAutomatedBattleState(AutomatedBattleState.CountingForNextBattle);
			}
			else if (_listedServer.Connected)
			{
				MultiplayerOptions.Instance.InitializeAllOptionsFromNext();
				UpdateCustomGameDataOnLobby();
				if (!MultiplayerIntermissionVotingManager.Instance.IsMapVoteEnabled && !MultiplayerIntermissionVotingManager.Instance.IsMapSelectedByAdmin)
				{
					SelectRandomMap();
				}
				GameNetwork.BeginBroadcastModuleEvent();
				GameNetwork.WriteMessage(new MultiplayerOptionsInitial());
				GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.IncludeUnsynchronizedClients);
				SetAutomatedBattleState(GetInitialState());
			}
			break;
		case AutomatedBattleState.Idle:
			break;
		}
	}

	private void UpdateCustomGameDataOnLobby()
	{
		string strValue = MultiplayerOptions.OptionType.GameType.GetStrValue();
		int intValue = MultiplayerOptions.OptionType.MaxNumberOfPlayers.GetIntValue();
		string strValue2 = MultiplayerOptions.OptionType.Map.GetStrValue();
		if (GameNetwork.IsDedicatedServer)
		{
			NetworkMain.CustomBattleServer.UpdateCustomGameData(strValue, strValue2, intValue);
		}
		else
		{
			NetworkMain.GameClient.UpdateCustomGameData(strValue, strValue2, intValue);
		}
		string uniqueMapIdentifierString = null;
		if (Utilities.TryGetUniqueIdentifiersForScene(strValue2, out var identifiers))
		{
			uniqueMapIdentifierString = identifiers.Serialize();
		}
		_handler.OnGameParametersChanged(strValue, strValue2, uniqueMapIdentifierString);
	}

	private void InitializeBannerVisualManager()
	{
		if (BannerManager.Instance == null)
		{
			BannerManager.Initialize();
			BannerManager.Instance.LoadBannerIcons(ModuleHelper.GetModuleFullPath("Native") + "ModuleData/banner_icons.xml");
		}
	}

	private bool IsNewTaskAssignable()
	{
		return _currentTask == null;
	}

	private void PrintCantCreateNewTask()
	{
		Debug.Print("--There is an already working task. You have to wait for it to be finished before creating another one.", 0, Debug.DebugColor.White, 17179869184uL);
	}

	private void SyncOptionsToClients()
	{
		GameNetwork.BeginBroadcastModuleEvent();
		GameNetwork.WriteMessage(new MultiplayerOptionsInitial());
		GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.IncludeUnsynchronizedClients);
		GameNetwork.BeginBroadcastModuleEvent();
		GameNetwork.WriteMessage(new MultiplayerOptionsImmediate());
		GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.IncludeUnsynchronizedClients);
	}

	public void StartGameAndMission()
	{
		if (IsNewTaskAssignable())
		{
			_currentTask = StartGameAndMissionAux();
		}
		else
		{
			PrintCantCreateNewTask();
		}
	}

	private async Task StartGameAndMissionAux()
	{
		await StartGameAux();
		await Task.Delay(1000);
		await StartMissionAux();
	}

	public void StartGame()
	{
		if (IsNewTaskAssignable())
		{
			_currentTask = StartGameAux();
		}
		else
		{
			PrintCantCreateNewTask();
		}
	}

	private async Task StartGameAux()
	{
		int tryCount = 0;
		while (tryCount++ < 30 && !_listedServer.IsRegistered)
		{
			await Task.Delay(1000);
		}
		if (_listedServer.IsRegistered && !_listedServer.IsPlaying)
		{
			bool flag = !string.IsNullOrEmpty(MultiplayerOptions.OptionType.Map.GetStrValue());
			if (!flag)
			{
				MBList<string> mapList = MultiplayerOptions.Instance.GetMapList();
				if (mapList.Count > 0)
				{
					MultiplayerOptions.OptionType.Map.SetValue(mapList.GetRandomElement());
					flag = true;
				}
			}
			if (flag)
			{
				if (GameStateManager.Current.ActiveState is InitialListedGameServerState)
				{
					string strValue = MultiplayerOptions.OptionType.GameType.GetStrValue();
					string strValue2 = MultiplayerOptions.OptionType.Map.GetStrValue();
					string uniqueSceneId = null;
					if (Utilities.TryGetUniqueIdentifiersForScene(strValue2, out var identifiers))
					{
						uniqueSceneId = identifiers.Serialize();
					}
					Debug.Print("--Game is starting...", 0, Debug.DebugColor.White, 17179869184uL);
					Debug.Print("--Selected scene: " + strValue2, 0, Debug.DebugColor.White, 17179869184uL);
					Debug.Print("--Hosting " + strValue, 0, Debug.DebugColor.White, 17179869184uL);
					if (string.IsNullOrEmpty(MultiplayerOptions.OptionType.ServerName.GetStrValue()))
					{
						MultiplayerOptions.OptionType.ServerName.SetValue("Dedicated Server " + MBRandom.RandomInt(1, 10000));
					}
					string strValue3 = MultiplayerOptions.OptionType.GamePassword.GetStrValue();
					string strValue4 = MultiplayerOptions.OptionType.AdminPassword.GetStrValue();
					int intValue = MultiplayerOptions.OptionType.GameDefinitionId.GetIntValue();
					string value = ((!string.IsNullOrEmpty(strValue3)) ? Common.CalculateMD5Hash(strValue3) : null);
					string value2 = ((!string.IsNullOrEmpty(strValue4)) ? Common.CalculateMD5Hash(strValue4) : null);
					MultiplayerOptions.OptionType.GamePassword.SetValue(value);
					MultiplayerOptions.OptionType.AdminPassword.SetValue(value2);
					string gameModule = MultiplayerGameTypes.GetGameTypeInfo(strValue).GameModule;
					int portToUse = Module.CurrentModule.StartupInfo.ServerPort;
					if (_portOverriden)
					{
						portToUse = _overridenPort;
					}
					string regionToUse = Module.CurrentModule.StartupInfo.ServerRegion;
					if (Module.CurrentModule.StartupInfo.PlayerHostedDedicatedServer)
					{
						regionToUse = "USER";
					}
					await _handler.OnGameStart(strValue, strValue2, uniqueSceneId, intValue, gameModule, portToUse, regionToUse);
					_gameStarted = true;
				}
				else
				{
					Debug.Print("--Unexpected state: " + GameStateManager.Current.ActiveState.GetType()?.ToString() + ". Game cannot be started. Please wait or restart the application.", 0, Debug.DebugColor.White, 17179869184uL);
				}
			}
			else
			{
				Debug.Print("--Server cannot start without any maps given.", 0, Debug.DebugColor.White, 17179869184uL);
			}
		}
		else if (!_listedServer.Connected)
		{
			if (!_startupInfo.PlayerHostedDedicatedServer)
			{
				throw new Exception("Couldn't start the game in time. Crashing intentionally so a new server can start.");
			}
			Debug.Print("--Dedicated server is not registered, check your internet connection.", 0, Debug.DebugColor.White, 17179869184uL);
		}
		else
		{
			if (!_startupInfo.PlayerHostedDedicatedServer)
			{
				throw new Exception("Couldn't start the game in time. Crashing intentionally so a new server can start.");
			}
			Debug.Print("--Connection is not at the correct state. Please wait or restart the application.", 0, Debug.DebugColor.White, 17179869184uL);
		}
	}

	public void StartMission()
	{
		if (IsNewTaskAssignable())
		{
			_currentTask = StartMissionAux();
		}
		else
		{
			PrintCantCreateNewTask();
		}
	}

	private async Task StartMissionAux()
	{
		if (_listedServer.IsRegistered && _listedServer.IsPlaying)
		{
			if (GameStateManager.Current.ActiveState is IIntermissionState)
			{
				_currentAutomatedBattleIndex++;
				if (_currentAutomatedBattleIndex > 10)
				{
					_currentAutomatedBattleIndex = 0;
				}
				GameNetwork.GetNetworkComponent<BaseNetworkComponentData>().UpdateCurrentBattleIndex(_currentAutomatedBattleIndex);
				MultiplayerOptions.Instance.GetOptionFromOptionType(MultiplayerOptions.OptionType.Map).GetValue(out string value);
				MultiplayerOptions.Instance.GetOptionFromOptionType(MultiplayerOptions.OptionType.GameType).GetValue(out string value2);
				GameNetwork.BeginBroadcastModuleEvent();
				GameNetwork.WriteMessage(new LoadMission(value2, value, _currentAutomatedBattleIndex));
				GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.IncludeUnsynchronizedClients);
				if (!Module.CurrentModule.StartMultiplayerGame(value2, value))
				{
					Debug.FailedAssert("[DEBUG]Invalid multiplayer game type.", "C:\\Develop\\MB3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.ListedServer\\ServerSideIntermissionManager.cs", "StartMissionAux", 734);
				}
				while (Mission.Current == null || Mission.Current.CurrentState != Mission.State.Continuing)
				{
					await Task.Delay(1);
				}
				if (_shutTheProcessAfterMissionIsOver)
				{
					_shutTheProcessAfterMissionIsOverWaiting = true;
				}
				MultiplayerOptions.Instance.ResetDefaultsToCurrent();
				Debug.Print("--Mission is running", 0, Debug.DebugColor.White, 17179869184uL);
			}
			else
			{
				Debug.Print("--Not at correct state, mission is already running or trying to run.", 0, Debug.DebugColor.White, 17179869184uL);
			}
		}
		else if (!_listedServer.Connected)
		{
			Debug.Print("--Dedicated server is not registered, check your internet connection.", 0, Debug.DebugColor.White, 17179869184uL);
		}
		else
		{
			Debug.Print("--Connection is not at the correct state. Please wait or restart the application.", 0, Debug.DebugColor.White, 17179869184uL);
		}
	}

	public void EndMission()
	{
		if (IsNewTaskAssignable())
		{
			_currentTask = EndMissionAux();
		}
		else
		{
			PrintCantCreateNewTask();
		}
	}

	private async Task EndMissionAux()
	{
		if (_listedServer.IsRegistered && _listedServer.IsPlaying)
		{
			if (GameStateManager.Current.ActiveState is MissionState && Mission.Current != null && !Mission.Current.MissionEnded)
			{
				Mission.Current.GetMissionBehavior<MissionLobbyComponent>().SetStateEndingAsServer();
				while (Mission.Current != null)
				{
					await Task.Delay(1);
				}
				Debug.Print("--Mission is closed", 0, Debug.DebugColor.White, 17179869184uL);
			}
			else
			{
				Debug.Print("--Not at correct state, mission does not exists.", 0, Debug.DebugColor.White, 17179869184uL);
			}
		}
		else if (!_listedServer.Connected)
		{
			Debug.Print("--Dedicated server is not registered, check your internet connection.", 0, Debug.DebugColor.White, 17179869184uL);
		}
		else
		{
			Debug.Print("--Connection is not at the correct state. Please wait or restart the application.", 0, Debug.DebugColor.White, 17179869184uL);
		}
	}

	public void Say(string message)
	{
		if (_listedServer.IsPlaying)
		{
			Debug.Print("--Sending message to all players: " + message, 0, Debug.DebugColor.White, 17179869184uL);
			GameNetwork.BeginBroadcastModuleEvent();
			GameNetwork.WriteMessage(new ServerMessage(message));
			GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.IncludeUnsynchronizedClients);
		}
		else
		{
			Debug.Print("--Cannot send a chat message when there is no game session.", 0, Debug.DebugColor.White, 17179869184uL);
		}
	}

	public void EnableAutomatedBattleSwitching()
	{
		_remainedAutomatedBattleCount = _automatedBattleCount;
		_automatedBattleSwitchingEnabled = true;
		_currentAutomatedBattleRemainingTime = _automatedBattleWaitTimeInSeconds;
		_currentVoteRemainingTime = _automatedBattleVoteTimeInSeconds;
		MultiplayerIntermissionVotingManager.Instance.IsAutomatedBattleSwitchingEnabled = _automatedBattleSwitchingEnabled;
		SelectMapAndFactions();
		SetAutomatedBattleState(GetInitialState());
	}

	private AutomatedBattleState GetInitialState()
	{
		if (MultiplayerIntermissionVotingManager.Instance.IsMapVoteEnabled)
		{
			return AutomatedBattleState.CountingForMapVoting;
		}
		if (MultiplayerIntermissionVotingManager.Instance.IsCultureVoteEnabled)
		{
			return AutomatedBattleState.CountingForCultureVoting;
		}
		return AutomatedBattleState.CountingForNextBattle;
	}

	public void SetAutomatedBattleCount(int number)
	{
		if (number < 0)
		{
			number = int.MaxValue;
		}
		_automatedBattleCount = number;
	}

	public void SetIntermissionMapVoting(bool isEnabled)
	{
		MultiplayerIntermissionVotingManager.Instance.IsMapVoteEnabled = isEnabled;
		MultiplayerIntermissionVotingManager.Instance.IsDisableMapVoteOverride = !isEnabled;
		if (isEnabled)
		{
			return;
		}
		if (_automatedBattleState == AutomatedBattleState.CountingForMapVoting)
		{
			if (MultiplayerIntermissionVotingManager.Instance.IsCultureVoteEnabled)
			{
				_currentVoteRemainingTime = 15f;
				SetAutomatedBattleState(AutomatedBattleState.CountingForCultureVoting);
			}
			else
			{
				SetAutomatedBattleState(AutomatedBattleState.CountingForNextBattle);
			}
		}
		SelectRandomMap();
	}

	public void SetIntermissionCultureVoting(bool isEnabled)
	{
		MultiplayerIntermissionVotingManager.Instance.IsCultureVoteEnabled = isEnabled;
		MultiplayerIntermissionVotingManager.Instance.IsDisableCultureVoteOverride = !isEnabled;
		if (!isEnabled)
		{
			if (_automatedBattleState == AutomatedBattleState.CountingForCultureVoting)
			{
				SetAutomatedBattleState(AutomatedBattleState.CountingForNextBattle);
			}
			SelectRandomCultures();
		}
	}

	private void SelectRandomMap()
	{
		Random random = new Random();
		string value = _automatedMapPool[random.Next(0, _automatedMapPool.Count)];
		MultiplayerOptions.Instance.GetOptionFromOptionType(MultiplayerOptions.OptionType.Map).UpdateValue(value);
	}

	private void SelectRandomCultures()
	{
		string[] array = new string[6] { "khuzait", "aserai", "battania", "vlandia", "sturgia", "empire" };
		Random random = new Random();
		string value = array[random.Next(0, array.Length)];
		string value2 = array[random.Next(0, array.Length)];
		MultiplayerOptions.Instance.GetOptionFromOptionType(MultiplayerOptions.OptionType.CultureTeam1).UpdateValue(value);
		MultiplayerOptions.Instance.GetOptionFromOptionType(MultiplayerOptions.OptionType.CultureTeam2).UpdateValue(value2);
	}

	private void SelectMapAndFactions()
	{
		MultiplayerOptions.Instance.GetOptionFromOptionType(MultiplayerOptions.OptionType.Map).GetValue(out string value);
		if (!_automatedMapPool.Contains(value) && !MultiplayerIntermissionVotingManager.Instance.GetUsableMaps(MultiplayerOptions.OptionType.GameType.GetStrValue()).Contains(value))
		{
			value = _automatedMapPool[0];
		}
		string uniqueMapIdentifierString = null;
		if (Utilities.TryGetUniqueIdentifiersForScene(value, out var identifiers))
		{
			uniqueMapIdentifierString = identifiers.Serialize();
		}
		string strValue = MultiplayerOptions.OptionType.GameType.GetStrValue();
		_handler.OnGameParametersChanged(strValue, value, uniqueMapIdentifierString);
	}

	public void SetPort(int port)
	{
		Console.WriteLine("Port set to " + port);
		_portOverriden = true;
		_overridenPort = port;
	}

	public void SetServerName(string name)
	{
		MultiplayerOptions.OptionType.ServerName.SetValue(name);
	}
}
