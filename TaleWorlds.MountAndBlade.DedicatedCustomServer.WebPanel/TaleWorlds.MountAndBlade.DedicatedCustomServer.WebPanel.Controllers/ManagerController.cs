using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaleWorlds.MountAndBlade.Diamond;
using TaleWorlds.MountAndBlade.ListedServer;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.MountAndBlade.DedicatedCustomServer.WebPanel.Controllers;

[Authorize]
public class ManagerController : Controller
{
	public class OptionVM
	{
		public string Name { get; set; }

		public string LongName { get; set; }

		public string TypeName { get; set; }

		public IEnumerable<string> MultipleSelectionValues { get; set; }

		public float NumericMinValue { get; set; }

		public float NumericMaxValue { get; set; }

		public string DefaultValue { get; set; }
	}

	public class PlayerVM
	{
		public string Id { get; set; }

		public string Username { get; set; }

		public bool IsSynchronized { get; set; }

		public bool IsConnectionActive { get; set; }
	}

	public class ServerStatusVM
	{
		public string StatusDescription { get; set; }

		public string AutomatedBattleStateDescription { get; set; }
	}

	public class RunningServerInfoVM
	{
		public string Name { get; set; }

		public string GameMode { get; set; }

		public string MapName { get; set; }

		public string PlayerCount { get; set; }

		public string AutomatedBattleCount { get; set; }
	}

	private static readonly Regex SafeAsciiPattern = new Regex("^[\\x20-\\x7E]+$", RegexOptions.Compiled);

	private const int MaxInputLength = 512;

	private const string SkirmishGameTypeID = "Skirmish";

	private const string CaptainGameTypeID = "Captain";

	private const string DuelGameTypeID = "Duel";

	private const string TeamDeathmatchGameTypeID = "TeamDeathmatch";

	private const string SiegeGameTypeID = "Siege";

	private const string BattleGameTypeID = "Battle";

	private static bool IsValidAsciiInput(string input, int maxLength = 512)
	{
		if (string.IsNullOrEmpty(input))
		{
			return false;
		}
		if (input.Length > maxLength)
		{
			return false;
		}
		if (!SafeAsciiPattern.IsMatch(input))
		{
			return false;
		}
		return true;
	}

	public IActionResult Index()
	{
		return View();
	}

	private string GetOptionLongName(MultiplayerOptions.OptionType type)
	{
		return type switch
		{
			MultiplayerOptions.OptionType.PremadeMatchGameMode => "Game Mode", 
			MultiplayerOptions.OptionType.ServerName => "Server Name", 
			MultiplayerOptions.OptionType.GamePassword => "Password", 
			MultiplayerOptions.OptionType.Map => "First Team's Culture", 
			MultiplayerOptions.OptionType.CultureTeam1 => "Second Team's Culture", 
			MultiplayerOptions.OptionType.PremadeGameType => "Map", 
			MultiplayerOptions.OptionType.WarmupTimeLimitInSeconds => "Time Limit", 
			MultiplayerOptions.OptionType.GameDefinitionId => "Allow Polls to Kick Players", 
			MultiplayerOptions.OptionType.CultureTeam2 => "Server Capacity", 
			MultiplayerOptions.OptionType.SpectatorCamera => "Warmup Duration", 
			MultiplayerOptions.OptionType.MapTimeLimit => "Round Duration", 
			MultiplayerOptions.OptionType.RoundTimeLimit => "Round Preparation Duration", 
			MultiplayerOptions.OptionType.RoundPreparationTimeLimit => "Maximum Number of Rounds", 
			MultiplayerOptions.OptionType.RoundTotal => "First Team's Respawn Period", 
			MultiplayerOptions.OptionType.RespawnPeriodTeam1 => "Second Team's Respawn Period", 
			MultiplayerOptions.OptionType.UnlimitedGold => "First Team's Gold Gain Change Percentage", 
			MultiplayerOptions.OptionType.GoldGainChangePercentageTeam1 => "Second Team's Gold Gain Change Percentage", 
			MultiplayerOptions.OptionType.MinScoreToWinMatch => "Minimum Score to Win Duel", 
			MultiplayerOptions.OptionType.EnableMissionRecording => "Has Single Spawn", 
			_ => type.ToString(), 
		};
	}

	private OptionVM GetOptionVM(MultiplayerOptions.OptionType optionType)
	{
		MultiplayerOptionsProperty optionProperty = optionType.GetOptionProperty();
		if (optionProperty != null)
		{
			OptionVM optionVM = new OptionVM
			{
				Name = optionType.ToString(),
				LongName = GetOptionLongName(optionType)
			};
			if (optionProperty.OptionValueType == MultiplayerOptions.OptionValueType.Enum || (optionProperty.OptionValueType == MultiplayerOptions.OptionValueType.String && optionProperty.HasMultipleSelections))
			{
				optionVM.TypeName = "MultipleSelection";
				optionVM.MultipleSelectionValues = MultiplayerOptions.Instance.GetMultiplayerOptionsList(optionType);
				optionVM.DefaultValue = optionType.GetStrValue();
			}
			else if (optionProperty.OptionValueType == MultiplayerOptions.OptionValueType.String)
			{
				optionVM.TypeName = "Input";
				optionVM.DefaultValue = optionType.GetStrValue();
			}
			else if (optionProperty.OptionValueType == MultiplayerOptions.OptionValueType.Bool)
			{
				optionVM.TypeName = "Toggle";
				optionVM.DefaultValue = optionType.GetBoolValue().ToString();
			}
			else if (optionProperty.OptionValueType == MultiplayerOptions.OptionValueType.Integer)
			{
				optionVM.TypeName = "Number";
				optionVM.NumericMinValue = optionProperty.BoundsMin;
				optionVM.NumericMaxValue = optionProperty.BoundsMax;
				optionVM.DefaultValue = optionType.GetIntValue().ToString();
			}
			return optionVM;
		}
		return null;
	}

	[HttpGet("[controller]/get_options")]
	public IEnumerable<OptionVM> GetOptions()
	{
		if (Module.CurrentModule == null || DedicatedCustomServerSubModule.Instance == null)
		{
			return Enumerable.Empty<OptionVM>();
		}
		if (!DedicatedCustomServerSubModule.Instance.DedicatedCustomGameServer.Connected)
		{
			return Enumerable.Empty<OptionVM>();
		}
		List<OptionVM> list = new List<OptionVM>();
		if (MultiplayerOptions.Instance != null)
		{
			list.Add(GetOptionVM(MultiplayerOptions.OptionType.PremadeMatchGameMode));
			list.Add(GetOptionVM(MultiplayerOptions.OptionType.ServerName));
			list.Add(GetOptionVM(MultiplayerOptions.OptionType.GamePassword));
			list.Add(GetOptionVM(MultiplayerOptions.OptionType.Map));
			list.Add(GetOptionVM(MultiplayerOptions.OptionType.CultureTeam1));
			list.Add(GetOptionVM(MultiplayerOptions.OptionType.PremadeGameType));
			list.Add(GetOptionVM(MultiplayerOptions.OptionType.WarmupTimeLimitInSeconds));
			list.Add(GetOptionVM(MultiplayerOptions.OptionType.CultureTeam2));
			string strValue = MultiplayerOptions.OptionType.PremadeMatchGameMode.GetStrValue();
			switch (strValue)
			{
			case "Skirmish":
			case "Captain":
			case "Battle":
			case "Siege":
				list.Add(GetOptionVM(MultiplayerOptions.OptionType.SpectatorCamera));
				break;
			}
			switch (strValue)
			{
			case "Skirmish":
			case "Captain":
			case "Battle":
				list.Add(GetOptionVM(MultiplayerOptions.OptionType.MapTimeLimit));
				list.Add(GetOptionVM(MultiplayerOptions.OptionType.RoundTimeLimit));
				list.Add(GetOptionVM(MultiplayerOptions.OptionType.RoundPreparationTimeLimit));
				break;
			}
			if (strValue == "TeamDeathmatch" || strValue == "Siege")
			{
				list.Add(GetOptionVM(MultiplayerOptions.OptionType.RoundTotal));
				list.Add(GetOptionVM(MultiplayerOptions.OptionType.RespawnPeriodTeam1));
				list.Add(GetOptionVM(MultiplayerOptions.OptionType.UnlimitedGold));
				list.Add(GetOptionVM(MultiplayerOptions.OptionType.GoldGainChangePercentageTeam1));
			}
			if (strValue == "Duel")
			{
				list.Add(GetOptionVM(MultiplayerOptions.OptionType.MinScoreToWinMatch));
			}
			if (strValue != "Duel")
			{
				list.Add(GetOptionVM(MultiplayerOptions.OptionType.EnableMissionRecording));
			}
		}
		return list.Where((OptionVM o) => o != null);
	}

	[HttpPost("[controller]/set_options")]
	public IActionResult SetOptions([FromBody] Dictionary<string, string> optionLookup)
	{
		if (Module.CurrentModule == null)
		{
			return Ok();
		}
		foreach (KeyValuePair<string, string> item in optionLookup)
		{
			if (!Enum.TryParse(typeof(MultiplayerOptions.OptionType), item.Key, out object result))
			{
				continue;
			}
			MultiplayerOptions.OptionType optionType = (MultiplayerOptions.OptionType)result;
			MultiplayerOptionsProperty optionProperty = optionType.GetOptionProperty();
			if (optionProperty == null)
			{
				continue;
			}
			float result3;
			if (optionProperty.OptionValueType == MultiplayerOptions.OptionValueType.String)
			{
				MultiplayerOptions.Instance.GetOptionFromOptionType(optionType).UpdateValue(item.Value);
			}
			else if (optionProperty.OptionValueType == MultiplayerOptions.OptionValueType.Bool)
			{
				if (bool.TryParse(item.Value, out var result2))
				{
					MultiplayerOptions.Instance.GetOptionFromOptionType(optionType).UpdateValue(result2);
				}
			}
			else if (optionProperty.OptionValueType == MultiplayerOptions.OptionValueType.Integer && float.TryParse(item.Value, out result3))
			{
				MultiplayerOptions.Instance.GetOptionFromOptionType(optionType).UpdateValue((int)result3);
			}
		}
		return Ok();
	}

	[HttpGet("[controller]/get_current_server_state")]
	public ServerStatusVM GetCurrentServerState()
	{
		if (DedicatedCustomServerSubModule.Instance != null && DedicatedCustomServerSubModule.Instance.DedicatedCustomGameServer != null)
		{
			CustomBattleServer dedicatedCustomGameServer = DedicatedCustomServerSubModule.Instance.DedicatedCustomGameServer;
			_ = string.Empty;
			if (dedicatedCustomGameServer.CurrentState == CustomBattleServer.State.RegisteredGame || dedicatedCustomGameServer.CurrentState == CustomBattleServer.State.RegisteredServer)
			{
				MultiplayerOptions.OptionType.ServerName.GetStrValue();
			}
			if (dedicatedCustomGameServer.CurrentState == CustomBattleServer.State.RegisteredGame)
			{
				GameNetwork.NetworkPeers.Where((NetworkCommunicator p) => p != null && !p.IsServerPeer).Count();
			}
			return new ServerStatusVM
			{
				StatusDescription = DedicatedCustomServerSubModule.Instance.DedicatedCustomGameServer.CurrentState.ToString(),
				AutomatedBattleStateDescription = ServerSideIntermissionManager.Instance.AutomatedBattleState.ToString()
			};
		}
		return new ServerStatusVM
		{
			StatusDescription = "Unavailable",
			AutomatedBattleStateDescription = string.Empty
		};
	}

	[HttpGet("[controller]/get_running_server_info")]
	public RunningServerInfoVM GetRunningServerInfo()
	{
		RunningServerInfoVM runningServerInfoVM = new RunningServerInfoVM
		{
			Name = string.Empty,
			GameMode = string.Empty,
			MapName = string.Empty,
			PlayerCount = string.Empty,
			AutomatedBattleCount = string.Empty
		};
		if (DedicatedCustomServerSubModule.Instance != null)
		{
			runningServerInfoVM.Name = MultiplayerOptions.OptionType.ServerName.GetStrValue();
			runningServerInfoVM.GameMode = MultiplayerOptions.OptionType.PremadeMatchGameMode.GetStrValue();
			runningServerInfoVM.MapName = MultiplayerOptions.OptionType.PremadeGameType.GetStrValue();
			int value = GameNetwork.NetworkPeers?.Where((NetworkCommunicator p) => !p.IsServerPeer).Count() ?? 0;
			int intValue = MultiplayerOptions.OptionType.CultureTeam2.GetIntValue();
			runningServerInfoVM.PlayerCount = $"{value} / {intValue}";
			int value2 = ServerSideIntermissionManager.Instance.AutomatedBattleCount - ServerSideIntermissionManager.Instance.RemainedAutomatedBattleCount;
			int automatedBattleCount = ServerSideIntermissionManager.Instance.AutomatedBattleCount;
			runningServerInfoVM.AutomatedBattleCount = $"{value2} / {automatedBattleCount}";
		}
		return runningServerInfoVM;
	}

	[HttpGet("[controller]/getplayers")]
	public IEnumerable<PlayerVM> GetPlayers()
	{
		List<PlayerVM> list = new List<PlayerVM>();
		if (GameNetwork.NetworkPeers != null)
		{
			foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
			{
				PlayerVM item = new PlayerVM
				{
					Id = networkPeer.VirtualPlayer.Id.ToString(),
					Username = networkPeer.VirtualPlayer.UserName,
					IsSynchronized = networkPeer.IsSynchronized,
					IsConnectionActive = networkPeer.IsConnectionActive
				};
				list.Add(item);
			}
		}
		return list;
	}

	[HttpGet("[controller]/kick_player/{playerId}/{ban}")]
	public IActionResult KickPlayer(string playerId, bool ban)
	{
		if (!IsValidAsciiInput(playerId, 128))
		{
			return BadRequest("Invalid player ID");
		}
		try
		{
			if (DedicatedCustomServerSubModule.Instance != null)
			{
				DedicatedCustomServerSubModule.Instance.DedicatedCustomGameServer.KickPlayer(PlayerId.FromString(playerId), ban);
				return Ok();
			}
			return NotFound();
		}
		catch (Exception ex)
		{
			Console.WriteLine("[WebPanel] Error in KickPlayer: " + ex.Message);
			return BadRequest("Invalid player ID format");
		}
	}

	[HttpGet("[controller]/get_chat_messages")]
	public IEnumerable<string> GetChatMessages()
	{
		List<string> result = new List<string>();
		if (DedicatedCustomServerWebPanelSubModule.Instance != null)
		{
			result = DedicatedCustomServerWebPanelSubModule.Instance.FlushMessageQueue();
		}
		return result;
	}

	[HttpGet("[controller]/start_game_and_mission")]
	public IActionResult StartGameAndMission()
	{
		if (ServerSideIntermissionManager.Instance != null)
		{
			ServerSideIntermissionManager.Instance.StartGameAndMission();
		}
		return Ok();
	}

	[HttpGet("[controller]/end_game_after_mission_is_over")]
	public IActionResult EndGameAfterMissionIsOver()
	{
		if (ServerSideIntermissionManager.Instance != null)
		{
			ServerSideIntermissionManager.Instance.EndGameAfterMissionIsOver();
		}
		return Ok();
	}

	[HttpGet("[controller]/set_port/{portAsString}")]
	public IActionResult SetPort(string portAsString)
	{
		if (int.TryParse(portAsString, out var result))
		{
			ServerSideIntermissionManager.Instance.SetPort(result);
			return Ok();
		}
		return BadRequest();
	}

	[HttpGet("[controller]/start_game")]
	public IActionResult StartGame()
	{
		if (ServerSideIntermissionManager.Instance != null)
		{
			ServerSideIntermissionManager.Instance.StartGame();
		}
		return Ok();
	}

	[HttpGet("[controller]/start_mission")]
	public IActionResult StartMission()
	{
		if (ServerSideIntermissionManager.Instance != null)
		{
			ServerSideIntermissionManager.Instance.StartMission();
		}
		return Ok();
	}

	[HttpGet("[controller]/end_mission")]
	public IActionResult EndMission()
	{
		if (ServerSideIntermissionManager.Instance != null)
		{
			ServerSideIntermissionManager.Instance.EndMission();
		}
		return Ok();
	}

	[HttpGet("[controller]/say/{message}")]
	public IActionResult Say(string message)
	{
		if (!IsValidAsciiInput(message))
		{
			return BadRequest("Invalid message: only printable ASCII characters are allowed");
		}
		if (ServerSideIntermissionManager.Instance != null)
		{
			ServerSideIntermissionManager.Instance.Say(message);
		}
		return Ok();
	}

	[HttpGet("[controller]/enable_automated_battle_switching")]
	public IActionResult EnableAutomatedBattleSwitching()
	{
		if (ServerSideIntermissionManager.Instance != null)
		{
			ServerSideIntermissionManager.Instance.EnableAutomatedBattleSwitching();
		}
		return Ok();
	}

	[HttpGet("[controller]/add_map_to_automated_battle_pool/{map}")]
	public IActionResult AddMapToAutomatedBattlePool(string map)
	{
		if (!IsValidAsciiInput(map, 256))
		{
			return BadRequest("Invalid map name");
		}
		if (ServerSideIntermissionManager.Instance != null)
		{
			ServerSideIntermissionManager.Instance.AddMapToAutomatedBattlePool(map);
		}
		return Ok();
	}

	[HttpGet("[controller]/enable_map_voting")]
	public IActionResult EnableMapVoting()
	{
		if (ServerSideIntermissionManager.Instance != null)
		{
			ServerSideIntermissionManager.Instance.SetIntermissionMapVoting(isEnabled: true);
		}
		return Ok();
	}

	[HttpGet("[controller]/disable_map_voting")]
	public IActionResult DisableMapVoting()
	{
		if (ServerSideIntermissionManager.Instance != null)
		{
			ServerSideIntermissionManager.Instance.SetIntermissionMapVoting(isEnabled: false);
		}
		return Ok();
	}

	[HttpGet("[controller]/enable_culture_voting")]
	public IActionResult EnableCultureVoting()
	{
		if (ServerSideIntermissionManager.Instance != null)
		{
			ServerSideIntermissionManager.Instance.SetIntermissionCultureVoting(isEnabled: true);
		}
		return Ok();
	}

	[HttpGet("[controller]/disable_culture_voting")]
	public IActionResult DisableCultureVoting()
	{
		if (ServerSideIntermissionManager.Instance != null)
		{
			ServerSideIntermissionManager.Instance.SetIntermissionCultureVoting(isEnabled: false);
		}
		return Ok();
	}

	[HttpGet("[controller]/set_automated_battle_count/{number}")]
	public IActionResult SetAutomatedBattleCount(string number)
	{
		if (int.TryParse(number, out var result))
		{
			if (ServerSideIntermissionManager.Instance != null)
			{
				ServerSideIntermissionManager.Instance.SetAutomatedBattleCount(result);
			}
			return Ok();
		}
		return BadRequest();
	}

	[HttpGet("[controller]/set_server_name/{name}")]
	public IActionResult SetServerName(string name)
	{
		if (!IsValidAsciiInput(name, 256))
		{
			return BadRequest("Invalid server name: only printable ASCII characters are allowed");
		}
		if (ServerSideIntermissionManager.Instance != null)
		{
			ServerSideIntermissionManager.Instance.SetServerName(name);
		}
		return Ok();
	}
}
