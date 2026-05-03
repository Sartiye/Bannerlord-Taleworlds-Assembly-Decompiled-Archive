using System;
using System.IO;

namespace TaleWorlds.MountAndBlade.ListedServer.MapServer;

public class CachedArchivedMap : ArchivedMap, IDisposable
{
	private FileStream _fileStream;

	private bool disposedValue;

	public override Stream Stream => new FileStream(_fileStream.Name, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

	internal CachedArchivedMap(FileStream fileStream, string name)
		: base(name)
	{
		_fileStream = fileStream;
	}

	protected virtual void Dispose(bool disposing)
	{
		if (disposedValue)
		{
			return;
		}
		if (disposing)
		{
			string name = _fileStream.Name;
			_fileStream.Dispose();
			try
			{
				File.Delete(name);
			}
			catch (Exception)
			{
				ModLogger.Warn("Failed to delete temp file of map '" + base.Name + "' at " + name);
			}
		}
		_fileStream = null;
		disposedValue = true;
	}

	public void Dispose()
	{
		Dispose(disposing: true);
	}
}
