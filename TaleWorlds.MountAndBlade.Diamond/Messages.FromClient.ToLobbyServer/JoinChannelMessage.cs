using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;
using TaleWorlds.MountAndBlade.Diamond;

namespace Messages.FromClient.ToLobbyServer;

[Serializable]
[MessageDescription("Client", "LobbyServer", true)]
public class JoinChannelMessage : Message
{
	[JsonProperty]
	public ChatChannelType Channel { get; private set; }

	public JoinChannelMessage()
	{
	}

	public JoinChannelMessage(ChatChannelType channel)
	{
		Channel = channel;
	}
}
