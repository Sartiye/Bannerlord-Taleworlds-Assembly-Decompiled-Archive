using System;
using TaleWorlds.Starter.Library;

namespace TaleWorlds.Starter.DotNetCore;

public class Program
{
	[STAThread]
	public static int Main(string[] args)
	{
		return TaleWorlds.Starter.Library.Program.Main(args);
	}
}
