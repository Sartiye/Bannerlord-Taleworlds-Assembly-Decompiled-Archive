using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class EquipmentSelectionModel : MBGameModel<EquipmentSelectionModel>
{
	public abstract MBList<MBEquipmentRoster> GetEquipmentRostersForHeroComeOfAge(Hero hero, bool isCivilian);

	public abstract MBList<MBEquipmentRoster> GetEquipmentRostersForHeroReachesTeenAge(Hero hero);

	public abstract MBList<MBEquipmentRoster> GetEquipmentRostersForInitialChildrenGeneration(Hero hero);

	public abstract MBList<MBEquipmentRoster> GetEquipmentRostersForDeliveredOffspring(Hero hero);

	public abstract MBList<MBEquipmentRoster> GetEquipmentRostersForCompanion(Hero companionHero, bool isCivilian);
}
