namespace TaleWorlds.MountAndBlade.ListedServer.MapServer;

public static class ListedServerMapServerCommandManager
{
	[ConsoleCommandMethod("dispose_helper_temp_files", "Disposes most of the temp files created by the download helper")]
	private static void DisposeHelperTempFiles()
	{
		ArchivedMap.DisposeInactiveMaps();
		ModLogger.Log("Disposed inactive map temp files successfully");
	}
}
