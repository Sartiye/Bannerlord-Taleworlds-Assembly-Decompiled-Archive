using System;
using TaleWorlds.Library;

namespace NavalDLC.Missions.ShipControl;

public struct NavalVec
{
	private Vec2 _deltaPosition;

	private float _deltaOrientation;

	private float _deltaSpeed;

	public Vec2 DeltaPosition => _deltaPosition;

	public float DeltaOrientation => _deltaOrientation;

	public float DeltaSpeed => _deltaSpeed;

	public static NavalVec Zero => new NavalVec(in Vec2.Zero, 0f);

	public NavalVec(in Vec2 deltaPosition, float deltaRotation, float deltaSpeed = 0f)
	{
		_deltaPosition = deltaPosition;
		_deltaOrientation = deltaRotation;
		_deltaSpeed = deltaSpeed;
	}

	public NavalVec(in Vec2 deltaPosition)
	{
		_deltaPosition = deltaPosition;
		_deltaOrientation = 0f;
		_deltaSpeed = 0f;
	}

	public void ClampAngle()
	{
		_deltaOrientation = TaleWorlds.Library.MathF.Clamp(_deltaOrientation, -System.MathF.PI, System.MathF.PI);
	}

	public static NavalVec operator +(in NavalVec vec1, in NavalVec vec2)
	{
		Vec2 deltaPosition = vec1.DeltaPosition + vec2.DeltaPosition;
		return new NavalVec(in deltaPosition, vec1.DeltaOrientation + vec2.DeltaOrientation, vec1.DeltaSpeed + vec2.DeltaSpeed);
	}

	public static NavalVec operator -(in NavalVec vec1, in NavalVec vec2)
	{
		Vec2 deltaPosition = vec1.DeltaPosition - vec2.DeltaPosition;
		return new NavalVec(in deltaPosition, vec1.DeltaOrientation - vec2.DeltaOrientation, vec1.DeltaSpeed - vec2.DeltaSpeed);
	}

	public static NavalVec operator *(in NavalVec vector, float scalar)
	{
		Vec2 deltaPosition = vector.DeltaPosition * scalar;
		return new NavalVec(in deltaPosition, vector.DeltaOrientation * scalar, vector.DeltaSpeed * scalar);
	}

	public static NavalVec operator *(float scalar, in NavalVec vector)
	{
		Vec2 deltaPosition = scalar * vector.DeltaPosition;
		return new NavalVec(in deltaPosition, scalar * vector.DeltaOrientation, scalar * vector.DeltaSpeed);
	}

	public static NavalVec operator *(in Vec3 vector, in NavalVec nVector)
	{
		Vec2 deltaPosition = vector.x * nVector.DeltaPosition;
		return new NavalVec(in deltaPosition, vector.y * nVector.DeltaOrientation, vector.z * nVector.DeltaSpeed);
	}

	public static NavalVec operator *(in NavalVec nVector, in Vec3 vector)
	{
		Vec2 deltaPosition = nVector.DeltaPosition * vector.x;
		return new NavalVec(in deltaPosition, nVector.DeltaOrientation * vector.y, nVector.DeltaSpeed * vector.z);
	}

	public static NavalVec operator /(in NavalVec vector, float scalar)
	{
		Vec2 deltaPosition = vector.DeltaPosition / scalar;
		return new NavalVec(in deltaPosition, vector.DeltaOrientation / scalar, vector.DeltaSpeed / scalar);
	}
}
