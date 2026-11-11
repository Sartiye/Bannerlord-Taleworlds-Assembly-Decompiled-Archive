using System;
using System.Linq;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.Diamond.ClientApplication;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade.Diamond;
using TaleWorlds.MountAndBlade.ListedServer;

namespace TaleWorlds.MountAndBlade.DedicatedCustomServer;

public class DedicatedCustomServerIntermissionManagerHandler : IServerSideIntermissionManagerHandler
{
	private int _lastSentLogId;

	private DiamondClientApplication _diamondClientApplication;

	private GameStartupInfo _startupInfo;

	private CustomServerListedServerAdapter _adapter;

	private string _lastSentStreamerPasswordHash;

	public CustomBattleServer DedicatedCustomGameServer { get; private set; }

	public MultiplayerGameLogger GameLogger { get; private set; }

	IListedServer IServerSideIntermissionManagerHandler.Server => _adapter;

	public DedicatedCustomServerIntermissionManagerHandler()
	{
		_startupInfo = Module.CurrentModule.StartupInfo;
		ModuleInfo moduleInfo = ModuleHelper.GetModuleInfo("Multiplayer");
		ApplicationVersion applicationVersion = ApplicationVersion.Empty;
		if (moduleInfo != null)
		{
			applicationVersion = moduleInfo.Version;
		}
		else
		{
			Console.WriteLine("Multiplayer module is not loaded! Server version is invalid!");
		}
		_lastSentLogId = 0;
		ClientApplicationConfiguration clientApplicationConfiguration = new ClientApplicationConfiguration();
		clientApplicationConfiguration.FillFrom("CustomBattleServer");
		_diamondClientApplication = new DiamondClientApplication(applicationVersion);
		_diamondClientApplication.Initialize(clientApplicationConfiguration);
		DedicatedCustomGameServer = _diamondClientApplication.GetClient<CustomBattleServer>("CustomBattleServer");
		NetworkMain.SetPeers(null, null, DedicatedCustomGameServer);
		_adapter = new CustomServerListedServerAdapter(DedicatedCustomGameServer);
		DedicatedCustomGameServerHandler handler = new DedicatedCustomGameServerHandler(DedicatedCustomGameServer);
		string text = Module.CurrentModule.StartupInfo.CustomGameServerAuthToken;
		if (string.IsNullOrEmpty(text))
		{
			PlatformDirectoryPath folderPath = new PlatformDirectoryPath(PlatformFileType.User, "Tokens");
			text = FileHelper.GetFileContentString(new PlatformFilePath(folderPath, "DedicatedCustomServerAuthToken.txt"));
		}
		bool isSinglePlatformServer = Module.CurrentModule.StartupInfo.IsSinglePlatformServer;
		DedicatedCustomGameServer.Connect(handler, text, isSinglePlatformServer, Utilities.GetModulesNames(), _startupInfo.CustomGameServerAllowsOptionalModules, _startupInfo.PlayerHostedDedicatedServer);
	}

	private GameLog[] GetUnsentGameLogs()
	{
		MultiplayerGameLogger gameLogger = GameLogger;
		if (gameLogger == null || gameLogger.GameLogs?.Count != 0)
		{
			GameLog[] array = GameLogger.GameLogs.Where((GameLog log) => log.Id > _lastSentLogId).ToArray();
			if (array.Length != 0)
			{
				int id = array.Last().Id;
				_lastSentLogId = id;
			}
			return array;
		}
		return new GameLog[0];
	}

	void IServerSideIntermissionManagerHandler.OnEarlyTick(float dt)
	{
		Game current = Game.Current;
		if (current != null)
		{
			if (GameLogger == null)
			{
				GameLogger = current.GetGameHandler<MultiplayerGameLogger>();
			}
			GameStateManager gameStateManager = current.GameStateManager;
			if (gameStateManager != null && gameStateManager.ActiveState is UnspecifiedDedicatedServerState)
			{
				InitialListedGameServerState gameState = Game.Current.GameStateManager.CreateState<InitialListedGameServerState>();
				gameStateManager.CleanAndPushState(gameState);
			}
		}
		if (DedicatedCustomGameServer != null)
		{
			DedicatedCustomGameServer.Update();
		}
	}

	void IServerSideIntermissionManagerHandler.OnTick(float dt)
	{
		if (DedicatedCustomGameServer != null && DedicatedCustomGameServer.IsIdle)
		{
			DedicatedCustomGameServer.FinishAsIdle(GetUnsentGameLogs());
		}
		if (_diamondClientApplication != null)
		{
			_diamondClientApplication.Update();
		}
	}

	void IServerSideIntermissionManagerHandler.OnShutDownAfterMission()
	{
		DedicatedCustomGameServer.FinishGame(GetUnsentGameLogs());
	}

	void IServerSideIntermissionManagerHandler.OnBeforeStartingNextBattle()
	{
		DedicatedCustomGameServer?.BeforeStartingNextBattle(GetUnsentGameLogs());
	}

	void IServerSideIntermissionManagerHandler.OnGameParametersChanged(string gameType, string mapID, string uniqueMapIdentifierString)
	{
		SendGameProperties(gameType, mapID, uniqueMapIdentifierString);
	}

	public void OnStreamerOptionsChangedByAdmin()
	{
		string strValue = MultiplayerOptions.OptionType.GameType.GetStrValue();
		string strValue2 = MultiplayerOptions.OptionType.Map.GetStrValue();
		string uniqueMapIdentifierString = null;
		if (Utilities.TryGetUniqueIdentifiersForScene(strValue2, out var identifiers))
		{
			uniqueMapIdentifierString = identifiers.Serialize();
		}
		SendGameProperties(strValue, strValue2, uniqueMapIdentifierString);
	}

	private void SendGameProperties(string gameType, string mapID, string uniqueMapIdentifierString)
	{
		if (DedicatedCustomGameServer.IsRegistered)
		{
			DedicatedCustomGameServer.UpdateGameProperties(gameType, mapID, uniqueMapIdentifierString, MultiplayerOptions.OptionType.MaxStreamerCount.GetIntValue(), GetHashedStreamerPassword());
		}
	}

	private string GetHashedStreamerPassword()
	{
		string strValue = MultiplayerOptions.OptionType.StreamerPassword.GetStrValue();
		if (string.IsNullOrEmpty(strValue))
		{
			_lastSentStreamerPasswordHash = null;
			return null;
		}
		if (strValue != _lastSentStreamerPasswordHash)
		{
			_lastSentStreamerPasswordHash = Common.CalculateMD5Hash(strValue);
			MultiplayerOptions.OptionType.StreamerPassword.SetValue(_lastSentStreamerPasswordHash);
		}
		return _lastSentStreamerPasswordHash;
	}

	async Task IServerSideIntermissionManagerHandler.OnGameStart(string selectedGameType, string selectedScene, string uniqueSceneId, int gameDefinitionId, string gameModule, int portToUse, string regionToUse)
	{
		string strValue = MultiplayerOptions.OptionType.StreamerPassword.GetStrValue();
		_lastSentStreamerPasswordHash = (string.IsNullOrEmpty(strValue) ? null : strValue);
		await DedicatedCustomGameServer.RegisterGame(gameDefinitionId, gameModule, selectedGameType, MultiplayerOptions.OptionType.ServerName.GetStrValue(), MultiplayerOptions.OptionType.MaxNumberOfPlayers.GetIntValue(), selectedScene, uniqueSceneId, portToUse, regionToUse, MultiplayerOptions.OptionType.GamePassword.GetStrValue(), MultiplayerOptions.OptionType.AdminPassword.GetStrValue(), MultiplayerOptions.OptionType.StreamerPassword.GetStrValue(), Module.CurrentModule.StartupInfo.Permission, Module.CurrentModule.StartupInfo.CustomServerHostIP, MultiplayerOptions.OptionType.MaxStreamerCount.GetIntValue());
	}
}
