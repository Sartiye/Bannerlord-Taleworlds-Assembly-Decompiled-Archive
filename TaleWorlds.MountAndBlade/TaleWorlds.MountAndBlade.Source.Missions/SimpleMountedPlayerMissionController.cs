using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.Source.Missions;

public class SimpleMountedPlayerMissionController : MissionLogic
{
	private readonly Game _game = Game.Current;

	public override void AfterStart()
	{
		BasicCharacterObject @object = _game.ObjectManager.GetObject<BasicCharacterObject>("aserai_tribal_horseman");
		WeakGameEntity weakGameEntity = Mission.Current.Scene.FindWeakEntityWithTag("sp_play");
		MatrixFrame matrixFrame = (weakGameEntity.IsValid ? weakGameEntity.GetGlobalFrame() : MatrixFrame.Identity);
		AgentBuildData agentBuildData = new AgentBuildData(new BasicBattleAgentOrigin(@object));
		AgentBuildData agentBuildData2 = agentBuildData.InitialPosition(in matrixFrame.origin);
		Vec2 direction = matrixFrame.rotation.f.AsVec2.Normalized();
		agentBuildData2.InitialDirection(in direction).Controller(AgentControllerType.Player);
		base.Mission.SpawnAgent(agentBuildData).WieldInitialWeapons();
	}

	public override bool MissionEnded(ref MissionResult missionResult)
	{
		return base.Mission.InputManager.IsGameKeyPressed(4);
	}
}
