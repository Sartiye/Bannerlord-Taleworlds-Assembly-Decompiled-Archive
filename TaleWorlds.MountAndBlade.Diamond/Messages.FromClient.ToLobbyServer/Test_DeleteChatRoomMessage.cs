using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;

namespace Messages.FromClient.ToLobbyServer;

[Serializable]
[MessageDescription("Client", "LobbyServer", true)]
public class Test_DeleteChatRoomMessage : Message
{
	[JsonProperty]
	public Guid ChatRoomId { get; private set; }

	public Test_DeleteChatRoomMessage()
	{
	}

	public Test_DeleteChatRoomMessage(Guid chatRoomId)
	{
		ChatRoomId = chatRoomId;
	}
}
