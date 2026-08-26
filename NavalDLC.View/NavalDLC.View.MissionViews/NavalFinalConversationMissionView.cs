using System.Linq;
using NavalDLC.Storyline;
using SandBox.Conversation.MissionLogics;
using StoryMode.StoryModeObjects;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace NavalDLC.View.MissionViews;

public class NavalFinalConversationMissionView : MissionView
{
	private const float FadeDuration = 0.5f;

	private CharacterObject _currentConversationCharacter;

	private float _remainingSisterSpawnTime = 0.6f;

	private bool _shouldSpawnSister;

	private bool _shouldStartSisterConversation;

	public override void OnMissionTick(float dt)
	{
		_currentConversationCharacter = Campaign.Current.ConversationManager.OneToOneConversationCharacter;
		if (_shouldStartSisterConversation && !ScreenFadeController.IsFadeActive)
		{
			Agent agent = Mission.Current.Agents.FirstOrDefault((Agent x) => x.Character == StoryModeHeroes.LittleSister.CharacterObject);
			Mission.Current.GetMissionBehavior<MissionConversationLogic>()?.StartConversation(agent, setActionsInstantly: false);
			_shouldStartSisterConversation = false;
		}
		if (_shouldSpawnSister && _remainingSisterSpawnTime > 0f)
		{
			_remainingSisterSpawnTime -= dt;
			if (_remainingSisterSpawnTime <= 0f)
			{
				TransitionToSister();
				_shouldSpawnSister = false;
			}
		}
	}

	public override void OnConversationEnd()
	{
		if (_currentConversationCharacter == NavalStorylineData.Gunnar.CharacterObject)
		{
			ScreenFadeController.BeginFadeOutAndIn();
			_shouldSpawnSister = true;
		}
	}

	private void TransitionToSister()
	{
		AgentBuildData agentBuildData = new AgentBuildData(StoryModeHeroes.LittleSister.CharacterObject);
		agentBuildData.TroopOrigin(new SimpleAgentOrigin(agentBuildData.AgentCharacter));
		Agent? agent = Mission.Current.Agents.FirstOrDefault((Agent x) => x.Character == NavalStorylineData.Gunnar.CharacterObject);
		Vec3 position = agent.Position;
		agentBuildData.InitialPosition(in position);
		Vec2 direction = -Agent.Main.LookDirection.AsVec2.Normalized();
		agentBuildData.InitialDirection(in direction);
		agentBuildData.NoHorses(noHorses: true);
		agentBuildData.CivilianEquipment(civilianEquipment: true);
		Mission.Current.SpawnAgent(agentBuildData);
		agent.FadeOut(hideInstantly: true, hideMount: true);
		_shouldStartSisterConversation = true;
	}
}
