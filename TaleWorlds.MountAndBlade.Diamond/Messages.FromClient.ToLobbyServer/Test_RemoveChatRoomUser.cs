using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;

namespace Messages.FromClient.ToLobbyServer;

[Serializable]
[MessageDescription("Client", "LobbyServer", true)]
public class Test_RemoveChatRoomUser : Message
{
	[JsonProperty]
	public string Name { get; private set; }

	public Test_RemoveChatRoomUser()
	{
	}

	public Test_RemoveChatRoomUser(string name)
	{
		Name = name;
	}
}
