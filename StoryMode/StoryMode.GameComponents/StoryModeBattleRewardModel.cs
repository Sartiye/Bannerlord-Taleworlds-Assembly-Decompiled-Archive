using System.Collections.Generic;
using StoryMode.StoryModePhases;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace StoryMode.GameComponents;

public class StoryModeBattleRewardModel : BattleRewardModel
{
	public override int CalculateGoldLossAfterDefeat(Hero partyLeaderHero)
	{
		return base.BaseModel.CalculateGoldLossAfterDefeat(partyLeaderHero);
	}

	public override ExplainedNumber CalculateInfluenceGain(PartyBase party, float influenceValueOfBattle, float contributionShare)
	{
		return base.BaseModel.CalculateInfluenceGain(party, influenceValueOfBattle, contributionShare);
	}

	public override ExplainedNumber CalculateMoraleChangeOnRoundVictory(PartyBase party, MapEventSide partySide, BattleSideEnum roundWinner)
	{
		return base.BaseModel.CalculateMoraleChangeOnRoundVictory(party, partySide, roundWinner);
	}

	public override ExplainedNumber CalculateMoraleGainVictory(PartyBase party, float renownValueOfBattle, float contributionShare, MapEvent battle)
	{
		return base.BaseModel.CalculateMoraleGainVictory(party, renownValueOfBattle, contributionShare, battle);
	}

	public override int CalculatePlunderedGoldAmountFromDefeatedParty(PartyBase defeatedParty)
	{
		return base.BaseModel.CalculatePlunderedGoldAmountFromDefeatedParty(defeatedParty);
	}

	public override ExplainedNumber CalculateRenownGain(PartyBase party, float renownValueOfBattle, float contributionShare)
	{
		if (TutorialPhase.Instance != null && !TutorialPhase.Instance.IsCompleted && party == PartyBase.MainParty)
		{
			return default(ExplainedNumber);
		}
		return base.BaseModel.CalculateRenownGain(party, renownValueOfBattle, contributionShare);
	}

	public override float CalculateShipDamageAfterDefeat(Ship ship)
	{
		return 0f;
	}

	public override MBReadOnlyList<KeyValuePair<Ship, MapEventParty>> DistributeDefeatedPartyShipsAmongWinners(MapEvent mapEvent, MBReadOnlyList<Ship> shipsToLoot, MBReadOnlyList<MapEventParty> winnerParties)
	{
		return new MBReadOnlyList<KeyValuePair<Ship, MapEventParty>>();
	}

	public override float GetAITradePenalty()
	{
		return base.BaseModel.GetAITradePenalty();
	}

	public override float GetBannerLootChanceFromDefeatedHero(Hero defeatedHero)
	{
		return base.BaseModel.GetBannerLootChanceFromDefeatedHero(defeatedHero);
	}

	public override ItemObject GetBannerRewardForWinningMapEvent(MapEvent mapEvent)
	{
		return base.BaseModel.GetBannerRewardForWinningMapEvent(mapEvent);
	}

	public override float GetExpectedLootedItemValueFromCasualty(Hero winnerPartyLeaderHero, CharacterObject casualtyCharacter)
	{
		return base.BaseModel.GetExpectedLootedItemValueFromCasualty(winnerPartyLeaderHero, casualtyCharacter);
	}

	public override Figurehead GetFigureheadLoot(MBReadOnlyList<MapEventParty> defeatedParties, PartyBase defeatedSideLeaderParty)
	{
		return base.BaseModel.GetFigureheadLoot(defeatedParties, defeatedSideLeaderParty);
	}

	public override MBReadOnlyList<KeyValuePair<MapEventParty, float>> GetLootCasualtyChances(MBReadOnlyList<MapEventParty> winnerParties, PartyBase defeatedParty)
	{
		return base.BaseModel.GetLootCasualtyChances(winnerParties, defeatedParty);
	}

	public override EquipmentElement GetLootedItemFromTroop(CharacterObject character, float targetValue)
	{
		return base.BaseModel.GetLootedItemFromTroop(character, targetValue);
	}

	public override MBReadOnlyList<KeyValuePair<MapEventParty, float>> GetLootGoldChances(MBReadOnlyList<MapEventParty> winnerParties)
	{
		return base.BaseModel.GetLootGoldChances(winnerParties);
	}

	public override MBList<KeyValuePair<MapEventParty, float>> GetLootItemChancesForWinnerParties(MBReadOnlyList<MapEventParty> winnerParties, PartyBase defeatedParty)
	{
		return base.BaseModel.GetLootItemChancesForWinnerParties(winnerParties, defeatedParty);
	}

	public override MBReadOnlyList<KeyValuePair<MapEventParty, float>> GetLootMemberChancesForWinnerParties(MBReadOnlyList<MapEventParty> winnerParties)
	{
		return base.BaseModel.GetLootMemberChancesForWinnerParties(winnerParties);
	}

	public override MBReadOnlyList<KeyValuePair<MapEventParty, float>> GetLootPrisonerChances(MBReadOnlyList<MapEventParty> winnerParties, TroopRosterElement prisonerElement)
	{
		return base.BaseModel.GetLootPrisonerChances(winnerParties, prisonerElement);
	}

	public override float GetMainPartyMemberScatterChance()
	{
		return base.BaseModel.GetMainPartyMemberScatterChance();
	}

	public override int GetPlayerGainedRelationAmount(MapEvent mapEvent, Hero hero)
	{
		return base.BaseModel.GetPlayerGainedRelationAmount(mapEvent, hero);
	}

	public override float GetShipSiegeEngineHitMoraleEffect(Ship ship, SiegeEngineType siegeEngineType)
	{
		return base.BaseModel.GetShipSiegeEngineHitMoraleEffect(ship, siegeEngineType);
	}

	public override float GetSunkenShipMoraleEffect(PartyBase shipOwner, Ship ship)
	{
		return base.BaseModel.GetSunkenShipMoraleEffect(shipOwner, ship);
	}

	public override MBReadOnlyList<MapEventParty> GetWinnerPartiesThatCanPlunderGoldFromShips(MBReadOnlyList<MapEventParty> winnerParties)
	{
		return base.BaseModel.GetWinnerPartiesThatCanPlunderGoldFromShips(winnerParties);
	}
}
