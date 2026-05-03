using System;
using TaleWorlds.MountAndBlade.ListedServer;

namespace TaleWorlds.MountAndBlade.DedicatedCommunityServer;

public class CommunityServer : IListedServer
{
	public Guid Id { get; set; }

	public bool Finished { get; set; }

	public bool IsRegistered { get; set; }

	public bool IsListed { get; set; }

	public bool IsPlaying { get; set; }

	public bool Connected { get; set; }

	public int Port { get; set; }

	string IListedServer.ServerTypeName => "(Community Server)";
}
