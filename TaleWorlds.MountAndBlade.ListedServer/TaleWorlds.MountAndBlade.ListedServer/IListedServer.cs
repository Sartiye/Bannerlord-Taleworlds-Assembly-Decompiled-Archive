namespace TaleWorlds.MountAndBlade.ListedServer;

public interface IListedServer
{
	bool Finished { get; }

	bool IsRegistered { get; }

	bool IsPlaying { get; }

	bool Connected { get; }

	string ServerTypeName { get; }
}
