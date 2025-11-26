using System;

namespace TaleWorlds.Library;

[Serializable]
public struct Transformation
{
	public Vec3 Origin;

	public Mat3 Rotation;

	public Vec3 Scale;

	public static Transformation Identity => new Transformation(new Vec3(0f, 0f, 0f, 1f), Mat3.Identity, Vec3.One);

	public MatrixFrame AsMatrixFrame
	{
		get
		{
			MatrixFrame result = default(MatrixFrame);
			result.origin = Origin;
			result.rotation = Rotation;
			result.rotation.ApplyScaleLocal(in Scale);
			result.Fill();
			return result;
		}
	}

	public Transformation(Vec3 origin, Mat3 rotation, Vec3 scale)
	{
		Origin = origin;
		Rotation = rotation;
		Scale = scale;
	}

	public static Transformation CreateFromMatrixFrame(MatrixFrame matrixFrame)
	{
		Mat3 rotation = matrixFrame.rotation;
		Vec3 scaleVector = matrixFrame.rotation.GetScaleVector();
		Vec3 scaleAmountXYZ = new Vec3(1f / scaleVector.x, 1f / scaleVector.y, 1f / scaleVector.z);
		rotation.ApplyScaleLocal(in scaleAmountXYZ);
		return new Transformation(matrixFrame.origin, rotation, scaleVector);
	}

	public static Transformation CreateFromRotation(Mat3 rotation)
	{
		return new Transformation(Vec3.Zero, rotation, Vec3.One);
	}

	public Vec3 TransformToParent(Vec3 v)
	{
		return AsMatrixFrame.TransformToParent(in v);
	}

	public Transformation TransformToParent(Transformation t)
	{
		MatrixFrame asMatrixFrame = AsMatrixFrame;
		MatrixFrame m = t.AsMatrixFrame;
		return CreateFromMatrixFrame(asMatrixFrame.TransformToParent(in m));
	}

	public Vec3 TransformToLocal(Vec3 v)
	{
		return AsMatrixFrame.TransformToLocal(in v);
	}

	public Transformation TransformToLocal(Transformation t)
	{
		MatrixFrame asMatrixFrame = AsMatrixFrame;
		MatrixFrame m = t.AsMatrixFrame;
		return CreateFromMatrixFrame(asMatrixFrame.TransformToLocal(in m));
	}

	public void Rotate(float radian, Vec3 axis)
	{
		Transformation transformation = this;
		transformation.Scale = Vec3.One;
		MatrixFrame asMatrixFrame = transformation.AsMatrixFrame;
		asMatrixFrame.Rotate(radian, in axis);
		Rotation = asMatrixFrame.rotation;
		Origin = asMatrixFrame.origin;
	}

	public static bool operator ==(Transformation t1, Transformation t2)
	{
		if (t1.Origin == t2.Origin && t1.Rotation == t2.Rotation)
		{
			return t1.Scale == t2.Scale;
		}
		return false;
	}

	public void ApplyScale(Vec3 vec3)
	{
		Scale.x *= vec3.x;
		Scale.y *= vec3.y;
		Scale.z *= vec3.z;
	}

	public static bool operator !=(Transformation t1, Transformation t2)
	{
		if (!(t1.Origin != t2.Origin) && !(t1.Rotation != t2.Rotation))
		{
			return t1.Scale != t2.Scale;
		}
		return true;
	}

	public override bool Equals(object obj)
	{
		return this == (Transformation)obj;
	}

	public override int GetHashCode()
	{
		return base.GetHashCode();
	}

	public override string ToString()
	{
		string text = "Transformation:\n";
		text = text + "Origin: " + Origin.x + ", " + Origin.y + ", " + Origin.z + "\n";
		text += "Rotation:\n";
		text += Rotation.ToString();
		return text + "Scale: " + Scale.x + ", " + Scale.y + ", " + Scale.z + "\n";
	}
}
