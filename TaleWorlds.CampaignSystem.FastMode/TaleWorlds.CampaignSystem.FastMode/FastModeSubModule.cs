using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace TaleWorlds.CampaignSystem.FastMode;

public class FastModeSubModule : MBSubModuleBase
{
	protected override void InitializeGameStarter(Game game, IGameStarter gameStarterObject)
	{
		if (game.GameType is Campaign { CampaignGameLoadingType: Campaign.GameLoadingType.NewCampaign } campaign)
		{
			campaign.Options.AccelerationMode = GameAccelerationMode.Fast;
			if (gameStarterObject is CampaignGameStarter campaignGameStarter)
			{
				campaignGameStarter.GetModel<DefaultCharacterDevelopmentModel>()?.InitializeXpRequiredForSkillLevel();
			}
		}
	}
}
