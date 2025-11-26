using System;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.View;

public static class CraftingPieceCollectionElementViewExtensions
{
	public static MatrixFrame GetCraftingPieceFrameForInventory(this CraftingPiece craftingPiece)
	{
		MatrixFrame identity = MatrixFrame.Identity;
		Mat3 identity2 = Mat3.Identity;
		float num = 0.85f;
		Vec3 vec = new Vec3(0f, 0f, 0f, -1f);
		MetaMesh copy = MetaMesh.GetCopy(craftingPiece.MeshName);
		if (copy != null)
		{
			identity2.RotateAboutSide(-System.MathF.PI / 2f);
			identity2.RotateAboutForward(-System.MathF.PI / 4f);
			Vec3 vec2 = new Vec3(1000000f, 1000000f, 1000000f);
			Vec3 vec3 = new Vec3(-1000000f, -1000000f, -1000000f);
			for (int i = 0; i != copy.MeshCount; i++)
			{
				Vec3 boundingBoxMin = copy.GetMeshAtIndex(i).GetBoundingBoxMin();
				Vec3 boundingBoxMax = copy.GetMeshAtIndex(i).GetBoundingBoxMax();
				Vec3[] array = new Vec3[8];
				Vec3 v = new Vec3(boundingBoxMin.x, boundingBoxMin.y, boundingBoxMin.z);
				array[0] = identity2.TransformToParent(in v);
				v = new Vec3(boundingBoxMin.x, boundingBoxMin.y, boundingBoxMax.z);
				array[1] = identity2.TransformToParent(in v);
				v = new Vec3(boundingBoxMin.x, boundingBoxMax.y, boundingBoxMin.z);
				array[2] = identity2.TransformToParent(in v);
				v = new Vec3(boundingBoxMin.x, boundingBoxMax.y, boundingBoxMax.z);
				array[3] = identity2.TransformToParent(in v);
				v = new Vec3(boundingBoxMax.x, boundingBoxMin.y, boundingBoxMin.z);
				array[4] = identity2.TransformToParent(in v);
				v = new Vec3(boundingBoxMax.x, boundingBoxMin.y, boundingBoxMax.z);
				array[5] = identity2.TransformToParent(in v);
				v = new Vec3(boundingBoxMax.x, boundingBoxMax.y, boundingBoxMin.z);
				array[6] = identity2.TransformToParent(in v);
				v = new Vec3(boundingBoxMax.x, boundingBoxMax.y, boundingBoxMax.z);
				array[7] = identity2.TransformToParent(in v);
				for (int j = 0; j < 8; j++)
				{
					vec2 = Vec3.Vec3Min(vec2, array[j]);
					vec3 = Vec3.Vec3Max(vec3, array[j]);
				}
			}
			float num2 = 1f;
			Vec3 vec4 = (vec2 + vec3) * 0.5f;
			float num3 = TaleWorlds.Library.MathF.Max(vec3.x - vec2.x, vec3.y - vec2.y);
			float num4 = num * num2 / num3;
			identity.origin -= vec4 * num4;
			identity.origin += vec;
			identity.rotation = identity2;
			identity.rotation.ApplyScaleLocal(num4);
			identity.origin.z -= 5f;
		}
		return identity;
	}
}
