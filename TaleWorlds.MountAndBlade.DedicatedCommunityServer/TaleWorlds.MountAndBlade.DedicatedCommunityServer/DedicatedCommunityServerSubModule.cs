using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade.ListedServer;

namespace TaleWorlds.MountAndBlade.DedicatedCommunityServer;

public class DedicatedCommunityServerSubModule : MBSubModuleBase
{
	private IWebHost _host;

	private ChatBox _chatBox;

	private Queue<string> _queuedTextMessages;

	public static DedicatedCommunityServerSubModule Instance { get; private set; }

	public ServerSideIntermissionManager ServerSideIntermissionManager { get; private set; }

	public int Port => Module.CurrentModule.StartupInfo.ServerPort;

	protected override void OnSubModuleLoad()
	{
		base.OnSubModuleLoad();
		Instance = this;
		_queuedTextMessages = new Queue<string>();
		InitialListedGameServerState.OnActivated += DedicatedCustomGameServerStateActivated;
		CommunityServerIntermissionManagerHandler handler = new CommunityServerIntermissionManagerHandler();
		MissionLobbyComponent.AddLobbyComponentType(typeof(MissionCommunityServerComponent), LobbyMissionType.Community, isSeverComponent: true);
		ServerSideIntermissionManager = ServerSideIntermissionManager.Instantiate(handler);
		ListedServerCommandManager.Initialize(ServerSideIntermissionManager);
	}

	protected override void OnSubModuleUnloaded()
	{
		base.OnSubModuleUnloaded();
		InitialListedGameServerState.OnActivated -= DedicatedCustomGameServerStateActivated;
		ChatBox chatBox = _chatBox;
		chatBox.OnMessageReceivedAtDedicatedServer = (Action<NetworkCommunicator, string>)Delegate.Remove(chatBox.OnMessageReceivedAtDedicatedServer, new Action<NetworkCommunicator, string>(OnMessageReceivedAtDedicatedServer));
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
		string value;
		using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.IP))
		{
			socket.Connect("8.8.8.8", 65530);
			value = (socket.LocalEndPoint as IPEndPoint).Address.ToString();
		}
		_host = WebHost.CreateDefaultBuilder().ConfigureLogging(delegate(ILoggingBuilder logging)
		{
			logging.ClearProviders();
		}).UseStartup<Startup>()
			.UseUrls($"http://*:{Port}")
			.Build();
		Console.ForegroundColor = ConsoleColor.Green;
		Console.WriteLine($"Dedicated Community Server Dashboard is live at {value}:{Port}!");
		Console.WriteLine($"Dashboard can be accessed from outside if server's TCP Port {Port} is open.");
		Console.ResetColor();
		Task.Run(delegate
		{
			_host.Run();
		});
	}

	private void OnMessageReceivedAtDedicatedServer(NetworkCommunicator fromPeer, string message)
	{
		lock (_queuedTextMessages)
		{
			_queuedTextMessages.Enqueue(fromPeer.UserName + ": " + message);
		}
	}

	protected override void OnApplicationTick(float dt)
	{
		base.OnApplicationTick(dt);
		ServerSideIntermissionManager.Tick(dt);
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
