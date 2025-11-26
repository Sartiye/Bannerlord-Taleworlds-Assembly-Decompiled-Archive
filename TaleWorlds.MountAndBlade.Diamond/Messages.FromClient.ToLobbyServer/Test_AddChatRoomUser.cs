using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;

namespace Messages.FromClient.ToLobbyServer;

[Serializable]
[MessageDescription("Client", "LobbyServer", true)]
public class Test_AddChatRoomUser : Message
{
	[JsonProperty]
	public string Name { get; private set; }

	public Test_AddChatRoomUser()
	{
	}

	public Test_AddChatRoomUser(string name)
	{
		Name = name;
	}
}
