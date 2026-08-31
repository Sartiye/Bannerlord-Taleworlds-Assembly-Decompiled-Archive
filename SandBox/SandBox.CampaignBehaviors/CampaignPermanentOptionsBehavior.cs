using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.MountAndBlade;

namespace SandBox.CampaignBehaviors;

public class CampaignPermanentOptionsBehavior : CampaignBehaviorBase
{
	public override void RegisterEvents()
	{
		CampaignEvents.RulingClanChanged.AddNonSerializedListener(this, OnRulingClanChanged);
		CampaignEvents.KingdomCreatedEvent.AddNonSerializedListener(this, OnKingdomCreated);
		CampaignEvents.MobilePartyCreated.AddNonSerializedListener(this, OnMobilePartyCreated);
		CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, OnClanChangedKingdom);
		CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoaded);
		CampaignEvents.OnMercenaryServiceEndedEvent.AddNonSerializedListener(this, OnMercenaryServiceEnded);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private void OnGameLoaded(CampaignGameStarter starter)
	{
		bool flag = CheckKingPlaythroughIsCompleted() || CheckVassalPlaythroughIsCompleted() || CheckMercenaryPlaythroughIsCompleted();
		if (CheckTraderPlaythroughIsCompleted() || flag)
		{
			BannerlordConfig.Save();
		}
	}

	private void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification)
	{
		if (newKingdom != null && (CheckVassalPlaythroughIsCompleted() || CheckMercenaryPlaythroughIsCompleted()))
		{
			BannerlordConfig.Save();
		}
	}

	private void OnRulingClanChanged(Kingdom kingdom, Clan oldRulingClan)
	{
		if (CheckKingPlaythroughIsCompleted())
		{
			BannerlordConfig.Save();
		}
	}

	private void OnMercenaryServiceEnded(Clan clan, EndMercenaryServiceAction.EndMercenaryServiceActionDetails details)
	{
		if (clan == Clan.PlayerClan && CheckVassalPlaythroughIsCompleted())
		{
			BannerlordConfig.Save();
		}
	}

	private void OnKingdomCreated(Kingdom kingdom)
	{
		if (CheckKingPlaythroughIsCompleted())
		{
			BannerlordConfig.Save();
		}
	}

	private void OnMobilePartyCreated(MobileParty party)
	{
		if (CheckTraderPlaythroughIsCompleted())
		{
			BannerlordConfig.Save();
		}
	}

	private bool CheckTraderPlaythroughIsCompleted()
	{
		if (Hero.MainHero.OwnedCaravans.Count > 0)
		{
			return TryUnlockTraderPlaythrough();
		}
		return false;
	}

	private bool CheckKingPlaythroughIsCompleted()
	{
		if (Clan.PlayerClan.Kingdom != null && Clan.PlayerClan.Kingdom.RulingClan == Clan.PlayerClan)
		{
			return TryUnlockKingPlaythrough();
		}
		return false;
	}

	private bool CheckVassalPlaythroughIsCompleted()
	{
		if (Clan.PlayerClan.Kingdom != null && Clan.PlayerClan.Kingdom.RulingClan != Clan.PlayerClan && !Clan.PlayerClan.IsUnderMercenaryService)
		{
			return TryUnlockVassalPlaythrough();
		}
		return false;
	}

	private bool CheckMercenaryPlaythroughIsCompleted()
	{
		if (Clan.PlayerClan.Kingdom != null && Clan.PlayerClan.Kingdom.RulingClan != Clan.PlayerClan && Clan.PlayerClan.IsUnderMercenaryService)
		{
			return TryUnlockMercenaryPlaythrough();
		}
		return false;
	}

	private bool TryUnlockKingPlaythrough()
	{
		bool num = !BannerlordConfig.CompletedKingPlaythrough;
		BannerlordConfig.CompletedKingPlaythrough = true;
		bool flag = TryUnlockVassalPlaythrough();
		return num || flag;
	}

	private bool TryUnlockVassalPlaythrough()
	{
		bool num = !BannerlordConfig.CompletedVassalPlaythrough;
		BannerlordConfig.CompletedVassalPlaythrough = true;
		bool flag = TryUnlockMercenaryPlaythrough();
		return num || flag;
	}

	private bool TryUnlockMercenaryPlaythrough()
	{
		bool result = !BannerlordConfig.CompletedMercenaryPlaythrough;
		BannerlordConfig.CompletedMercenaryPlaythrough = true;
		return result;
	}

	private bool TryUnlockTraderPlaythrough()
	{
		bool result = !BannerlordConfig.CompletedTraderPlaythrough;
		BannerlordConfig.CompletedTraderPlaythrough = true;
		return result;
	}
}
