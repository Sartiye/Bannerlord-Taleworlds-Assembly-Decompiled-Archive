using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.LinQuick;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultTroopSupplierProbabilityModel : TroopSupplierProbabilityModel
{
	public override void EnqueueTroopSpawnProbabilitiesAccordingToUnitSpawnPrioritization(MapEventParty battleParty, FlattenedTroopRoster priorityTroops, bool includePlayer, int sizeOfSide, bool forcePriorityTroops, List<(FlattenedTroopRosterElement, MapEventParty, float)> priorityList)
	{
		UnitSpawnPrioritizations unitSpawnPrioritizations = UnitSpawnPrioritizations.HighLevel;
		bool flag = PlayerEncounter.Battle?.IsSiegeAmbush ?? false;
		if (battleParty.Party == PartyBase.MainParty && !flag)
		{
			unitSpawnPrioritizations = Game.Current.UnitSpawnPrioritization;
		}
		if (includePlayer)
		{
			List<KeyValuePair<int, FlattenedTroopRosterElement>> list = new List<KeyValuePair<int, FlattenedTroopRosterElement>>();
			List<KeyValuePair<int, FlattenedTroopRosterElement>> list2 = new List<KeyValuePair<int, FlattenedTroopRosterElement>>();
			List<KeyValuePair<int, FlattenedTroopRosterElement>> list3 = new List<KeyValuePair<int, FlattenedTroopRosterElement>>();
			List<KeyValuePair<int, FlattenedTroopRosterElement>> list4 = new List<KeyValuePair<int, FlattenedTroopRosterElement>>();
			int num = 0;
			foreach (FlattenedTroopRosterElement troop in battleParty.Troops)
			{
				if (CanTroopJoinBattle(troop, includePlayer))
				{
					int key = 0;
					switch (unitSpawnPrioritizations)
					{
					case UnitSpawnPrioritizations.Default:
						key = num;
						break;
					case UnitSpawnPrioritizations.HighLevel:
						key = troop.Troop.Level;
						break;
					case UnitSpawnPrioritizations.LowLevel:
						key = -troop.Troop.Level;
						break;
					}
					bool isHero = troop.Troop.IsHero;
					if (isHero && troop.Troop.IsPlayerCharacter)
					{
						key = int.MaxValue;
					}
					bool flag2 = false;
					if (priorityTroops != null)
					{
						foreach (FlattenedTroopRosterElement priorityTroop in priorityTroops)
						{
							if (priorityTroop.Troop == troop.Troop)
							{
								flag2 = true;
								break;
							}
						}
					}
					if (flag2)
					{
						if (isHero)
						{
							list3.Add(new KeyValuePair<int, FlattenedTroopRosterElement>(key, troop));
						}
						else
						{
							list4.Add(new KeyValuePair<int, FlattenedTroopRosterElement>(key, troop));
						}
					}
					else if (isHero)
					{
						list2.Add(new KeyValuePair<int, FlattenedTroopRosterElement>(key, troop));
					}
					else
					{
						list.Add(new KeyValuePair<int, FlattenedTroopRosterElement>(key, troop));
					}
				}
				num++;
			}
			list3 = list3.OrderByQ((KeyValuePair<int, FlattenedTroopRosterElement> x) => x.Key).ToList();
			list4 = list4.OrderByQ((KeyValuePair<int, FlattenedTroopRosterElement> x) => x.Key).ToList();
			list2 = list2.OrderByQ((KeyValuePair<int, FlattenedTroopRosterElement> x) => x.Key).ToList();
			list = list.OrderByQ((KeyValuePair<int, FlattenedTroopRosterElement> x) => x.Key).ToList();
			for (int i = 0; i < list3.Count; i++)
			{
				priorityList.Add((list3[i].Value, battleParty, 3f + (float)(i + 1) / (float)list3.Count));
			}
			for (int j = 0; j < list4.Count; j++)
			{
				priorityList.Add((list4[j].Value, battleParty, 2f + (float)(j + 1) / (float)list4.Count));
			}
			for (int k = 0; k < list2.Count; k++)
			{
				priorityList.Add((list2[k].Value, battleParty, 1f + (float)(k + 1) / (float)list2.Count));
			}
			for (int l = 0; l < list.Count; l++)
			{
				priorityList.Add((list[l].Value, battleParty, (float)(l + 1) / (float)list.Count));
			}
			return;
		}
		foreach (FlattenedTroopRosterElement troop2 in battleParty.Troops)
		{
			if (CanTroopJoinBattle(troop2, includePlayer))
			{
				priorityList.Add((troop2, battleParty, 2.1474836E+09f));
			}
		}
	}

	private bool CanTroopJoinBattle(FlattenedTroopRosterElement troopRoster, bool includePlayer)
	{
		if (!troopRoster.IsWounded && !troopRoster.IsRouted && !troopRoster.IsKilled)
		{
			if (!includePlayer)
			{
				return !troopRoster.Troop.IsPlayerCharacter;
			}
			return true;
		}
		return false;
	}
}
