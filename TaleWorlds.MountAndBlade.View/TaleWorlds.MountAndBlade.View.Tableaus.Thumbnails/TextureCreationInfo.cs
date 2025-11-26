using TaleWorlds.Engine;

namespace TaleWorlds.MountAndBlade.View.Tableaus.Thumbnails;

public struct TextureCreationInfo
{
	public bool IsValid;

	public bool CreatedNewTexture;

	public bool UsingExistingTexture;

	public Texture Texture;

	public bool IsSuccess
	{
		get
		{
			if (IsValid)
			{
				if (!CreatedNewTexture)
				{
					return UsingExistingTexture;
				}
				return true;
			}
			return false;
		}
	}

	public bool IsFail
	{
		get
		{
			if (IsValid && !CreatedNewTexture)
			{
				return !UsingExistingTexture;
			}
			return false;
		}
	}

	public static TextureCreationInfo WithNewTexture(Texture texture = null)
	{
		TextureCreationInfo result = default(TextureCreationInfo);
		result.IsValid = true;
		result.CreatedNewTexture = true;
		result.Texture = texture;
		return result;
	}

	public static TextureCreationInfo WithExistingTexture(Texture texture)
	{
		TextureCreationInfo result = default(TextureCreationInfo);
		result.IsValid = true;
		result.UsingExistingTexture = true;
		result.Texture = texture;
		return result;
	}

	public static TextureCreationInfo Fail()
	{
		TextureCreationInfo result = default(TextureCreationInfo);
		result.IsValid = true;
		return result;
	}
}
