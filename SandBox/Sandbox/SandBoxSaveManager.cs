using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;

namespace SandBox;

public class SandBoxSaveManager : ISaveManager
{
	public int GetAutoSaveInterval()
	{
		return BannerlordConfig.AutoSaveInterval;
	}

	public void OnSaveOver(bool isSuccessful, string newSaveGameName)
	{
		if (isSuccessful)
		{
			BannerlordConfig.LatestSaveGameName = newSaveGameName;
			BannerlordConfig.Save();
		}
	}
}
