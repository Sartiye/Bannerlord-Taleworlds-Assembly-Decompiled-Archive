using System;
using System.Runtime.InteropServices;
using System.Security;

namespace TaleWorlds.Starter.DotNetCore.Linux;

internal static class MBDotNet
{
	public const string MainDllName = "libRgl.so";

	public const string DotNetLibraryDllName = "libFairyTale.DotNet.so";

	[DllImport("libRgl.so", CallingConvention = CallingConvention.StdCall, EntryPoint = "WotsMain")]
	[SuppressUnmanagedCodeSecurity]
	public static extern int WotsMainDotNet(string args);

	[DllImport("libFairyTale.DotNet.so", CallingConvention = CallingConvention.StdCall, EntryPoint = "pass_controller_methods")]
	[SuppressUnmanagedCodeSecurity]
	public static extern void PassControllerMethods(Delegate currentDomainInitializer);

	[DllImport("libFairyTale.DotNet.so", CallingConvention = CallingConvention.StdCall, EntryPoint = "pass_managed_initialize_method_pointer")]
	[SuppressUnmanagedCodeSecurity]
	public static extern void PassManagedInitializeMethodPointerDotNet([MarshalAs(UnmanagedType.FunctionPtr)] Delegate initalizer);

	[DllImport("libFairyTale.DotNet.so", CallingConvention = CallingConvention.StdCall, EntryPoint = "pass_managed_library_callback_method_pointers")]
	[SuppressUnmanagedCodeSecurity]
	public static extern void PassManagedEngineCallbackMethodPointersDotNet([MarshalAs(UnmanagedType.FunctionPtr)] Delegate methodDelegate);
}
