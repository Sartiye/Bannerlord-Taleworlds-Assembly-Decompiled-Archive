using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class PartyRolesCampaignBehavior : CampaignBehaviorBase
{
	public override void RegisterEvents()
	{
		CampaignEvents.HeroKilledEvent.AddNonSerializedListener(this, OnHeroKilled);
		CampaignEvents.OnGovernorChangedEvent.AddNonSerializedListener(this, OnGovernorChanged);
		CampaignEvents.MobilePartyCreated.AddNonSerializedListener(this, OnPartySpawned);
		CampaignEvents.CompanionRemoved.AddNonSerializedListener(this, OnCompanionRemoved);
		CampaignEvents.OnHeroGetsBusyEvent.AddNonSerializedListener(this, OnHeroGetsBusy);
		CampaignEvents.HeroPrisonerTaken.AddNonSerializedListener(this, OnHeroPrisonerTaken);
		CampaignEvents.OnHeroChangedClanEvent.AddNonSerializedListener(this, OnHeroChangedClan);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private void OnHeroKilled(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, bool showNotification = true)
	{
		if (victim.Clan == Clan.PlayerClan)
		{
			RemovePartyRoleIfExist(victim);
		}
	}

	private void OnHeroPrisonerTaken(PartyBase party, Hero prisoner)
	{
		if (prisoner.Clan == Clan.PlayerClan)
		{
			RemovePartyRoleIfExist(prisoner);
		}
	}

	private void OnGovernorChanged(Town fortification, Hero oldGovernor, Hero newGovernor)
	{
		if (newGovernor?.Clan == Clan.PlayerClan)
		{
			RemovePartyRoleIfExist(newGovernor);
		}
	}

	private void OnPartySpawned(MobileParty spawnedParty)
	{
		if (!spawnedParty.IsLordParty || spawnedParty.ActualClan != Clan.PlayerClan)
		{
			return;
		}
		foreach (TroopRosterElement item in spawnedParty.MemberRoster.GetTroopRoster())
		{
			if (item.Character.IsHero)
			{
				RemovePartyRoleIfExist(item.Character.HeroObject);
			}
		}
	}

	private void OnCompanionRemoved(Hero companion, RemoveCompanionAction.RemoveCompanionDetail detail)
	{
		RemovePartyRoleIfExist(companion);
	}

	private void OnHeroGetsBusy(Hero hero, HeroGetsBusyReasons heroGetsBusyReason)
	{
		if (hero.Clan == Clan.PlayerClan)
		{
			RemovePartyRoleIfExist(hero);
		}
	}

	private void OnHeroChangedClan(Hero hero, Clan oldClan)
	{
		if (oldClan == Clan.PlayerClan)
		{
			RemovePartyRoleIfExist(hero);
		}
	}

	private void RemovePartyRoleIfExist(Hero hero)
	{
		foreach (WarPartyComponent warPartyComponent in Clan.PlayerClan.WarPartyComponents)
		{
			if (warPartyComponent.MobileParty.GetHeroPartyRole(hero) != 0)
			{
				warPartyComponent.MobileParty.RemoveHeroPartyRole(hero);
			}
		}
	}
}
