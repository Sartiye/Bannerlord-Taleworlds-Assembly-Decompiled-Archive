using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using TaleWorlds.ObjectSystem;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class CreateCorpseAgent : GameNetworkMessage
{
	public int AgentIndex { get; private set; }

	public int MountAgentIndex { get; private set; }

	public NetworkCommunicator Peer { get; private set; }

	public BasicCharacterObject Character { get; private set; }

	public Monster Monster { get; private set; }

	public MissionEquipment MissionEquipment { get; private set; }

	public Equipment SpawnEquipment { get; private set; }

	public BodyProperties BodyPropertiesValue { get; private set; }

	public int BodyPropertiesSeed { get; private set; }

	public bool IsFemale { get; private set; }

	public int TeamIndex { get; private set; }

	public Vec3 Position { get; private set; }

	public Vec2 Direction { get; private set; }

	public int FormationIndex { get; private set; }

	public bool IsPlayerAgent { get; private set; }

	public uint ClothingColor1 { get; private set; }

	public uint ClothingColor2 { get; private set; }

	public int CorpsesToFadeIndex { get; private set; }

	public int AttachedWeaponCount { get; private set; }

	public MBList<MissionWeapon> AttachedWeapons { get; private set; }

	public MBList<sbyte> AttachedWeaponsBoneIndices { get; private set; }

	public MBList<MatrixFrame> AttachedWeaponsLocalFrames { get; private set; }

	public ActionIndexCache DeathActionIndex { get; private set; }

	public CreateCorpseAgent(int agentIndex, BasicCharacterObject character, Monster monster, Equipment spawnEquipment, MissionEquipment missionEquipment, BodyProperties bodyPropertiesValue, int bodyPropertiesSeed, bool isFemale, int agentTeamIndex, int agentFormationIndex, uint clothingColor1, uint clothingColor2, int mountAgentIndex, Equipment mountAgentSpawnEquipment, bool isPlayerAgent, Vec3 position, Vec2 direction, NetworkCommunicator peer, ActionIndexCache deathActionIndex, int corpsesToFadeIndex = -1, int attachedWeaponCount = 0, MBList<MissionWeapon> attachedWeapons = null, MBList<sbyte> attachedWeaponsBoneIndices = null, MBList<MatrixFrame> attachedWeaponsLocalFrames = null)
	{
		AgentIndex = agentIndex;
		MountAgentIndex = mountAgentIndex;
		Peer = peer;
		Character = character;
		Monster = monster;
		SpawnEquipment = new Equipment();
		MissionEquipment = new MissionEquipment();
		for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex < EquipmentIndex.NumAllWeaponSlots; equipmentIndex++)
		{
			MissionEquipment[equipmentIndex] = missionEquipment[equipmentIndex];
		}
		for (EquipmentIndex equipmentIndex2 = EquipmentIndex.NumAllWeaponSlots; equipmentIndex2 < EquipmentIndex.ArmorItemEndSlot; equipmentIndex2++)
		{
			SpawnEquipment[equipmentIndex2] = spawnEquipment.GetEquipmentFromSlot(equipmentIndex2);
		}
		if (MountAgentIndex >= 0)
		{
			SpawnEquipment[EquipmentIndex.ArmorItemEndSlot] = mountAgentSpawnEquipment[EquipmentIndex.ArmorItemEndSlot];
			SpawnEquipment[EquipmentIndex.HorseHarness] = mountAgentSpawnEquipment[EquipmentIndex.HorseHarness];
		}
		else
		{
			SpawnEquipment[EquipmentIndex.ArmorItemEndSlot] = default(EquipmentElement);
			SpawnEquipment[EquipmentIndex.HorseHarness] = default(EquipmentElement);
		}
		BodyPropertiesValue = bodyPropertiesValue;
		BodyPropertiesSeed = bodyPropertiesSeed;
		IsFemale = isFemale;
		TeamIndex = agentTeamIndex;
		Position = position;
		Direction = direction;
		FormationIndex = agentFormationIndex;
		ClothingColor1 = clothingColor1;
		ClothingColor2 = clothingColor2;
		IsPlayerAgent = isPlayerAgent;
		CorpsesToFadeIndex = corpsesToFadeIndex;
		AttachedWeaponCount = attachedWeaponCount;
		AttachedWeapons = attachedWeapons;
		AttachedWeaponsBoneIndices = attachedWeaponsBoneIndices;
		AttachedWeaponsLocalFrames = attachedWeaponsLocalFrames;
		DeathActionIndex = deathActionIndex;
	}

	public CreateCorpseAgent()
	{
		AttachedWeapons = new MBList<MissionWeapon>();
		AttachedWeaponsBoneIndices = new MBList<sbyte>();
		AttachedWeaponsLocalFrames = new MBList<MatrixFrame>();
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		Character = (BasicCharacterObject)GameNetworkMessage.ReadObjectReferenceFromPacket(MBObjectManager.Instance, CompressionBasic.GUIDCompressionInfo, ref bufferReadValid);
		Monster = (Monster)GameNetworkMessage.ReadObjectReferenceFromPacket(MBObjectManager.Instance, CompressionBasic.GUIDCompressionInfo, ref bufferReadValid);
		AgentIndex = GameNetworkMessage.ReadAgentIndexFromPacket(ref bufferReadValid);
		MountAgentIndex = GameNetworkMessage.ReadAgentIndexFromPacket(ref bufferReadValid);
		Peer = GameNetworkMessage.ReadNetworkPeerReferenceFromPacket(ref bufferReadValid);
		SpawnEquipment = new Equipment();
		MissionEquipment = new MissionEquipment();
		for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex < EquipmentIndex.NumAllWeaponSlots; equipmentIndex++)
		{
			MissionEquipment[equipmentIndex] = ModuleNetworkData.ReadWeaponReferenceFromPacket(MBObjectManager.Instance, ref bufferReadValid);
		}
		for (EquipmentIndex equipmentIndex2 = EquipmentIndex.NumAllWeaponSlots; equipmentIndex2 < EquipmentIndex.NumEquipmentSetSlots; equipmentIndex2++)
		{
			SpawnEquipment.AddEquipmentToSlotWithoutAgent(equipmentIndex2, ModuleNetworkData.ReadItemReferenceFromPacket(MBObjectManager.Instance, ref bufferReadValid));
		}
		IsPlayerAgent = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
		BodyPropertiesSeed = ((!IsPlayerAgent) ? GameNetworkMessage.ReadIntFromPacket(CompressionBasic.RandomSeedCompressionInfo, ref bufferReadValid) : 0);
		BodyPropertiesValue = GameNetworkMessage.ReadBodyPropertiesFromPacket(ref bufferReadValid);
		IsFemale = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
		TeamIndex = GameNetworkMessage.ReadTeamIndexFromPacket(ref bufferReadValid);
		Position = GameNetworkMessage.ReadVec3FromPacket(CompressionBasic.PositionCompressionInfo, ref bufferReadValid);
		Direction = GameNetworkMessage.ReadVec2FromPacket(CompressionBasic.UnitVectorCompressionInfo, ref bufferReadValid).Normalized();
		FormationIndex = GameNetworkMessage.ReadIntFromPacket(CompressionMission.FormationClassCompressionInfo, ref bufferReadValid);
		ClothingColor1 = GameNetworkMessage.ReadUintFromPacket(CompressionBasic.ColorCompressionInfo, ref bufferReadValid);
		ClothingColor2 = GameNetworkMessage.ReadUintFromPacket(CompressionBasic.ColorCompressionInfo, ref bufferReadValid);
		CorpsesToFadeIndex = GameNetworkMessage.ReadAgentIndexFromPacket(ref bufferReadValid);
		AttachedWeaponCount = GameNetworkMessage.ReadIntFromPacket(CompressionMission.AttachedWeaponsCompressionInfo, ref bufferReadValid);
		for (int i = 0; i < AttachedWeaponCount; i++)
		{
			AttachedWeapons.Add(MissionWeapon.Invalid);
			AttachedWeaponsBoneIndices.Add(-1);
			AttachedWeaponsLocalFrames.Add(MatrixFrame.Zero);
		}
		for (int j = 0; j < AttachedWeaponCount; j++)
		{
			AttachedWeapons[j] = ModuleNetworkData.ReadWeaponReferenceFromPacket(MBObjectManager.Instance, ref bufferReadValid);
		}
		for (int k = 0; k < AttachedWeaponCount; k++)
		{
			AttachedWeaponsBoneIndices[k] = (sbyte)GameNetworkMessage.ReadIntFromPacket(CompressionMission.BoneIndexCompressionInfo, ref bufferReadValid);
		}
		for (int l = 0; l < AttachedWeaponCount; l++)
		{
			Vec3 o = GameNetworkMessage.ReadVec3FromPacket(CompressionBasic.LocalPositionCompressionInfo, ref bufferReadValid);
			Mat3 rot = GameNetworkMessage.ReadRotationMatrixFromPacket(ref bufferReadValid);
			if (bufferReadValid)
			{
				AttachedWeaponsLocalFrames[l] = new MatrixFrame(in rot, in o);
			}
		}
		DeathActionIndex = new ActionIndexCache(GameNetworkMessage.ReadIntFromPacket(CompressionBasic.ActionCodeCompressionInfo, ref bufferReadValid));
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteObjectReferenceToPacket(Character, CompressionBasic.GUIDCompressionInfo);
		GameNetworkMessage.WriteObjectReferenceToPacket(Monster, CompressionBasic.GUIDCompressionInfo);
		GameNetworkMessage.WriteAgentIndexToPacket(AgentIndex);
		GameNetworkMessage.WriteAgentIndexToPacket(MountAgentIndex);
		GameNetworkMessage.WriteNetworkPeerReferenceToPacket(Peer);
		for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex < EquipmentIndex.NumAllWeaponSlots; equipmentIndex++)
		{
			ModuleNetworkData.WriteWeaponReferenceToPacket(MissionEquipment[equipmentIndex]);
		}
		for (EquipmentIndex equipmentIndex2 = EquipmentIndex.NumAllWeaponSlots; equipmentIndex2 < EquipmentIndex.NumEquipmentSetSlots; equipmentIndex2++)
		{
			ModuleNetworkData.WriteItemReferenceToPacket(SpawnEquipment.GetEquipmentFromSlot(equipmentIndex2));
		}
		GameNetworkMessage.WriteBoolToPacket(IsPlayerAgent);
		if (!IsPlayerAgent)
		{
			GameNetworkMessage.WriteIntToPacket(BodyPropertiesSeed, CompressionBasic.RandomSeedCompressionInfo);
		}
		GameNetworkMessage.WriteBodyPropertiesToPacket(BodyPropertiesValue);
		GameNetworkMessage.WriteBoolToPacket(IsFemale);
		GameNetworkMessage.WriteTeamIndexToPacket(TeamIndex);
		GameNetworkMessage.WriteVec3ToPacket(Position, CompressionBasic.PositionCompressionInfo);
		GameNetworkMessage.WriteVec2ToPacket(Direction, CompressionBasic.UnitVectorCompressionInfo);
		GameNetworkMessage.WriteIntToPacket(FormationIndex, CompressionMission.FormationClassCompressionInfo);
		GameNetworkMessage.WriteUintToPacket(ClothingColor1, CompressionBasic.ColorCompressionInfo);
		GameNetworkMessage.WriteUintToPacket(ClothingColor2, CompressionBasic.ColorCompressionInfo);
		GameNetworkMessage.WriteAgentIndexToPacket(CorpsesToFadeIndex);
		GameNetworkMessage.WriteIntToPacket(AttachedWeaponCount, CompressionMission.AttachedWeaponsCompressionInfo);
		for (int i = 0; i < AttachedWeaponCount; i++)
		{
			ModuleNetworkData.WriteWeaponReferenceToPacket(AttachedWeapons[i]);
		}
		for (int j = 0; j < AttachedWeaponCount; j++)
		{
			GameNetworkMessage.WriteIntToPacket(AttachedWeaponsBoneIndices[j], CompressionMission.BoneIndexCompressionInfo);
		}
		for (int k = 0; k < AttachedWeaponCount; k++)
		{
			GameNetworkMessage.WriteVec3ToPacket(AttachedWeaponsLocalFrames[k].origin, CompressionBasic.LocalPositionCompressionInfo);
			GameNetworkMessage.WriteRotationMatrixToPacket(AttachedWeaponsLocalFrames[k].rotation);
		}
		GameNetworkMessage.WriteIntToPacket(DeathActionIndex.Index, CompressionBasic.ActionCodeCompressionInfo);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.Agents;
	}

	protected override string OnGetLogFormat()
	{
		return "Create an agent with index: " + AgentIndex + ((Peer != null) ? (", belonging to peer with Name: " + Peer.UserName + ", and peer-index: " + Peer.Index) : "") + ((MountAgentIndex == -1) ? "" : (", owning a mount with index: " + MountAgentIndex));
	}
}
