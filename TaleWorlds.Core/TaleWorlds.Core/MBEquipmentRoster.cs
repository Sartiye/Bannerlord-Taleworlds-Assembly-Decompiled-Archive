using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.Core;

public class MBEquipmentRoster : MBObjectBase
{
	public static readonly Equipment EmptyEquipment = new Equipment(Equipment.EquipmentType.Civilian);

	private MBList<Equipment> _equipments = new MBList<Equipment>();

	public BasicCultureObject EquipmentCulture;

	public EquipmentFlags EquipmentFlags { get; private set; }

	public MBReadOnlyList<Equipment> AllEquipments
	{
		get
		{
			if (_equipments.IsEmpty())
			{
				return new MBList<Equipment>(1) { EmptyEquipment };
			}
			return _equipments;
		}
	}

	public Equipment DefaultEquipment
	{
		get
		{
			if (!_equipments.IsEmpty())
			{
				return _equipments.FirstOrDefault();
			}
			return EmptyEquipment;
		}
	}

	public bool HasEquipmentFlags(EquipmentFlags flags)
	{
		return (EquipmentFlags & flags) == flags;
	}

	public bool IsEquipmentTemplate()
	{
		return EquipmentFlags != EquipmentFlags.None;
	}

	public void Init(MBObjectManager objectManager, XmlNode node)
	{
		if (node.Name == "EquipmentRoster")
		{
			InitEquipment(objectManager, node);
		}
		else
		{
			Debug.FailedAssert("false", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.Core\\MBEquipmentRoster.cs", "Init", 81);
		}
	}

	public override void Deserialize(MBObjectManager objectManager, XmlNode node)
	{
		base.Deserialize(objectManager, node);
		if (node.Attributes["culture"] != null)
		{
			EquipmentCulture = MBObjectManager.Instance.ReadObjectReferenceFromXml<BasicCultureObject>("culture", node);
		}
		foreach (XmlNode childNode in node.ChildNodes)
		{
			if (childNode.Name == "EquipmentSet")
			{
				InitEquipment(objectManager, childNode);
			}
			if (!(childNode.Name == "Flags"))
			{
				continue;
			}
			foreach (XmlAttribute attribute in childNode.Attributes)
			{
				EquipmentFlags equipmentFlags = (EquipmentFlags)Enum.Parse(typeof(EquipmentFlags), attribute.Name);
				if (bool.Parse(attribute.InnerText))
				{
					EquipmentFlags |= equipmentFlags;
				}
			}
		}
	}

	private void InitEquipment(MBObjectManager objectManager, XmlNode node)
	{
		base.Initialize();
		Equipment.EquipmentType result = Equipment.EquipmentType.Battle;
		if (node.Attributes["equipmentType"] != null)
		{
			if (!Enum.TryParse<Equipment.EquipmentType>(node.Attributes["equipmentType"].Value, out result))
			{
				Debug.FailedAssert("This equipment definition is wrong", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.Core\\MBEquipmentRoster.cs", "InitEquipment", 127);
			}
		}
		else if (node.Attributes["civilian"] != null && bool.Parse(node.Attributes["civilian"].Value))
		{
			result = Equipment.EquipmentType.Civilian;
		}
		Equipment equipment = new Equipment(result);
		equipment.Deserialize(objectManager, node);
		_equipments.Add(equipment);
		AfterInitialized();
	}

	public void AddEquipmentRoster(MBEquipmentRoster equipmentRoster, Equipment.EquipmentType equipmentType)
	{
		foreach (Equipment item in equipmentRoster._equipments.ToList())
		{
			if ((equipmentType == Equipment.EquipmentType.Stealth && item.IsStealth) || (equipmentType == Equipment.EquipmentType.Civilian && item.IsCivilian) || (equipmentType == Equipment.EquipmentType.Battle && item.IsBattle))
			{
				_equipments.Add(item);
			}
		}
		EquipmentFlags = equipmentRoster.EquipmentFlags;
	}

	public void AddOverridenEquipments(MBObjectManager objectManager, List<XmlNode> overridenEquipmentSlots)
	{
		List<Equipment> list = _equipments.ToList();
		_equipments.Clear();
		foreach (Equipment item in list)
		{
			_equipments.Add(item.Clone());
		}
		foreach (XmlNode overridenEquipmentSlot in overridenEquipmentSlots)
		{
			foreach (Equipment equipment in _equipments)
			{
				equipment.DeserializeNode(objectManager, overridenEquipmentSlot);
			}
		}
	}

	public void OrderEquipments()
	{
		_equipments = new MBList<Equipment>(_equipments.OrderByDescending((Equipment eq) => !eq.IsCivilian && !eq.IsStealth));
	}

	public void InitializeDefaultEquipment(string equipmentName)
	{
		if (_equipments[0] == null)
		{
			_equipments[0] = new Equipment(Equipment.EquipmentType.Battle);
		}
		_equipments[0].FillFrom(Game.Current.GetDefaultEquipmentWithName(equipmentName));
	}
}
