using System.Collections.Generic;
using NavalDLC.Missions.Objects;
using SandBox;
using SandBox.View.Map;
using SandBox.View.Map.Visuals;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace NavalDLC.View.Map.Visuals;

public class AnchorVisual : MapEntityVisual<AnchorPoint>
{
	private ShipHull _flagshipHull;

	private uint _cachedVersion;

	private List<SailVisual> _sailVisuals = new List<SailVisual>();

	private Scene _mapScene;

	public override CampaignVec2 InteractionPositionForPlayer => base.MapEntity.GetInteractionPosition(MobileParty.MainParty);

	public override MapEntityVisual AttachedTo => null;

	public GameEntity Entity { get; private set; }

	private Scene MapScene
	{
		get
		{
			if (_mapScene == null && Campaign.Current != null && Campaign.Current.MapSceneWrapper != null)
			{
				_mapScene = ((MapScene)Campaign.Current.MapSceneWrapper).Scene;
			}
			return _mapScene;
		}
	}

	public AnchorVisual(AnchorPoint mapEntity)
		: base(mapEntity)
	{
	}

	public override Vec3 GetVisualPosition()
	{
		return base.MapEntity.Position.AsVec3();
	}

	public override bool IsVisibleOrFadingOut()
	{
		return !base.MapEntity.Owner.IsTransitionInProgress;
	}

	public override void OnHover()
	{
		InformationManager.ShowTooltip(typeof(AnchorPoint), base.MapEntity);
	}

	public override bool OnMapClick(bool followModifierUsed)
	{
		MobileParty.MainParty.SetMoveGoToInteractablePoint(base.MapEntity, MobileParty.NavigationType.All);
		return true;
	}

	public override void ReleaseResources()
	{
	}

	public override void OnOpenEncyclopedia()
	{
	}

	public void OnStartup()
	{
		if (Entity != null)
		{
			OnVisualUpdate();
		}
		else
		{
			RefreshGameEntity();
		}
	}

	public void OnRemoved()
	{
		if (PartyBase.MainParty.Ships.Count > 0)
		{
			Entity.SetVisibilityExcludeParents(visible: false);
			return;
		}
		base.MapEntity.ResetPosition();
		Entity?.Remove(111);
		Entity = null;
		ResetVersionCache();
		_sailVisuals.Clear();
	}

	public void OnVisualUpdate()
	{
		Ship flagShip = PartyBase.MainParty.FlagShip;
		if (_flagshipHull == null || _flagshipHull != flagShip.ShipHull)
		{
			if (Entity != null)
			{
				MapScreen.VisualsOfEntities.Remove(Entity.Pointer);
				Entity?.Remove(111);
			}
			RefreshGameEntity();
		}
		else if (flagShip.VersionNo != _cachedVersion)
		{
			UpdateVersionCache();
			NavalDLCViewHelpers.ShipVisualHelper.RefreshShipVisuals(Entity.WeakEntity, flagShip, _sailVisuals);
		}
	}

	private void UpdateVersionCache()
	{
		Ship flagShip = PartyBase.MainParty.FlagShip;
		_cachedVersion = flagShip.VersionNo;
		_flagshipHull = flagShip.ShipHull;
	}

	private void ResetVersionCache()
	{
		_flagshipHull = null;
		_cachedVersion = 0u;
	}

	private void RefreshGameEntity()
	{
		UpdateVersionCache();
		Entity = NavalDLCViewHelpers.ShipVisualHelper.GetFlagshipEntity(PartyBase.MainParty, MapScene);
		NavalDLCViewHelpers.ShipVisualHelper.CollectSailVisuals(Entity.WeakEntity, _sailVisuals);
		Entity.SetVisibilityExcludeParents(visible: false);
		Entity.AddSphereAsBody(new Vec3(0f, 0f, 0f, -1f), 3f, BodyFlags.Moveable | BodyFlags.OnlyCollideWithRaycast);
		UpdateAnchorVisualPosition();
	}

	internal void UpdateAnchorVisualPosition()
	{
		MatrixFrame frame = CalculateAnchorFrame(base.MapEntity);
		Entity.SetFrame(ref frame);
		Entity.SetVisibilityExcludeParents(visible: true);
	}

	private MatrixFrame CalculateAnchorFrame(AnchorPoint anchor)
	{
		Vec2 vec = (anchor.GetInteractionPosition(anchor.Owner).ToVec2() - anchor.Position.ToVec2()).Normalized();
		Vec3 scaleAmountXYZ = Entity.GetLocalScale();
		MatrixFrame identity = MatrixFrame.Identity;
		identity.origin = GetVisualPosition();
		identity.rotation.f.AsVec2 = vec.RightVec();
		identity.rotation.f.NormalizeWithoutChangingZ();
		identity.rotation.Orthonormalize();
		identity.rotation.ApplyScaleLocal(in scaleAmountXYZ);
		return identity;
	}

	private bool CanHaveAnchor()
	{
		if (base.MapEntity.Owner.HasNavalNavigationCapability && base.MapEntity.IsValid)
		{
			return !base.MapEntity.IsDisabled;
		}
		return false;
	}
}
