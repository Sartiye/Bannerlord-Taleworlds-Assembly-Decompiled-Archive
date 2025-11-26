using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;
using TaleWorlds.MountAndBlade.Diamond;

namespace Messages.FromClient.ToLobbyServer;

[Serializable]
[MessageDescription("Client", "LobbyServer", true)]
public class ChannelMessage : Message
{
	[JsonProperty]
	public ChatChannelType Channel { get; private set; }

	[JsonProperty]
	public string Message { get; private set; }

	public ChannelMessage()
	{
	}

	public ChannelMessage(ChatChannelType channel, string message)
	{
		Channel = channel;
		Message = message;
	}
}
