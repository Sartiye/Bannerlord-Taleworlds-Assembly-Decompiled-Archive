using Helpers;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.Actions;

public static class GainKingdomInfluenceAction
{
	private enum InfluenceGainingReason
	{
		Default,
		BeingAtArmy,
		Battle,
		Raiding,
		Besieging,
		CaptureSettlement,
		JoinFaction,
		GivingFood,
		LeaveGarrison,
		BoardGameWon,
		ClanSupport,
		DonatePrisoners,
		SiegeSafePassage
	}

	private static void ApplyInternal(Hero hero, MobileParty party, float gainedInfluence, InfluenceGainingReason detail)
	{
		Clan clan = null;
		if (hero != null)
		{
			if (hero.CompanionOf != null)
			{
				clan = hero.CompanionOf;
			}
			else if (hero.Clan != null)
			{
				clan = hero.Clan;
			}
		}
		else if (party.ActualClan != null)
		{
			clan = party.ActualClan;
		}
		else if (party.Owner != null)
		{
			clan = party.Owner.Clan;
		}
		if (clan != null && clan.Kingdom != null)
		{
			MobileParty mobileParty = party ?? hero.PartyBelongedTo;
			if (detail != InfluenceGainingReason.BeingAtArmy && detail == InfluenceGainingReason.ClanSupport)
			{
				gainedInfluence = 0.5f;
			}
			if (detail != 0 && detail != InfluenceGainingReason.GivingFood && detail != InfluenceGainingReason.JoinFaction && detail != InfluenceGainingReason.ClanSupport && ((Kingdom)clan.MapFaction).ActivePolicies.Contains(DefaultPolicies.MilitaryCoronae))
			{
				gainedInfluence *= 1.2f;
			}
			ExplainedNumber stat = new ExplainedNumber(gainedInfluence);
			if (detail == InfluenceGainingReason.Battle && gainedInfluence > 0f)
			{
				PerkHelper.AddPerkBonusForParty(DefaultPerks.Tactics.PreBattleManeuvers, mobileParty, isPrimaryBonus: true, ref stat);
			}
			if (detail == InfluenceGainingReason.CaptureSettlement && (hero != null || mobileParty.LeaderHero != null))
			{
				Hero hero2 = hero ?? mobileParty.LeaderHero;
				PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Tactics.Besieged, hero2.CharacterObject, isPrimaryBonus: false, ref stat);
			}
			ChangeClanInfluenceAction.Apply(clan, stat.ResultNumber);
			if (detail == InfluenceGainingReason.DonatePrisoners && party == MobileParty.MainParty)
			{
				MBTextManager.SetTextVariable("INFLUENCE", (int)gainedInfluence);
				MBTextManager.SetTextVariable("NEW_INFLUENCE", (int)clan.Influence);
				InformationManager.DisplayMessage(new InformationMessage(GameTexts.FindText("str_influence_gain_message").ToString()));
			}
			if (detail == InfluenceGainingReason.Battle && hero == Hero.MainHero)
			{
				MBTextManager.SetTextVariable("INFLUENCE", (int)gainedInfluence);
				MBTextManager.SetTextVariable("NEW_INFLUENCE", (int)clan.Influence);
				InformationManager.DisplayMessage(new InformationMessage(GameTexts.FindText("str_influence_gain_message").ToString()));
			}
			if (detail == InfluenceGainingReason.SiegeSafePassage && hero == Hero.MainHero)
			{
				MBTextManager.SetTextVariable("INFLUENCE", -(int)gainedInfluence);
				InformationManager.DisplayMessage(new InformationMessage(GameTexts.FindText("str_leave_siege_lose_influence_message").ToString()));
			}
		}
	}

	public static void ApplyForBattle(Hero hero, float value)
	{
		ApplyInternal(hero, null, value, InfluenceGainingReason.Battle);
	}

	public static void ApplyForGivingFood(Hero hero1, Hero hero2, float value)
	{
		ApplyInternal(hero1, null, value, InfluenceGainingReason.GivingFood);
		ApplyInternal(hero2, null, 0f - value, InfluenceGainingReason.GivingFood);
	}

	public static void ApplyForDefault(Hero hero, float value)
	{
		ApplyInternal(hero, null, value, InfluenceGainingReason.Default);
	}

	public static void ApplyForJoiningFaction(Hero hero, float value)
	{
		ApplyInternal(hero, null, value, InfluenceGainingReason.JoinFaction);
	}

	public static void ApplyForDonatePrisoners(MobileParty donatingParty, float value)
	{
		ApplyInternal(null, donatingParty, value, InfluenceGainingReason.DonatePrisoners);
	}

	public static void ApplyForRaidingEnemyVillage(MobileParty side1Party, float value)
	{
		ApplyInternal(null, side1Party, value, InfluenceGainingReason.Raiding);
	}

	public static void ApplyForBesiegingEnemySettlement(MobileParty side1Party, float value)
	{
		ApplyInternal(null, side1Party, value, InfluenceGainingReason.Besieging);
	}

	public static void ApplyForSiegeSafePassageBarter(MobileParty side1Party, float value)
	{
		ApplyInternal(null, side1Party, value, InfluenceGainingReason.SiegeSafePassage);
	}

	public static void ApplyForCapturingEnemySettlement(MobileParty side1Party, float value)
	{
		ApplyInternal(null, side1Party, value, InfluenceGainingReason.CaptureSettlement);
	}

	public static void ApplyForLeavingTroopToGarrison(Hero hero, float value)
	{
		ApplyInternal(hero, null, value, InfluenceGainingReason.LeaveGarrison);
	}

	public static void ApplyForBoardGameWon(Hero hero, float value)
	{
		ApplyInternal(hero, null, value, InfluenceGainingReason.BoardGameWon);
	}
}
