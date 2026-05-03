using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade.ListedServer;

namespace TaleWorlds.MountAndBlade.DedicatedCustomServer.WebPanel;

public class DedicatedCustomServerWebPanelSubModule : MBSubModuleBase
{
	public static DedicatedCustomServerWebPanelSubModule Instance;

	private IWebHost _host;

	private ChatBox _chatBox;

	private Queue<string> _queuedTextMessages;

	public int Port => Module.CurrentModule.StartupInfo.ServerPort;

	protected override void OnSubModuleLoad()
	{
		Instance = this;
		base.OnSubModuleLoad();
		InitialListedGameServerState.OnActivated += DedicatedCustomGameServerStateActivated;
		_queuedTextMessages = new Queue<string>();
	}

	private void DedicatedCustomGameServerStateActivated()
	{
		if (Module.CurrentModule == null)
		{
			Console.WriteLine("Web panel can't be activated! No modules loaded.");
			return;
		}
		_chatBox = Game.Current.GetGameHandler<ChatBox>();
		ChatBox chatBox = _chatBox;
		chatBox.OnMessageReceivedAtDedicatedServer = (Action<NetworkCommunicator, string>)Delegate.Combine(chatBox.OnMessageReceivedAtDedicatedServer, new Action<NetworkCommunicator, string>(OnMessageReceivedAtDedicatedServer));
		_host = WebHost.CreateDefaultBuilder().ConfigureLogging(delegate(ILoggingBuilder logging)
		{
			logging.ClearProviders();
		}).UseStartup<Startup>()
			.UseUrls($"http://*:{Port}")
			.Build();
		Console.ForegroundColor = ConsoleColor.Green;
		Console.WriteLine($"Dedicated Custom Server Dashboard is live at port {Port}!");
		Console.WriteLine($"Dedicated Custom Server Dashboard can be accessed from outside via server's external IP address if server's TCP Port {Port} is open.");
		Console.ResetColor();
		Task.Run(delegate
		{
			_host.Run();
		});
	}

	protected override void OnSubModuleUnloaded()
	{
		base.OnSubModuleUnloaded();
		InitialListedGameServerState.OnActivated -= DedicatedCustomGameServerStateActivated;
		ChatBox chatBox = _chatBox;
		chatBox.OnMessageReceivedAtDedicatedServer = (Action<NetworkCommunicator, string>)Delegate.Remove(chatBox.OnMessageReceivedAtDedicatedServer, new Action<NetworkCommunicator, string>(OnMessageReceivedAtDedicatedServer));
	}

	private void OnMessageReceivedAtDedicatedServer(NetworkCommunicator fromPeer, string message)
	{
		lock (_queuedTextMessages)
		{
			_queuedTextMessages.Enqueue(fromPeer.UserName + ": " + message);
		}
	}

	public List<string> FlushMessageQueue()
	{
		List<string> list = new List<string>();
		lock (_queuedTextMessages)
		{
			string result;
			while (_queuedTextMessages.TryDequeue(out result))
			{
				list.Add(result);
			}
			return list;
		}
	}
}
