using TaleWorlds.Library;

namespace TaleWorlds.Engine;

public struct WorldFrame
{
	public Mat3 Rotation;

	public WorldPosition Origin;

	public static readonly WorldFrame Invalid = new WorldFrame(Mat3.Identity, WorldPosition.Invalid);

	public bool IsValid => Origin.IsValid;

	public WorldFrame(Mat3 rotation, WorldPosition origin)
	{
		Rotation = rotation;
		Origin = origin;
	}

	public MatrixFrame ToGroundMatrixFrame()
	{
		ref Mat3 rotation = ref Rotation;
		Vec3 o = Origin.GetGroundVec3();
		return new MatrixFrame(in rotation, in o);
	}

	public MatrixFrame ToGroundMatrixFrameMT()
	{
		ref Mat3 rotation = ref Rotation;
		Vec3 o = Origin.GetGroundVec3MT();
		return new MatrixFrame(in rotation, in o);
	}

	public MatrixFrame ToNavMeshMatrixFrame()
	{
		ref Mat3 rotation = ref Rotation;
		Vec3 o = Origin.GetNavMeshVec3();
		return new MatrixFrame(in rotation, in o);
	}
}
