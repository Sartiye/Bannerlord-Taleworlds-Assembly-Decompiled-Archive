using Helpers;
using NavalDLC.CharacterDevelopment;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace NavalDLC.CampaignBehaviors;

public class NavalFishingCampaignBehaviour : CampaignBehaviorBase
{
	public override void RegisterEvents()
	{
		CampaignEvents.HourlyTickPartyEvent.AddNonSerializedListener(this, OnHourlyTickParty);
		CampaignEvents.DailyTickSettlementEvent.AddNonSerializedListener(this, OnDailyTickSettlement);
	}

	private void OnDailyTickSettlement(Settlement settlement)
	{
		if (settlement.IsVillage && settlement.Village.TradeBound != null)
		{
			ExplainedNumber bonuses = new ExplainedNumber(0f, includeDescriptions: false, null);
			PerkHelper.AddPerkBonusForTown(NavalPerks.Shipmaster.NightRaider, settlement.Village.TradeBound.Town, ref bonuses);
			if (bonuses.RoundedResultNumber > 0)
			{
				ItemObject @object = MBObjectManager.Instance.GetObject<ItemObject>("fish");
				int roundedResultNumber = bonuses.RoundedResultNumber;
				settlement.Village.Owner.ItemRoster.AddToCounts(@object, roundedResultNumber);
				CampaignEventDispatcher.Instance.OnItemProduced(@object, settlement.Village.Owner.Settlement, roundedResultNumber);
			}
		}
	}

	private void OnHourlyTickParty(MobileParty party)
	{
		if (party.IsCurrentlyAtSea)
		{
			float num = 0f;
			if (party.HasPerk(NavalPerks.Shipmaster.MasterAngler))
			{
				num += NavalPerks.Shipmaster.MasterAngler.PrimaryBonus;
			}
			if (MBRandom.RandomFloat < num)
			{
				ItemObject @object = MBObjectManager.Instance.GetObject<ItemObject>("fish");
				party.ItemRoster.AddToCounts(@object, 1);
			}
		}
	}

	public override void SyncData(IDataStore dataStore)
	{
	}
}
