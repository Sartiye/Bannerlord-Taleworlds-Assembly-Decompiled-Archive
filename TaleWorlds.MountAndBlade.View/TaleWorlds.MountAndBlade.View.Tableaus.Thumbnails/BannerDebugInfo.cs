using System.Text;

namespace TaleWorlds.MountAndBlade.View.Tableaus.Thumbnails;

public struct BannerDebugInfo
{
	public enum SourceTypes
	{
		Undefined,
		Widget,
		Manual
	}

	public SourceTypes SourceType;

	public string SourceName;

	public static BannerDebugInfo CreateManual(string sourceName)
	{
		BannerDebugInfo result = default(BannerDebugInfo);
		result.SourceName = sourceName;
		result.SourceType = SourceTypes.Manual;
		return result;
	}

	public static BannerDebugInfo CreateWidget(string sourceName)
	{
		BannerDebugInfo result = default(BannerDebugInfo);
		result.SourceType = SourceTypes.Widget;
		result.SourceName = sourceName;
		return result;
	}

	public string CreateName()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("type:");
		stringBuilder.Append(GetSourceTypeName(SourceType));
		stringBuilder.Append("name:");
		stringBuilder.Append(SourceName);
		return stringBuilder.ToString();
	}

	private static string GetSourceTypeName(SourceTypes type)
	{
		return type switch
		{
			SourceTypes.Widget => "Wi", 
			SourceTypes.Manual => "Mn", 
			_ => "Un", 
		};
	}

	public override string ToString()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append($"type: {SourceType}_");
		stringBuilder.Append("name: " + SourceName + "_");
		return stringBuilder.ToString();
	}
}
