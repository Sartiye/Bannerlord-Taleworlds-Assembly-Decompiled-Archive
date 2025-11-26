using System.Collections.Generic;
using TaleWorlds.Core;

namespace TaleWorlds.MountAndBlade;

public interface IMissionAgentSpawnLogic : IMissionBehavior
{
	void StartSpawner(BattleSideEnum side);

	void StopSpawner(BattleSideEnum side);

	bool IsSideSpawnEnabled(BattleSideEnum side);

	bool IsSideDepleted(BattleSideEnum side);

	float GetReinforcementInterval();

	IEnumerable<IAgentOriginBase> GetAllTroopsForSide(BattleSideEnum side);

	bool GetSpawnHorses(BattleSideEnum side);

	int GetNumberOfPlayerControllableTroops();
}
