using System.Threading.Tasks;

namespace TaleWorlds.MountAndBlade.ListedServer;

public interface IServerSideIntermissionManagerHandler
{
	IListedServer Server { get; }

	void OnBeforeStartingNextBattle();

	void OnGameParametersChanged(string gameType, string mapID, string uniqueMapIdentifierString);

	void OnShutDownAfterMission();

	void OnEarlyTick(float dt);

	void OnTick(float dt);

	Task OnGameStart(string selectedGameType, string selectedScene, string uniqueSceneId, int gameDefinitionId, string gameModule, int portToUse, string regionToUse);
}
