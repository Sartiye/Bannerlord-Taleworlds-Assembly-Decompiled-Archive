using System.Collections.Generic;
using System.Linq;
using SandBox.Objects.AreaMarkers;
using SandBox.Objects.Usables;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace SandBox.Missions.MissionLogics;

public class StealthAreaMissionLogic : MissionLogic
{
	public delegate List<IAgentOriginBase> GetReinforcementAllyTroopsDelegate(StealthAreaData triggeredStealthAreaData, StealthAreaMarker stealthAreaMarker);

	public class StealthAreaData
	{
		internal bool IsStealthAreaTriggered;

		internal bool IsReinforcementCalled;

		internal readonly StealthAreaUsePoint StealthAreaUsePoint;

		internal readonly Dictionary<StealthAreaMarker, List<Agent>> StealthAreaMarkers;

		internal StealthAreaData(StealthAreaUsePoint stealthAreaUsePoint)
		{
			StealthAreaUsePoint = stealthAreaUsePoint;
			StealthAreaMarkers = new Dictionary<StealthAreaMarker, List<Agent>>();
			foreach (WeakGameEntity child in stealthAreaUsePoint.GameEntity.GetChildren())
			{
				if (child.HasScriptOfType<StealthAreaMarker>())
				{
					StealthAreaMarkers.Add(child.GetFirstScriptOfType<StealthAreaMarker>(), new List<Agent>());
				}
			}
		}

		internal void AddAgentToStealthAreaMarker(StealthAreaMarker stealthAreaMarker, Agent agent)
		{
			StealthAreaMarkers[stealthAreaMarker].Add(agent);
		}

		internal void RemoveAgentFromStealthAreaMarker(StealthAreaMarker stealthAreaMarker, Agent agent)
		{
			StealthAreaMarkers[stealthAreaMarker].Remove(agent);
			if (StealthAreaMarkers.All((KeyValuePair<StealthAreaMarker, List<Agent>> x) => x.Value.IsEmpty()))
			{
				StealthAreaUsePoint.EnableStealthAreaUsePoint();
				IsStealthAreaTriggered = true;
			}
		}
	}

	private readonly MBList<StealthAreaData> _stealthAreaData = new MBList<StealthAreaData>();

	private readonly Dictionary<string, Dictionary<string, int>> _agentSpawnTypes = new Dictionary<string, Dictionary<string, int>>();

	private readonly MBList<Agent> _allyTroops = new MBList<Agent>();

	public GetReinforcementAllyTroopsDelegate GetReinforcementAllyTroops;

	public MBReadOnlyList<Agent> AllyTroops => _allyTroops;

	public bool AllReinforcementsCalled { get; private set; }

	public StealthAreaMissionLogic()
	{
		SetAgentSpawnTypes();
	}

	public bool IsSentry(Agent agent)
	{
		foreach (StealthAreaData stealthAreaDatum in _stealthAreaData)
		{
			foreach (KeyValuePair<StealthAreaMarker, List<Agent>> stealthAreaMarker in stealthAreaDatum.StealthAreaMarkers)
			{
				if (stealthAreaMarker.Value.Contains(agent))
				{
					return true;
				}
			}
		}
		return false;
	}

	public override void OnBehaviorInitialize()
	{
		base.OnBehaviorInitialize();
		foreach (StealthAreaUsePoint item in base.Mission.MissionObjects.FindAllWithType<StealthAreaUsePoint>())
		{
			_stealthAreaData.Add(new StealthAreaData(item));
		}
	}

	public void AddAgentSpawnType(string spawnGroupId, Dictionary<string, int> spawnDictionary)
	{
		_agentSpawnTypes[spawnGroupId] = spawnDictionary;
	}

	private void SetAgentSpawnTypes()
	{
		Dictionary<string, int> dictionary = new Dictionary<string, int>();
		dictionary.Add("deserter", 1);
		dictionary.Add("forest_bandits_bandit", 2);
		_agentSpawnTypes.Add("reinforcement_ally_group_1", dictionary);
		Dictionary<string, int> dictionary2 = new Dictionary<string, int>();
		dictionary2.Add("aserai_footman", 3);
		dictionary2.Add("aserai_skirmisher", 2);
		_agentSpawnTypes.Add("reinforcement_ally_group_cambush", dictionary2);
	}

	private List<IAgentOriginBase> GetReinforcementAllyGroupTroops(StealthAreaData triggeredStealthAreaData, StealthAreaMarker stealthAreaMarker)
	{
		if (GetReinforcementAllyTroops == null)
		{
			string reinforcementAllyGroupId = stealthAreaMarker.ReinforcementAllyGroupId;
			List<IAgentOriginBase> list = new List<IAgentOriginBase>();
			if (_agentSpawnTypes.TryGetValue(reinforcementAllyGroupId, out var value))
			{
				foreach (KeyValuePair<string, int> item in value)
				{
					CharacterObject @object = MBObjectManager.Instance.GetObject<CharacterObject>(item.Key);
					int value2 = item.Value;
					for (int i = 0; i < value2; i++)
					{
						list.Add(new PartyAgentOrigin(PartyBase.MainParty, @object));
					}
				}
			}
			return list;
		}
		return GetReinforcementAllyTroops(triggeredStealthAreaData, stealthAreaMarker);
	}

	public override void OnAgentBuild(Agent agent, Banner banner)
	{
		base.OnAgentBuild(agent, banner);
		CheckStealthAreaMarkerForAgent(agent);
	}

	public override void OnAgentTeamChanged(Team prevTeam, Team newTeam, Agent agent)
	{
		base.OnAgentTeamChanged(prevTeam, newTeam, agent);
		CheckStealthAreaMarkerForAgent(agent);
	}

	private void CheckStealthAreaMarkerForAgent(Agent agent)
	{
		if (!agent.IsHuman || agent.Team != Mission.Current.PlayerEnemyTeam)
		{
			return;
		}
		foreach (StealthAreaData stealthAreaDatum in _stealthAreaData)
		{
			foreach (KeyValuePair<StealthAreaMarker, List<Agent>> stealthAreaMarker in stealthAreaDatum.StealthAreaMarkers)
			{
				if (stealthAreaMarker.Key.IsPositionInRange(agent.Position))
				{
					stealthAreaDatum.AddAgentToStealthAreaMarker(stealthAreaMarker.Key, agent);
					break;
				}
			}
		}
	}

	public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
	{
		if (affectorAgent == null || !affectorAgent.IsMainAgent)
		{
			return;
		}
		foreach (StealthAreaData stealthAreaDatum in _stealthAreaData)
		{
			foreach (KeyValuePair<StealthAreaMarker, List<Agent>> stealthAreaMarker in stealthAreaDatum.StealthAreaMarkers)
			{
				if (stealthAreaMarker.Value.Contains(affectedAgent))
				{
					stealthAreaDatum.RemoveAgentFromStealthAreaMarker(stealthAreaMarker.Key, affectedAgent);
				}
			}
		}
	}

	public override void OnObjectUsed(Agent userAgent, UsableMissionObject usedObject)
	{
		if (usedObject is StealthAreaUsePoint)
		{
			StealthAreaData stealthAreaData = null;
			foreach (StealthAreaData stealthAreaDatum in _stealthAreaData)
			{
				if (stealthAreaDatum.StealthAreaUsePoint == usedObject)
				{
					stealthAreaData = stealthAreaDatum;
					break;
				}
			}
			if (stealthAreaData != null)
			{
				stealthAreaData.IsReinforcementCalled = true;
				foreach (KeyValuePair<StealthAreaMarker, List<Agent>> stealthAreaMarker in stealthAreaData.StealthAreaMarkers)
				{
					List<IAgentOriginBase> reinforcementAllyGroupTroops = GetReinforcementAllyGroupTroops(stealthAreaData, stealthAreaMarker.Key);
					if (!reinforcementAllyGroupTroops.IsEmpty())
					{
						foreach (IAgentOriginBase item in reinforcementAllyGroupTroops)
						{
							SpawnAllyAgent(item, stealthAreaMarker.Key.ReinforcementAllyGroupSpawnPoint, stealthAreaMarker.Key.WaitPoint.GlobalPosition);
						}
					}
					else
					{
						Debug.FailedAssert("There is not any troops to spawn as stealth area reinforcement.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox\\Missions\\MissionLogics\\StealthAreaMissionLogic.cs", "OnObjectUsed", 269);
					}
				}
			}
		}
		AllReinforcementsCalled = _stealthAreaData.All((StealthAreaData x) => x.IsReinforcementCalled);
	}

	private void SpawnAllyAgent(IAgentOriginBase character, GameEntity spawnPoint, Vec3 position)
	{
		MatrixFrame globalFrame = spawnPoint.GetGlobalFrame();
		Agent agent = Mission.Current.SpawnTroop(character, isPlayerSide: true, hasFormation: false, spawnWithHorse: false, isReinforcement: false, 0, 0, isAlarmed: true, wieldInitialWeapons: true, forceDismounted: true, globalFrame.origin, globalFrame.rotation.f.AsVec2.Normalized());
		Vec3 randomPositionAroundPoint = Mission.Current.GetRandomPositionAroundPoint(position, 0f, 2f, nearFirst: true);
		WorldPosition position2 = new WorldPosition(spawnPoint.Scene, randomPositionAroundPoint);
		agent.SetScriptedPosition(ref position2, addHumanLikeDelay: true, Agent.AIScriptedFrameFlags.NoAttack | Agent.AIScriptedFrameFlags.Crouch);
		_allyTroops.Add(agent);
	}

	public bool CheckIfAllStealthAreasAreTriggered()
	{
		return _stealthAreaData.All((StealthAreaData x) => x.IsStealthAreaTriggered);
	}

	public bool CheckIfAllStealthAreasReinforcementsAreCalled()
	{
		return AllReinforcementsCalled;
	}
}
