using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using TaleWorlds.Engine;

namespace TaleWorlds.MountAndBlade.ListedServer.MapServer;

public abstract class ArchivedMap
{
	public const string ZipLockedKey = "ZIP_LOCKED";

	private static Dictionary<string, InactiveArchivedMap> inactiveMapArchives = new Dictionary<string, InactiveArchivedMap>();

	public abstract Stream Stream { get; }

	public string Name { get; private set; }

	public ArchivedMap(string name)
	{
		Name = name;
	}

	public static CachedArchivedMap FromCurrent()
	{
		string strValue = MultiplayerOptions.OptionType.Map.GetStrValue();
		return new CachedArchivedMap(ZipMap(strValue), strValue);
	}

	public static InactiveArchivedMap FromMap(string mapName)
	{
		if (!inactiveMapArchives.TryGetValue(mapName, out var value))
		{
			value = new InactiveArchivedMap(mapName);
			inactiveMapArchives[mapName] = value;
		}
		return value;
	}

	protected static FileStream ZipMap(string mapName)
	{
		string? directoryName = System.IO.Path.GetDirectoryName(Utilities.GetFullFilePathOfScene(mapName));
		string tempFileName = GetTempFileName(mapName);
		ModLogger.Log($"Creating archive for map '{mapName}' to '{tempFileName}'");
		ZipFile.CreateFromDirectory(directoryName, tempFileName);
		ModLogger.Log("Created archive for map '" + mapName + "'");
		return new FileStream(tempFileName, FileMode.Open, FileAccess.Read, FileShare.Read);
	}

	protected static string GetTempFileName(string anyIdentifier)
	{
		return System.IO.Path.Combine(System.IO.Path.GetTempPath(), "BL_" + anyIdentifier + "_" + Guid.NewGuid());
	}

	public static void DisposeInactiveMaps()
	{
		foreach (InactiveArchivedMap value in inactiveMapArchives.Values)
		{
			value.DisposeTempFile();
		}
	}
}
