using System.Collections.Concurrent;
using System.IO;
using System.Text;
using Microsoft.Extensions.Caching.Memory;

namespace TaleWorlds.MountAndBlade.DedicatedCustomServer.WebPanel;

public class CacheTextWriter : TextWriter
{
	private TextWriter _console;

	public IMemoryCache _cache;

	public override Encoding Encoding => Encoding.UTF8;

	public CacheTextWriter(IMemoryCache cache, TextWriter console)
	{
		_console = console;
		_cache = cache;
		ConcurrentQueue<string> value = new ConcurrentQueue<string>();
		_cache.Set("queue", value);
	}

	public override void WriteLine(string value)
	{
		ConcurrentQueue<string> concurrentQueue = _cache.Get<ConcurrentQueue<string>>("queue");
		base.WriteLine(value);
		concurrentQueue.Enqueue(value);
		while (concurrentQueue.Count > 200)
		{
			concurrentQueue.TryDequeue(out var _);
		}
		_console.WriteLine(value);
	}
}
