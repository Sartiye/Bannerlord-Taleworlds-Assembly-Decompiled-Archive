using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;

namespace NavalDLC.CampaignBehaviors;

public class NavalShipDistributionCampaignBehavior : CampaignBehaviorBase
{
	public override void SyncData(IDataStore dataStore)
	{
	}

	public override void RegisterEvents()
	{
		CampaignEvents.OnPartyDisbandedEvent.AddNonSerializedListener(this, OnPartyDisbanded);
		CampaignEvents.MobilePartyDestroyed.AddNonSerializedListener(this, OnMobilePartyDestroyed);
	}

	private void OnMobilePartyDestroyed(MobileParty party, PartyBase destroyerParty)
	{
		if (party.ActualClan != null && !party.IsCurrentlyAtSea)
		{
			DistributePartyShipsAndRecoverGold(party);
		}
	}

	private void DistributePartyShipsAndRecoverGold(MobileParty mobileParty)
	{
		DistributeShips(mobileParty);
		RecoverGoldFromRemainingShipsAfterDistribution(mobileParty);
	}

	private void OnPartyDisbanded(MobileParty disbandParty, Settlement relatedSettlement)
	{
		if (disbandParty.ActualClan != null && !disbandParty.ActualClan.IsBanditFaction)
		{
			DistributePartyShipsAndRecoverGold(disbandParty);
		}
	}

	private void RecoverGoldFromRemainingShipsAfterDistribution(MobileParty party)
	{
		if (party.ActualClan == null || party.ActualClan.IsBanditFaction || party.ActualClan.Leader == null || !party.ActualClan.Leader.IsActive || party.Ships.Count <= 0)
		{
			return;
		}
		int num = (int)party.Ships.SumQ((Ship x) => Campaign.Current.Models.ShipCostModel.GetShipTradeValue(x, party.Party, null));
		if (party.ActualClan == Clan.PlayerClan)
		{
			float shipSellingPenalty = Campaign.Current.Models.ShipCostModel.GetShipSellingPenalty();
			num = (int)((float)num * shipSellingPenalty);
			if (party.Owner != null)
			{
				MBTextManager.SetTextVariable("GOLD_AMOUNT", num);
				MBTextManager.SetTextVariable("LEADER_NAME", party.Owner.Name);
				MBTextManager.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
				MBInformationManager.AddQuickInformation(new TextObject("{=YaSnA9j0}{LEADER_NAME}'s party has disbanded. You recovered {GOLD_AMOUNT}{GOLD_ICON} from its ships."));
			}
		}
		GiveGoldAction.ApplyBetweenCharacters(null, party.ActualClan.Leader, num);
	}

	private void DistributeShips(MobileParty party)
	{
		for (int num = party.Ships.Count - 1; num >= 0; num--)
		{
			Ship shipToSend = party.Ships[num];
			if (party.ActualClan.WarPartyComponents.AnyQ((WarPartyComponent x) => x.MobileParty != party && NavalDLCManager.Instance.GameModels.ShipDistributionModel.CanSendShipToParty(shipToSend, x.MobileParty)))
			{
				MobileParty clanPartyToGetShipOfDisbandingParty = GetClanPartyToGetShipOfDisbandingParty(shipToSend, party.ActualClan);
				if (clanPartyToGetShipOfDisbandingParty != null && clanPartyToGetShipOfDisbandingParty != party)
				{
					ChangeShipOwnerAction.ApplyByTransferring(clanPartyToGetShipOfDisbandingParty.Party, shipToSend);
				}
			}
		}
	}

	private MobileParty GetClanPartyToGetShipOfDisbandingParty(Ship ship, Clan clan)
	{
		MobileParty mobileParty = null;
		float num = 0f;
		MBList<Ship> mBList = new MBList<Ship>();
		foreach (WarPartyComponent warPartyComponent in clan.WarPartyComponents)
		{
			if (warPartyComponent.Party != ship.Owner && NavalDLCManager.Instance.GameModels.ShipDistributionModel.CanSendShipToParty(ship, warPartyComponent.MobileParty) && (mobileParty == null || warPartyComponent.Party.Ships.Count <= mobileParty.Ships.Count))
			{
				mBList.Clear();
				mBList.AddRange(warPartyComponent.Party.Ships);
				float scoreForPartyShipComposition = NavalDLCManager.Instance.GameModels.ShipDistributionModel.GetScoreForPartyShipComposition(warPartyComponent.MobileParty, mBList);
				mBList.Add(ship);
				float num2 = NavalDLCManager.Instance.GameModels.ShipDistributionModel.GetScoreForPartyShipComposition(warPartyComponent.MobileParty, mBList) - scoreForPartyShipComposition;
				if (num2 > num)
				{
					mobileParty = warPartyComponent.MobileParty;
					num = num2;
				}
			}
		}
		return mobileParty;
	}
}
