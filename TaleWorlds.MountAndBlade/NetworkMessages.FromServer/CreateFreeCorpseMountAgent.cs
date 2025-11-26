using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using TaleWorlds.ObjectSystem;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class CreateFreeCorpseMountAgent : GameNetworkMessage
{
	public int AgentIndex { get; private set; }

	public EquipmentElement HorseItem { get; private set; }

	public EquipmentElement HorseHarnessItem { get; private set; }

	public Vec3 Position { get; private set; }

	public Vec2 Direction { get; private set; }

	public int CorpsesToFadeIndex { get; private set; }

	public int AttachedWeaponCount { get; private set; }

	public MBList<MissionWeapon> AttachedWeapons { get; private set; }

	public MBList<sbyte> AttachedWeaponsBoneIndices { get; private set; }

	public MBList<MatrixFrame> AttachedWeaponsLocalFrames { get; private set; }

	public ActionIndexCache DeathActionIndex { get; private set; }

	public CreateFreeCorpseMountAgent(Agent agent, Vec3 position, Vec2 direction, ActionIndexCache deathActionIndex, int attachedWeaponCount = 0, MBList<MissionWeapon> attachedWeapons = null, MBList<sbyte> attachedWeaponsBoneIndices = null, MBList<MatrixFrame> attachedWeaponsLocalFrames = null)
	{
		AgentIndex = agent.Index;
		HorseItem = agent.SpawnEquipment.GetEquipmentFromSlot(EquipmentIndex.ArmorItemEndSlot);
		HorseHarnessItem = agent.SpawnEquipment.GetEquipmentFromSlot(EquipmentIndex.HorseHarness);
		Position = position;
		Direction = direction.Normalized();
		CorpsesToFadeIndex = -1;
		AttachedWeaponCount = attachedWeaponCount;
		AttachedWeapons = attachedWeapons;
		AttachedWeaponsBoneIndices = attachedWeaponsBoneIndices;
		AttachedWeaponsLocalFrames = attachedWeaponsLocalFrames;
		DeathActionIndex = deathActionIndex;
	}

	public CreateFreeCorpseMountAgent(int agentIndex, EquipmentElement horseItem, EquipmentElement horseHarnessItem, Vec3 position, Vec2 direction, int corpsesToFadeIndex, ActionIndexCache deathActionIndex, int attachedWeaponCount = 0, MBList<MissionWeapon> attachedWeapons = null, MBList<sbyte> attachedWeaponsBoneIndices = null, MBList<MatrixFrame> attachedWeaponsLocalFrames = null)
	{
		AgentIndex = agentIndex;
		HorseItem = horseItem;
		HorseHarnessItem = horseHarnessItem;
		Position = position;
		Direction = direction.Normalized();
		CorpsesToFadeIndex = corpsesToFadeIndex;
		AttachedWeaponCount = attachedWeaponCount;
		AttachedWeapons = attachedWeapons;
		AttachedWeaponsBoneIndices = attachedWeaponsBoneIndices;
		AttachedWeaponsLocalFrames = attachedWeaponsLocalFrames;
		DeathActionIndex = deathActionIndex;
	}

	public CreateFreeCorpseMountAgent()
	{
		AttachedWeapons = new MBList<MissionWeapon>();
		AttachedWeaponsBoneIndices = new MBList<sbyte>();
		AttachedWeaponsLocalFrames = new MBList<MatrixFrame>();
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		AgentIndex = GameNetworkMessage.ReadAgentIndexFromPacket(ref bufferReadValid);
		HorseItem = ModuleNetworkData.ReadItemReferenceFromPacket(Game.Current.ObjectManager, ref bufferReadValid);
		HorseHarnessItem = ModuleNetworkData.ReadItemReferenceFromPacket(Game.Current.ObjectManager, ref bufferReadValid);
		Position = GameNetworkMessage.ReadVec3FromPacket(CompressionBasic.PositionCompressionInfo, ref bufferReadValid);
		Direction = GameNetworkMessage.ReadVec2FromPacket(CompressionBasic.UnitVectorCompressionInfo, ref bufferReadValid);
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
		GameNetworkMessage.WriteAgentIndexToPacket(AgentIndex);
		ModuleNetworkData.WriteItemReferenceToPacket(HorseItem);
		ModuleNetworkData.WriteItemReferenceToPacket(HorseHarnessItem);
		GameNetworkMessage.WriteVec3ToPacket(Position, CompressionBasic.PositionCompressionInfo);
		GameNetworkMessage.WriteVec2ToPacket(Direction, CompressionBasic.UnitVectorCompressionInfo);
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
		return "Create a mount-agent with index: " + AgentIndex;
	}
}
