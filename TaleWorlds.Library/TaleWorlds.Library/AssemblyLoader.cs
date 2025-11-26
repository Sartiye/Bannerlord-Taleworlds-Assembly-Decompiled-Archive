using System;
using System.Collections.Generic;
using System.Reflection;

namespace TaleWorlds.Library;

public static class AssemblyLoader
{
	private static List<Assembly> _loadedAssemblies;

	static AssemblyLoader()
	{
		_loadedAssemblies = new List<Assembly>();
		Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
		foreach (Assembly item in assemblies)
		{
			_loadedAssemblies.Add(item);
		}
		AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
	}

	public static void Initialize()
	{
	}

	public static Assembly LoadFrom(string assemblyFile, bool show_error = true)
	{
		Assembly assembly = null;
		Debug.Print("Loading assembly: " + assemblyFile + "\n");
		try
		{
			if (ApplicationPlatform.CurrentRuntimeLibrary == Runtime.DotNetCore)
			{
				try
				{
					assembly = Assembly.LoadFrom(assemblyFile);
				}
				catch (Exception)
				{
					assembly = null;
				}
				if (assembly != null && !_loadedAssemblies.Contains(assembly))
				{
					_loadedAssemblies.Add(assembly);
					AssemblyName[] referencedAssemblies = assembly.GetReferencedAssemblies();
					for (int i = 0; i < referencedAssemblies.Length; i++)
					{
						string text = referencedAssemblies[i].Name + ".dll";
						if (!text.StartsWith("System") && !text.StartsWith("mscorlib") && !text.StartsWith("netstandard"))
						{
							LoadFrom(text);
						}
					}
				}
			}
			else
			{
				assembly = Assembly.LoadFrom(assemblyFile);
			}
		}
		catch (Exception ex2)
		{
			if (show_error)
			{
				string lpText = "Cannot load: " + assemblyFile;
				string lpCaption = "ERROR";
				Debug.ShowMessageBox(lpText, lpCaption, 4u);
			}
			Debug.Print("ERROR: " + assemblyFile + ": " + ex2.Message);
			if (ex2.InnerException != null)
			{
				Debug.Print($"ERROR: {assemblyFile}: {ex2.InnerException}");
			}
		}
		Debug.Print("Assembly load result: " + ((assembly == null) ? "NULL" : "SUCCESS"));
		return assembly;
	}

	private static Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
	{
		Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
		foreach (Assembly assembly in assemblies)
		{
			if (assembly.FullName == args.Name)
			{
				return assembly;
			}
		}
		if (ApplicationPlatform.CurrentRuntimeLibrary == Runtime.Mono && ApplicationPlatform.IsPlatformWindows())
		{
			return LoadFrom(args.Name.Split(new char[1] { ',' }, StringSplitOptions.RemoveEmptyEntries)[0] + ".dll", show_error: false);
		}
		return null;
	}
}
