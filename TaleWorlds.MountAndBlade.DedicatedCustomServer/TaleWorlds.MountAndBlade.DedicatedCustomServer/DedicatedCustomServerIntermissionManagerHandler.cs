using System;
using System.Linq;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.Diamond;
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
			((Client<CustomBattleServer>)(object)DedicatedCustomGameServer).Update();
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
		if (DedicatedCustomGameServer.IsRegistered)
		{
			DedicatedCustomGameServer.UpdateGameProperties(gameType, mapID, uniqueMapIdentifierString);
		}
	}

	async Task IServerSideIntermissionManagerHandler.OnGameStart(string selectedGameType, string selectedScene, string uniqueSceneId, int gameDefinitionId, string gameModule, int portToUse, string regionToUse)
	{
		await DedicatedCustomGameServer.RegisterGame(gameDefinitionId, gameModule, selectedGameType, MultiplayerOptions.OptionType.ServerName.GetStrValue(), MultiplayerOptions.OptionType.CultureTeam2.GetIntValue(), selectedScene, uniqueSceneId, portToUse, regionToUse, MultiplayerOptions.OptionType.GamePassword.GetStrValue(), MultiplayerOptions.OptionType.AdminPassword.GetStrValue(), Module.CurrentModule.StartupInfo.Permission, Module.CurrentModule.StartupInfo.CustomServerHostIP);
	}
}
