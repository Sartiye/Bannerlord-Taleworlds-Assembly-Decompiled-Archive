using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class DefencePoint : ScriptComponentBehavior
{
	private List<Agent> defenders = new List<Agent>();

	public BattleSideEnum Side;

	public IEnumerable<Agent> Defenders => defenders;

	public void AddDefender(Agent defender)
	{
		defenders.Add(defender);
	}

	public bool RemoveDefender(Agent defender)
	{
		return defenders.Remove(defender);
	}

	public void PurgeInactiveDefenders()
	{
		foreach (Agent item in defenders.Where((Agent d) => !d.IsActive()).ToList())
		{
			RemoveDefender(item);
		}
	}

	private MatrixFrame GetPosition(int index)
	{
		MatrixFrame globalFrame = base.GameEntity.GetGlobalFrame();
		Vec3 f = globalFrame.rotation.f;
		f.Normalize();
		globalFrame.origin -= f * index * ManagedParameters.Instance.GetManagedParameter(ManagedParametersEnum.BipedalRadius) * 2f * 1.5f;
		return globalFrame;
	}

	public MatrixFrame GetVacantPosition(Agent a)
	{
		Mission current = Mission.Current;
		Team team = current.Teams.First((Team t) => t.Side == Side);
		for (int i = 0; i < 100; i++)
		{
			MatrixFrame position = GetPosition(i);
			Agent closestAllyAgent = current.GetClosestAllyAgent(team, position.origin, ManagedParameters.Instance.GetManagedParameter(ManagedParametersEnum.BipedalRadius));
			if (closestAllyAgent == null || closestAllyAgent == a)
			{
				return position;
			}
		}
		Debug.FailedAssert("Couldn't find a vacant position", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\Objects\\DefencePoint.cs", "GetVacantPosition", 73);
		return MatrixFrame.Identity;
	}

	public int CountOccupiedDefenderPositions()
	{
		Mission current = Mission.Current;
		Team team = current.Teams.First((Team t) => t.Side == Side);
		for (int i = 0; i < 100; i++)
		{
			if (current.GetClosestAllyAgent(team, GetPosition(i).origin, ManagedParameters.Instance.GetManagedParameter(ManagedParametersEnum.BipedalRadius)) == null)
			{
				return i;
			}
		}
		return 100;
	}
}
