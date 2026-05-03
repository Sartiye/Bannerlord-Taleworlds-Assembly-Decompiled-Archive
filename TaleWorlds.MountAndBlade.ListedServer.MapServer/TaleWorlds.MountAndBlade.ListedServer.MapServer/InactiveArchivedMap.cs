using System;
using System.IO;
using System.Threading;

namespace TaleWorlds.MountAndBlade.ListedServer.MapServer;

public class InactiveArchivedMap : ArchivedMap
{
	private readonly object _zippingLock = new object();

	private string filePath;

	internal bool TempFileExists
	{
		get
		{
			if (filePath != null)
			{
				return File.Exists(filePath);
			}
			return false;
		}
	}

	public override Stream Stream
	{
		get
		{
			if (!TempFileExists)
			{
				return SaveZippedMap();
			}
			return new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
		}
	}

	internal InactiveArchivedMap(string name)
		: base(name)
	{
	}

	private FileStream SaveZippedMap()
	{
		if (Monitor.TryEnter(_zippingLock))
		{
			try
			{
				ModLogger.Log("Zipping inactive map '" + base.Name + "'");
				FileStream fileStream = ArchivedMap.ZipMap(base.Name);
				filePath = fileStream.Name;
				return fileStream;
			}
			finally
			{
				Monitor.Exit(_zippingLock);
			}
		}
		InvalidOperationException ex = new InvalidOperationException("Already zipping map '" + base.Name + "', try later");
		ex.Data["ZIP_LOCKED"] = true;
		throw ex;
	}

	internal void DisposeTempFile()
	{
		if (TempFileExists)
		{
			File.Delete(filePath);
		}
	}
}
