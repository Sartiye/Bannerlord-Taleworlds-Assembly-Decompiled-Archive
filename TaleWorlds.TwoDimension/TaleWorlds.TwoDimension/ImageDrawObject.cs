using System.Runtime.CompilerServices;
using TaleWorlds.Library;

namespace TaleWorlds.TwoDimension;

public struct ImageDrawObject : IDrawObject
{
	public bool IsValid;

	public float Scale;

	public Rectangle2D Rectangle;

	public Vec3 Uvs;

	public static ImageDrawObject Invalid => CreateInvalid();

	bool IDrawObject.IsValid => IsValid;

	Rectangle2D IDrawObject.Rectangle
	{
		get
		{
			return Rectangle;
		}
		set
		{
			Rectangle = value;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static ImageDrawObject CreateInvalid()
	{
		ImageDrawObject result = default(ImageDrawObject);
		result.IsValid = false;
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ImageDrawObject Create(in Rectangle2D rectangle, in Vec2 uvMin, in Vec2 uvMax)
	{
		ImageDrawObject result = default(ImageDrawObject);
		result.IsValid = true;
		result.Scale = 1f;
		result.Uvs = new Vec3(uvMin.x, uvMin.y, uvMax.x, uvMax.y);
		result.Rectangle = rectangle;
		return result;
	}
}
