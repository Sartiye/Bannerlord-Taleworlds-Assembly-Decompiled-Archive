using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;

namespace TaleWorlds.Starter.DotNetCore.Linux;

public class Program
{
	private delegate void ControllerDelegate(Delegate currentDomainInitializer);

	private delegate void InitializerDelegate(Delegate argument);

	private delegate void StartMethodDelegate(string args);

	private static string[] _args;

	private static int Starter()
	{
		try
		{
			Assembly.LoadFrom("TaleWorlds.Library.dll");
			Assembly.LoadFrom("TaleWorlds.DotNet.dll").GetType("TaleWorlds.DotNet.Controller").GetMethod("SetEngineMethodsAsDotNet")
				.Invoke(null, new object[3]
				{
					new ControllerDelegate(MBDotNet.PassControllerMethods),
					new InitializerDelegate(MBDotNet.PassManagedInitializeMethodPointerDotNet),
					new InitializerDelegate(MBDotNet.PassManagedEngineCallbackMethodPointersDotNet)
				});
		}
		catch (FileNotFoundException ex)
		{
			Console.WriteLine("Exception: " + ex);
			Console.WriteLine("Fusion Log: " + ex.FusionLog);
			Console.WriteLine("Exception detailed: " + ex.ToString());
			if (ex.InnerException != null)
			{
				Console.WriteLine("Inner Exception: " + ex.InnerException);
			}
			Console.WriteLine("Press a key to continue...");
			Console.ReadKey();
		}
		catch (Exception ex2)
		{
			Console.WriteLine("Exception: " + ex2);
			if (ex2.InnerException != null)
			{
				Console.WriteLine("Inner Exception: " + ex2.InnerException);
			}
			Console.WriteLine("Press a key to continue...");
			Console.ReadKey();
		}
		string text = "";
		for (int i = 0; i < _args.Length; i++)
		{
			string text2 = _args[i];
			text += text2;
			if (i + 1 < _args.Length)
			{
				text += " ";
			}
		}
		return MBDotNet.WotsMainDotNet(text);
	}

	[STAThread]
	public static int Main(string[] args)
	{
		Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
		Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
		CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
		CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
		_args = args;
		return Starter();
	}
}
