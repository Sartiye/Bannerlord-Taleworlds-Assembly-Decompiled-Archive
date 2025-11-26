using System.Collections.Generic;
using TaleWorlds.CampaignSystem.ViewModelCollection;

namespace TaleWorlds.CampaignSystem.FastMode;

public class FastModeOptionsProvider : ICampaignOptionProvider
{
	public IEnumerable<ICampaignOptionData> GetGameplayCampaignOptions()
	{
		yield return new BooleanCampaignOptionData("IsFastModeEnabled", 880, CampaignOptionEnableState.Disabled, () => 1f, delegate
		{
		});
	}

	public IEnumerable<ICampaignOptionData> GetCharacterCreationCampaignOptions()
	{
		yield return new BooleanCampaignOptionData("IsFastModeEnabled", 880, CampaignOptionEnableState.Disabled, () => 1f, delegate
		{
		});
	}
}
