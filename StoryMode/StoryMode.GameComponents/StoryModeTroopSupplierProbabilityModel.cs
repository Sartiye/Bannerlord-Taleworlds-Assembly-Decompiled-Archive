using System.Collections.Generic;
using System.Linq;
using StoryMode.StoryModeObjects;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;

namespace StoryMode.GameComponents;

public class StoryModeTroopSupplierProbabilityModel : TroopSupplierProbabilityModel
{
	public override void EnqueueTroopSpawnProbabilitiesAccordingToUnitSpawnPrioritization(MapEventParty battleParty, FlattenedTroopRoster priorityTroops, bool includePlayers, int sizeOfSide, bool forcePriorityTroops, List<(FlattenedTroopRosterElement, MapEventParty, float)> priorityList)
	{
		int count = priorityList.Count;
		base.BaseModel.EnqueueTroopSpawnProbabilitiesAccordingToUnitSpawnPrioritization(battleParty, priorityTroops, includePlayers, sizeOfSide, forcePriorityTroops, priorityList);
		Settlement currentSettlement = Settlement.CurrentSettlement;
		if (currentSettlement == null || !currentSettlement.IsHideout || priorityTroops == null)
		{
			return;
		}
		if (!StoryModeManager.Current.MainStoryLine.TutorialPhase.IsCompleted)
		{
			for (int i = count; i < priorityList.Count; i++)
			{
				CharacterObject character2 = priorityList[i].Item1.Troop;
				if (character2 == StoryModeHeroes.Radagos.CharacterObject && priorityTroops.All((FlattenedTroopRosterElement t) => t.Troop != character2))
				{
					priorityList[i] = (priorityList[i].Item1, priorityList[i].Item2, 0.01f);
					break;
				}
			}
			return;
		}
		for (int j = 0; j < priorityList.Count; j++)
		{
			CharacterObject character = priorityList[j].Item1.Troop;
			if (character == StoryModeHeroes.RadagosHenchman.CharacterObject && priorityTroops.All((FlattenedTroopRosterElement t) => t.Troop != character))
			{
				priorityList[j] = (priorityList[j].Item1, priorityList[j].Item2, 0.01f);
				break;
			}
		}
	}
}
