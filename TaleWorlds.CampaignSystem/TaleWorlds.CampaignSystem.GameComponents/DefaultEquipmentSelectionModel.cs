using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultEquipmentSelectionModel : EquipmentSelectionModel
{
	public override MBList<MBEquipmentRoster> GetEquipmentRostersForHeroComeOfAge(Hero hero, bool isCivilian)
	{
		MBList<MBEquipmentRoster> mBList = new MBList<MBEquipmentRoster>();
		bool flag = !hero.IsNoncombatant;
		foreach (MBEquipmentRoster item in MBEquipmentRosterExtensions.All)
		{
			if (!IsRosterAppropriateForHeroAsTemplate(item, hero, shouldMatchGender: true, EquipmentFlags.IsNobleTemplate))
			{
				continue;
			}
			if (isCivilian)
			{
				if (flag)
				{
					if (item.HasEquipmentFlags(EquipmentFlags.IsCombatantTemplate | EquipmentFlags.IsCivilianTemplate))
					{
						mBList.Add(item);
					}
				}
				else if (item.HasEquipmentFlags(EquipmentFlags.IsNoncombatantTemplate))
				{
					mBList.Add(item);
				}
			}
			else if (item.HasEquipmentFlags(EquipmentFlags.IsMediumTemplate))
			{
				mBList.Add(item);
			}
		}
		return mBList;
	}

	public override MBList<MBEquipmentRoster> GetEquipmentRostersForHeroReachesTeenAge(Hero hero)
	{
		EquipmentFlags suitableFlags = EquipmentFlags.IsNobleTemplate | EquipmentFlags.IsTeenagerEquipmentTemplate;
		MBList<MBEquipmentRoster> roster = new MBList<MBEquipmentRoster>();
		AddEquipmentsToRoster(hero, suitableFlags, ref roster, shouldMatchGender: true);
		return roster;
	}

	public override MBList<MBEquipmentRoster> GetEquipmentRostersForInitialChildrenGeneration(Hero hero)
	{
		bool flag = hero.Age < (float)Campaign.Current.Models.AgeModel.BecomeTeenagerAge;
		EquipmentFlags suitableFlags = EquipmentFlags.IsNobleTemplate | (flag ? EquipmentFlags.IsChildEquipmentTemplate : EquipmentFlags.IsTeenagerEquipmentTemplate);
		MBList<MBEquipmentRoster> roster = new MBList<MBEquipmentRoster>();
		AddEquipmentsToRoster(hero, suitableFlags, ref roster, shouldMatchGender: true);
		return roster;
	}

	public override MBList<MBEquipmentRoster> GetEquipmentRostersForDeliveredOffspring(Hero hero)
	{
		EquipmentFlags suitableFlags = EquipmentFlags.IsNobleTemplate | EquipmentFlags.IsChildEquipmentTemplate;
		MBList<MBEquipmentRoster> roster = new MBList<MBEquipmentRoster>();
		AddEquipmentsToRoster(hero, suitableFlags, ref roster, shouldMatchGender: true);
		return roster;
	}

	public override MBList<MBEquipmentRoster> GetEquipmentRostersForCompanion(Hero hero, bool isCivilian)
	{
		EquipmentFlags suitableFlags = (isCivilian ? (EquipmentFlags.IsCivilianTemplate | EquipmentFlags.IsNobleTemplate) : (EquipmentFlags.IsNobleTemplate | EquipmentFlags.IsMediumTemplate));
		MBList<MBEquipmentRoster> roster = new MBList<MBEquipmentRoster>();
		AddEquipmentsToRoster(hero, suitableFlags, ref roster, isCivilian);
		return roster;
	}

	private bool IsRosterAppropriateForHeroAsTemplate(MBEquipmentRoster equipmentRoster, Hero hero, bool shouldMatchGender, EquipmentFlags customFlags = EquipmentFlags.None)
	{
		bool result = false;
		if (equipmentRoster.IsEquipmentTemplate() && (!shouldMatchGender || equipmentRoster.HasEquipmentFlags(EquipmentFlags.IsFemaleTemplate) == hero.IsFemale) && equipmentRoster.EquipmentCulture == hero.Culture && (customFlags == EquipmentFlags.None || equipmentRoster.HasEquipmentFlags(customFlags)))
		{
			bool num = equipmentRoster.HasEquipmentFlags(EquipmentFlags.IsNomadTemplate) || equipmentRoster.HasEquipmentFlags(EquipmentFlags.IsWoodlandTemplate);
			bool flag = !hero.IsChild && (equipmentRoster.HasEquipmentFlags(EquipmentFlags.IsChildEquipmentTemplate) || equipmentRoster.HasEquipmentFlags(EquipmentFlags.IsTeenagerEquipmentTemplate));
			if (!num && !flag)
			{
				result = true;
			}
		}
		return result;
	}

	private bool IsHeroCombatant(Hero hero)
	{
		if (hero.IsFemale && hero.Clan != Hero.MainHero.Clan && (hero.Mother == null || hero.Mother.IsNoncombatant))
		{
			if (hero.RandomIntWithSeed(17u, 0, 1) == 0)
			{
				return hero.GetTraitLevel(DefaultTraits.Valor) == 1;
			}
			return false;
		}
		return true;
	}

	private void AddEquipmentsToRoster(Hero hero, EquipmentFlags suitableFlags, ref MBList<MBEquipmentRoster> roster, bool shouldMatchGender)
	{
		foreach (MBEquipmentRoster item in MBEquipmentRosterExtensions.All)
		{
			if (IsRosterAppropriateForHeroAsTemplate(item, hero, shouldMatchGender, suitableFlags))
			{
				roster.Add(item);
			}
		}
	}
}
