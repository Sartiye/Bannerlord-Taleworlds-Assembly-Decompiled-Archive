using JetBrains.Annotations;

namespace TaleWorlds.MountAndBlade.ListedServer;

public static class ListedServerCommandManager
{
	public static ServerSideIntermissionManager ServerSideIntermissionManager { get; private set; }

	public static void Initialize(ServerSideIntermissionManager serverSideIntermissionManager)
	{
		ServerSideIntermissionManager = serverSideIntermissionManager;
	}

	[UsedImplicitly]
	[ConsoleCommandMethod("start_game_and_mission", "Start the server and the mission with the current configuration.")]
	private static void StartGameAndMission()
	{
		ServerSideIntermissionManager.StartGameAndMission();
	}

	[UsedImplicitly]
	[ConsoleCommandMethod("end_game_after_mission_is_over", "Automatically shuts the server after the mission is over.")]
	private static void EndGameAfterMissionIsOver()
	{
		ServerSideIntermissionManager.EndGameAfterMissionIsOver();
	}

	[UsedImplicitly]
	[ConsoleCommandMethod("set_port", "Sets the port which will be used by start_game command.")]
	private static void SetPort(string portAsString)
	{
		if (int.TryParse(portAsString, out var result))
		{
			ServerSideIntermissionManager.SetPort(result);
		}
	}

	[UsedImplicitly]
	[ConsoleCommandMethod("start_game", "Start the server with the current configuration.")]
	private static void StartGame()
	{
		ServerSideIntermissionManager.StartGame();
	}

	[UsedImplicitly]
	[ConsoleCommandMethod("start_mission", "Start the mission with the current configuration.")]
	private static void StartMission()
	{
		ServerSideIntermissionManager.StartMission();
	}

	[UsedImplicitly]
	[ConsoleCommandMethod("end_mission", "Start the mission with the current configuration.")]
	private static void EndMission()
	{
		ServerSideIntermissionManager.EndMission();
	}

	[UsedImplicitly]
	[ConsoleCommandMethod("say", "Sends a message to all players on the server, formatted as a server message.")]
	private static void Say(string message)
	{
		ServerSideIntermissionManager.Say(message);
	}

	[UsedImplicitly]
	[ConsoleCommandMethod("enable_automated_battle_switching", "Enables automated battle switching and shuts the server after battles are finished.")]
	private static void EnableAutomatedBattleSwitching()
	{
		ServerSideIntermissionManager.EnableAutomatedBattleSwitching();
	}

	[UsedImplicitly]
	[ConsoleCommandMethod("add_map_to_automated_battle_pool", "Adds a map to automated battle pool.")]
	private static void AddMapToAutomatedBattlePool(string map)
	{
		ServerSideIntermissionManager.AddMapToAutomatedBattlePool(map);
	}

	[UsedImplicitly]
	[ConsoleCommandMethod("add_map_to_usable_maps", "Adds a usable map to the server.")]
	private static void AddMapToUsableMaps(string usable_map_add_command)
	{
		ServerSideIntermissionManager.AddMapToUsableMaps(usable_map_add_command);
	}

	[UsedImplicitly]
	[ConsoleCommandMethod("enable_map_voting", "Enables voting for maps in intermission.")]
	private static void EnableMapVoting()
	{
		ServerSideIntermissionManager.SetIntermissionMapVoting(isEnabled: true);
	}

	[UsedImplicitly]
	[ConsoleCommandMethod("disable_map_voting", "Disables voting for maps in intermission.")]
	private static void DisableMapVoting()
	{
		ServerSideIntermissionManager.SetIntermissionMapVoting(isEnabled: false);
	}

	[UsedImplicitly]
	[ConsoleCommandMethod("enable_culture_voting", "Enables voting for cultures in intermission.")]
	private static void EnableCultureVoting()
	{
		ServerSideIntermissionManager.SetIntermissionCultureVoting(isEnabled: true);
	}

	[UsedImplicitly]
	[ConsoleCommandMethod("disable_culture_voting", "Disables voting for cultures in intermission.")]
	private static void DisableCultureVoting()
	{
		ServerSideIntermissionManager.SetIntermissionCultureVoting(isEnabled: false);
	}

	[UsedImplicitly]
	[ConsoleCommandMethod("set_automated_battle_count", "Sets how many maps will be played for a single dedicated server instance.")]
	private static void SetAutomatedBattleCount(string number)
	{
		ServerSideIntermissionManager.SetAutomatedBattleCount(int.Parse(number));
	}
}
