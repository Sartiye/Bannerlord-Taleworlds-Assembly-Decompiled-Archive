using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.ModuleManager;

namespace TaleWorlds.MountAndBlade.ListedServer;

public class InitialListedGameServerState : GameState
{
	private bool _isQuitting;

	private Stopwatch _stopwatch;

	private ServerSideIntermissionManager _serverSideIntermissionManager;

	private IListedServer _listedServer;

	public static event Action OnActivated;

	protected override void OnInitialize()
	{
		base.OnInitialize();
		_serverSideIntermissionManager = ServerSideIntermissionManager.Instance;
		_listedServer = _serverSideIntermissionManager.ListedServer;
		_serverSideIntermissionManager.OnBaseGameStateInitialize();
		string customGameServerConfigFile = Module.CurrentModule.StartupInfo.CustomGameServerConfigFile;
		if (customGameServerConfigFile != null)
		{
			Console.WriteLine("Executing commands from " + customGameServerConfigFile);
			string[] array = File.ReadAllLines(ModuleHelper.GetModuleFullPath("Native") + customGameServerConfigFile);
			List<string> list = new List<string>();
			List<string> list2 = new List<string>();
			for (int i = 0; i < array.Length; i++)
			{
				string text = array[i];
				if (text.IndexOf(' ') > 0)
				{
					text = text.Substring(0, text.IndexOf(' '));
				}
				if (MultiplayerOptions.TryGetOptionTypeFromString(text, out var _, out var _))
				{
					list.Add(array[i]);
				}
				else
				{
					list2.Add(array[i]);
				}
			}
			MultiplayerOptions.Instance.InitializeFromCommandList(list);
			if (!string.IsNullOrEmpty(Module.CurrentModule.StartupInfo.CustomGameServerNameOverride))
			{
				MultiplayerOptions.OptionType.ServerName.SetValue(Module.CurrentModule.StartupInfo.CustomGameServerNameOverride);
			}
			if (!string.IsNullOrEmpty(Module.CurrentModule.StartupInfo.CustomGameServerPasswordOverride))
			{
				MultiplayerOptions.OptionType.GamePassword.SetValue(Module.CurrentModule.StartupInfo.CustomGameServerPasswordOverride);
			}
			foreach (string item in list2)
			{
				GameNetwork.HandleConsoleCommand(item);
			}
			MultiplayerOptions.Instance.InitializeNextAndDefaultOptionContainers();
		}
		else
		{
			Console.WriteLine("Command file is null");
		}
	}

	protected override void OnActivate()
	{
		base.OnActivate();
		Console.WriteLine(_listedServer.ServerTypeName + " is ready! You can now enter console commands (Enter 'list' to inspect all options and commands).");
		InitialListedGameServerState.OnActivated?.Invoke();
		if (_listedServer.Finished)
		{
			_isQuitting = true;
			_stopwatch = new Stopwatch();
			_stopwatch.Start();
		}
	}

	protected override void OnTick(float dt)
	{
		base.OnTick(dt);
		if (_isQuitting && _stopwatch.ElapsedMilliseconds > 5000)
		{
			Utilities.QuitGame();
		}
	}
}
