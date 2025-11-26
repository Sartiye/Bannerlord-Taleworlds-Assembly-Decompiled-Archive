using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.Inventory;

public struct TransferCommand
{
	public Equipment FromSideEquipment => FromSide switch
	{
		InventoryLogic.InventorySide.CivilianEquipment => Character?.FirstCivilianEquipment, 
		InventoryLogic.InventorySide.BattleEquipment => Character?.FirstBattleEquipment, 
		InventoryLogic.InventorySide.StealthEquipment => Character?.FirstStealthEquipment, 
		_ => null, 
	};

	public Equipment ToSideEquipment => ToSide switch
	{
		InventoryLogic.InventorySide.CivilianEquipment => Character?.FirstCivilianEquipment, 
		InventoryLogic.InventorySide.BattleEquipment => Character?.FirstBattleEquipment, 
		InventoryLogic.InventorySide.StealthEquipment => Character?.FirstStealthEquipment, 
		_ => null, 
	};

	public InventoryLogic.InventorySide FromSide { get; private set; }

	public InventoryLogic.InventorySide ToSide { get; private set; }

	public EquipmentIndex FromEquipmentIndex { get; private set; }

	public EquipmentIndex ToEquipmentIndex { get; private set; }

	public int Amount { get; private set; }

	public ItemRosterElement ElementToTransfer { get; private set; }

	public CharacterObject Character { get; private set; }

	public static TransferCommand Transfer(int amount, InventoryLogic.InventorySide fromSide, InventoryLogic.InventorySide toSide, ItemRosterElement elementToTransfer, EquipmentIndex fromEquipmentIndex, EquipmentIndex toEquipmentIndex, CharacterObject character)
	{
		TransferCommand result = default(TransferCommand);
		result.FromSide = fromSide;
		result.ToSide = toSide;
		result.ElementToTransfer = elementToTransfer;
		result.FromEquipmentIndex = fromEquipmentIndex;
		result.ToEquipmentIndex = toEquipmentIndex;
		result.Character = character;
		result.Amount = amount;
		return result;
	}
}
