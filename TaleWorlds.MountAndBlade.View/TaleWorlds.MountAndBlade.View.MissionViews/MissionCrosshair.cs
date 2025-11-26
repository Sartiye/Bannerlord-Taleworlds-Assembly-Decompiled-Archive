using System;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.View.MissionViews;

[DefaultView]
public class MissionCrosshair : MissionView
{
	private GameEntity[] _crosshairEntities;

	private GameEntity[] _arrowEntities;

	private float[] _gadgetOpacities;

	private const int GadgetCount = 7;

	public override void OnMissionScreenInitialize()
	{
		base.OnMissionScreenInitialize();
		_crosshairEntities = new GameEntity[3];
		_arrowEntities = new GameEntity[4];
		_gadgetOpacities = new float[7];
		for (int i = 0; i < 3; i++)
		{
			_crosshairEntities[i] = GameEntity.CreateEmpty(base.Mission.Scene);
			string text = i switch
			{
				1 => "crosshair_left", 
				0 => "crosshair_top", 
				_ => "crosshair_right", 
			};
			MetaMesh copy = MetaMesh.GetCopy(text);
			int meshCount = copy.MeshCount;
			for (int j = 0; j < meshCount; j++)
			{
				Mesh meshAtIndex = copy.GetMeshAtIndex(j);
				meshAtIndex.SetMeshRenderOrder(200);
				meshAtIndex.VisibilityMask = VisibilityMaskFlags.Final;
			}
			_crosshairEntities[i].AddComponent(copy);
			MatrixFrame frame = MatrixFrame.Identity;
			_crosshairEntities[i].Name = text;
			_crosshairEntities[i].SetFrame(ref frame);
			_crosshairEntities[i].SetVisibilityExcludeParents(visible: false);
		}
		for (int k = 0; k < 4; k++)
		{
			_arrowEntities[k] = GameEntity.CreateEmpty(base.Mission.Scene);
			string text2 = k switch
			{
				2 => "arrow_down", 
				1 => "arrow_right", 
				0 => "arrow_up", 
				_ => "arrow_left", 
			};
			MetaMesh copy2 = MetaMesh.GetCopy(text2);
			int meshCount2 = copy2.MeshCount;
			for (int l = 0; l < meshCount2; l++)
			{
				Mesh meshAtIndex2 = copy2.GetMeshAtIndex(l);
				meshAtIndex2.SetMeshRenderOrder(200);
				meshAtIndex2.VisibilityMask = VisibilityMaskFlags.Final;
			}
			_arrowEntities[k].AddComponent(copy2);
			MatrixFrame frame2 = MatrixFrame.Identity;
			_arrowEntities[k].Name = text2;
			_arrowEntities[k].SetFrame(ref frame2);
			_arrowEntities[k].SetVisibilityExcludeParents(visible: false);
		}
	}

	public override void OnMissionScreenTick(float dt)
	{
		base.OnMissionScreenTick(dt);
		bool flag = false;
		float[] array = new float[8];
		for (int i = 0; i < 7; i++)
		{
			array[i] = 0f;
		}
		if (MBEditor.EditModeEnabled && (_crosshairEntities[0] == null || _arrowEntities[0] == null))
		{
			return;
		}
		_crosshairEntities[0].SetVisibilityExcludeParents(visible: false);
		_crosshairEntities[1].SetVisibilityExcludeParents(visible: false);
		_crosshairEntities[2].SetVisibilityExcludeParents(visible: false);
		_arrowEntities[0].SetVisibilityExcludeParents(visible: false);
		_arrowEntities[1].SetVisibilityExcludeParents(visible: false);
		_arrowEntities[2].SetVisibilityExcludeParents(visible: false);
		_arrowEntities[3].SetVisibilityExcludeParents(visible: false);
		if (base.Mission.Mode == MissionMode.Conversation || base.Mission.Mode == MissionMode.CutScene)
		{
			return;
		}
		_ = base.MissionScreen.CombatCamera.Near;
		float num = 4.7f + (base.MissionScreen.CombatCamera.HorizontalFov - 0.64f) * 7.14f;
		if (base.Mission.MainAgent != null)
		{
			Agent mainAgent = base.Mission.MainAgent;
			float num2 = base.MissionScreen.CameraViewAngle * (System.MathF.PI / 180f);
			float b = TaleWorlds.Library.MathF.Tan((mainAgent.CurrentAimingError + mainAgent.CurrentAimingTurbulance) * (0.5f / TaleWorlds.Library.MathF.Tan(num2 * 0.5f)));
			new Vec2(0.5f, 0.375f);
			Vec2 vec = new Vec2(0f, b);
			MatrixFrame frame = base.MissionScreen.CombatCamera.Frame;
			frame.Elevate(-5f);
			ref Mat3 rotation = ref frame.rotation;
			Vec3 scaleAmountXYZ = new Vec3(num, num, num);
			rotation.ApplyScaleLocal(in scaleAmountXYZ);
			frame.Strafe(vec.x);
			frame.Advance(vec.y);
			_crosshairEntities[0].SetFrame(ref frame);
			Vec2 vec2 = vec;
			vec2.RotateCCW(System.MathF.PI * 13f / 18f);
			MatrixFrame frame2 = base.MissionScreen.CombatCamera.Frame;
			frame2.Elevate(-5f);
			ref Mat3 rotation2 = ref frame2.rotation;
			scaleAmountXYZ = new Vec3(num, num, num);
			rotation2.ApplyScaleLocal(in scaleAmountXYZ);
			frame2.Strafe(vec2.x);
			frame2.Advance(vec2.y);
			_crosshairEntities[1].SetFrame(ref frame2);
			Vec2 vec3 = vec;
			vec3.RotateCCW(System.MathF.PI * -13f / 18f);
			MatrixFrame frame3 = base.MissionScreen.CombatCamera.Frame;
			frame3.Elevate(-5f);
			ref Mat3 rotation3 = ref frame3.rotation;
			scaleAmountXYZ = new Vec3(num, num, num);
			rotation3.ApplyScaleLocal(in scaleAmountXYZ);
			frame3.Strafe(vec3.x);
			frame3.Advance(vec3.y);
			_crosshairEntities[2].SetFrame(ref frame3);
			MatrixFrame frame4 = base.MissionScreen.CombatCamera.Frame;
			frame4.Elevate(-5f);
			ref Mat3 rotation4 = ref frame4.rotation;
			scaleAmountXYZ = new Vec3(num, num, num);
			rotation4.ApplyScaleLocal(in scaleAmountXYZ);
			frame4.Strafe(0f);
			frame4.Advance(0.07499999f);
			_arrowEntities[0].SetFrame(ref frame4);
			MatrixFrame frame5 = base.MissionScreen.CombatCamera.Frame;
			frame5.Elevate(-5f);
			ref Mat3 rotation5 = ref frame5.rotation;
			scaleAmountXYZ = new Vec3(num, num, num);
			rotation5.ApplyScaleLocal(in scaleAmountXYZ);
			frame5.Strafe(0.14999998f);
			frame5.Advance(-0.025000006f);
			_arrowEntities[1].SetFrame(ref frame5);
			MatrixFrame frame6 = base.MissionScreen.CombatCamera.Frame;
			frame6.Elevate(-5f);
			ref Mat3 rotation6 = ref frame6.rotation;
			scaleAmountXYZ = new Vec3(num, num, num);
			rotation6.ApplyScaleLocal(in scaleAmountXYZ);
			frame6.Strafe(0f);
			frame6.Advance(-0.07499999f);
			_arrowEntities[2].SetFrame(ref frame6);
			MatrixFrame frame7 = base.MissionScreen.CombatCamera.Frame;
			frame7.Elevate(-5f);
			ref Mat3 rotation7 = ref frame7.rotation;
			scaleAmountXYZ = new Vec3(num, num, num);
			rotation7.ApplyScaleLocal(in scaleAmountXYZ);
			frame7.Strafe(-0.15f);
			frame7.Advance(-0.025000006f);
			_arrowEntities[3].SetFrame(ref frame7);
			WeaponInfo wieldedWeaponInfo = mainAgent.GetWieldedWeaponInfo(Agent.HandIndex.MainHand);
			float numberToCheck = MBMath.WrapAngle(mainAgent.LookDirection.AsVec2.RotationInRadians - mainAgent.GetMovementDirection().RotationInRadians);
			if (wieldedWeaponInfo.IsValid && !base.MissionScreen.IsViewingCharacter())
			{
				if (wieldedWeaponInfo.IsRangedWeapon && BannerlordConfig.DisplayTargetingReticule)
				{
					Vec2 bodyRotationConstraint = mainAgent.GetBodyRotationConstraint();
					if (base.Mission.MainAgent.MountAgent == null || MBMath.IsBetween(numberToCheck, bodyRotationConstraint.x, bodyRotationConstraint.y))
					{
						array[0] = 0.9f;
						array[1] = 0.9f;
						array[2] = 0.9f;
					}
					else if (base.Mission.MainAgent.MountAgent != null && !MBMath.IsBetween(numberToCheck, bodyRotationConstraint.x, bodyRotationConstraint.y) && (bodyRotationConstraint.x < -0.1f || bodyRotationConstraint.y > 0.1f))
					{
						flag = true;
					}
				}
				else if (wieldedWeaponInfo.IsMeleeWeapon)
				{
					Agent.ActionCodeType currentActionType = mainAgent.GetCurrentActionType(1);
					Agent.UsageDirection currentActionDirection = mainAgent.GetCurrentActionDirection(1);
					if (BannerlordConfig.DisplayAttackDirection && (currentActionType == Agent.ActionCodeType.ReadyMelee || currentActionDirection != Agent.UsageDirection.None))
					{
						if (currentActionType == Agent.ActionCodeType.ReadyMelee)
						{
							switch (mainAgent.AttackDirection)
							{
							case Agent.UsageDirection.AttackUp:
								array[3] = 0.7f;
								break;
							case Agent.UsageDirection.AttackRight:
								array[4] = 0.7f;
								break;
							case Agent.UsageDirection.AttackDown:
								array[5] = 0.7f;
								break;
							case Agent.UsageDirection.AttackLeft:
								array[6] = 0.7f;
								break;
							}
						}
						else
						{
							flag = true;
							switch (currentActionDirection)
							{
							case Agent.UsageDirection.AttackEnd:
								array[3] = 0.7f;
								break;
							case Agent.UsageDirection.DefendRight:
								array[4] = 0.7f;
								break;
							case Agent.UsageDirection.DefendDown:
								array[5] = 0.7f;
								break;
							case Agent.UsageDirection.DefendLeft:
								array[6] = 0.7f;
								break;
							}
						}
					}
					else if (BannerlordConfig.DisplayAttackDirection || BannerlordConfig.AttackDirectionControl == 0)
					{
						Agent.UsageDirection usageDirection = mainAgent.PlayerAttackDirection();
						switch (usageDirection)
						{
						case Agent.UsageDirection.AttackUp:
							array[3] = 0.7f;
							break;
						case Agent.UsageDirection.AttackRight:
							array[4] = 0.7f;
							break;
						case Agent.UsageDirection.AttackDown:
							array[5] = 0.7f;
							break;
						case Agent.UsageDirection.AttackLeft:
							if (usageDirection == Agent.UsageDirection.AttackLeft)
							{
								array[6] = 0.7f;
							}
							break;
						}
					}
				}
			}
		}
		for (int j = 0; j < 7; j++)
		{
			float num3 = dt;
			num3 = ((j >= 3) ? (num3 * 3f) : (num3 * 5f));
			if (array[j] > _gadgetOpacities[j])
			{
				_gadgetOpacities[j] += 1.2f * num3;
				if (_gadgetOpacities[j] > array[j])
				{
					_gadgetOpacities[j] = array[j];
				}
			}
			else if (array[j] < _gadgetOpacities[j])
			{
				_gadgetOpacities[j] -= num3;
				if (_gadgetOpacities[j] < array[j])
				{
					_gadgetOpacities[j] = array[j];
				}
			}
			int num4 = (int)(255f * _gadgetOpacities[j]);
			if (num4 > 0)
			{
				if (j < 3)
				{
					_crosshairEntities[j].SetVisibilityExcludeParents(visible: true);
				}
				else
				{
					_arrowEntities[j - 3].SetVisibilityExcludeParents(visible: true);
				}
			}
			num4 <<= 24;
			if (j < 3)
			{
				Mesh firstMesh = _crosshairEntities[j].GetFirstMesh();
				if (flag)
				{
					firstMesh.Color = 0xFF0000u | (uint)num4;
				}
				else
				{
					firstMesh.Color = 0xFFFFFFu | (uint)num4;
				}
			}
			else
			{
				Mesh firstMesh2 = _arrowEntities[j - 3].GetFirstMesh();
				if (flag)
				{
					firstMesh2.Color = 0x33AAFFu | (uint)num4;
				}
				else
				{
					firstMesh2.Color = 0xFFBB11u | (uint)num4;
				}
			}
		}
	}
}
