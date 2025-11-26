using System;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class DefaultFormationDeploymentPlan : IFormationDeploymentPlan
{
	private WorldFrame _spawnFrame;

	private FormationClass _spawnClass;

	private readonly FormationClass _class;

	private float _plannedWidth;

	private float _plannedDepth;

	private int _plannedFootTroopCount;

	private int _plannedMountedTroopCount;

	public FormationClass Class => _class;

	public FormationClass SpawnClass => _spawnClass;

	public float PlannedWidth => _plannedWidth;

	public float PlannedDepth => _plannedDepth;

	public int PlannedTroopCount => _plannedFootTroopCount + _plannedMountedTroopCount;

	public int PlannedFootTroopCount => _plannedFootTroopCount;

	public int PlannedMountedTroopCount => _plannedMountedTroopCount;

	public bool HasDimensions
	{
		get
		{
			if (_plannedWidth >= 1E-05f)
			{
				return _plannedDepth >= 1E-05f;
			}
			return false;
		}
	}

	public bool HasSignificantMountedTroops => DefaultMissionDeploymentPlan.HasSignificantMountedTroops(_plannedFootTroopCount, _plannedMountedTroopCount);

	public DefaultFormationDeploymentPlan(FormationClass fClass)
	{
		_class = fClass;
		_spawnClass = fClass;
		Clear();
	}

	public bool HasFrame()
	{
		return _spawnFrame.IsValid;
	}

	public FormationDeploymentFlank GetDefaultFlank(int formationTroopCount, int infantryCount, bool spawnWithHorses = false)
	{
		FormationDeploymentFlank formationDeploymentFlank = FormationDeploymentFlank.Count;
		if (!_class.IsMounted() && formationTroopCount == 0)
		{
			return FormationDeploymentFlank.Rear;
		}
		if (HasSignificantMountedTroops && (!spawnWithHorses || infantryCount == 0))
		{
			if (formationTroopCount == 0 || _class == FormationClass.LightCavalry || _class == FormationClass.HorseArcher)
			{
				return FormationDeploymentFlank.Rear;
			}
			return FormationDeploymentFlank.Front;
		}
		switch (_class)
		{
		case FormationClass.Cavalry:
		case FormationClass.HeavyCavalry:
			return FormationDeploymentFlank.Left;
		case FormationClass.HorseArcher:
		case FormationClass.LightCavalry:
			return FormationDeploymentFlank.Right;
		case FormationClass.Ranged:
		case FormationClass.NumberOfRegularFormations:
		case FormationClass.Bodyguard:
		case FormationClass.NumberOfAllFormations:
			return FormationDeploymentFlank.Rear;
		default:
			return FormationDeploymentFlank.Front;
		}
	}

	public FormationDeploymentOrder GetFlankDeploymentOrder(int offset = 0)
	{
		return FormationDeploymentOrder.GetDeploymentOrder(_class, offset);
	}

	public MatrixFrame GetFrame()
	{
		return _spawnFrame.ToGroundMatrixFrame();
	}

	public Vec3 GetPosition()
	{
		return _spawnFrame.Origin.GetGroundVec3();
	}

	public Vec2 GetDirection()
	{
		return _spawnFrame.Rotation.f.AsVec2.Normalized();
	}

	public WorldPosition CreateNewDeploymentWorldPosition(WorldPosition.WorldPositionEnforcedCache worldPositionEnforcedCache)
	{
		return worldPositionEnforcedCache switch
		{
			WorldPosition.WorldPositionEnforcedCache.NavMeshVec3 => new WorldPosition(Mission.Current.Scene, UIntPtr.Zero, _spawnFrame.Origin.GetNavMeshVec3(), hasValidZ: false), 
			WorldPosition.WorldPositionEnforcedCache.GroundVec3 => new WorldPosition(Mission.Current.Scene, UIntPtr.Zero, _spawnFrame.Origin.GetGroundVec3(), hasValidZ: false), 
			_ => _spawnFrame.Origin, 
		};
	}

	public void Clear()
	{
		_plannedWidth = 0f;
		_plannedDepth = 0f;
		_plannedFootTroopCount = 0;
		_plannedMountedTroopCount = 0;
		_spawnFrame = WorldFrame.Invalid;
	}

	public void SetPlannedTroopCount(int footTroopCount, int mountedTroopCount)
	{
		_plannedFootTroopCount = footTroopCount;
		_plannedMountedTroopCount = mountedTroopCount;
	}

	public void SetPlannedDimensions(float width, float depth)
	{
		_plannedWidth = TaleWorlds.Library.MathF.Max(0f, width);
		_plannedDepth = TaleWorlds.Library.MathF.Max(0f, depth);
	}

	public void SetFrame(in WorldFrame frame)
	{
		_spawnFrame = frame;
	}

	public void SetSpawnClass(FormationClass spawnClass)
	{
		_spawnClass = spawnClass;
	}
}
