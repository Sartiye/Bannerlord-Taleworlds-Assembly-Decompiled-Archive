using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;

namespace Messages.FromClient.ToLobbyServer;

[Serializable]
[MessageDescription("Client", "LobbyServer", true)]
public class Test_CreateChatRoomMessage : Message
{
	[JsonProperty]
	public string Name { get; private set; }

	public Test_CreateChatRoomMessage()
	{
	}

	public Test_CreateChatRoomMessage(string name)
	{
		Name = name;
	}
}
