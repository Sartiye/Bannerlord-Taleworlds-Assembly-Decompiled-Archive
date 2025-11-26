using System;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;

namespace TaleWorlds.Library;

public static class MachineId
{
	private static string MachineIdString;

	public static void Initialize()
	{
		if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			MachineIdString = "nonwindows";
		}
		else
		{
			MachineIdString = ProcessId();
		}
	}

	public static int AsInteger()
	{
		if (!string.IsNullOrEmpty(MachineIdString))
		{
			return BitConverter.ToInt32(Encoding.ASCII.GetBytes(MachineIdString), 0);
		}
		return 0;
	}

	private static string ProcessId()
	{
		return string.Concat(string.Concat("" + GetMotherboardIdentifier(), GetCpuIdentifier()), GetDiskIdentifier());
	}

	private static string GetMotherboardIdentifier()
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		string text = "";
		try
		{
			ManagementObjectCollection instances = new ManagementClass("win32_baseboard").GetInstances();
			try
			{
				ManagementObjectEnumerator enumerator = instances.GetEnumerator();
				try
				{
					while (enumerator.MoveNext())
					{
						string text2 = (((ManagementBaseObject)(ManagementObject)enumerator.Current)["SerialNumber"] as string).Trim(new char[1] { ' ' });
						text += text2.Replace("-", "");
					}
					return text;
				}
				finally
				{
					((IDisposable)enumerator)?.Dispose();
				}
			}
			finally
			{
				((IDisposable)instances)?.Dispose();
			}
		}
		catch (Exception)
		{
			return "";
		}
	}

	private static string GetCpuIdentifier()
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		string text = "";
		try
		{
			ManagementObjectCollection instances = new ManagementClass("win32_processor").GetInstances();
			try
			{
				ManagementObjectEnumerator enumerator = instances.GetEnumerator();
				try
				{
					while (enumerator.MoveNext())
					{
						if (((ManagementBaseObject)(ManagementObject)enumerator.Current)["ProcessorId"] is string text2)
						{
							string text3 = text2.Trim(new char[1] { ' ' });
							text += text3.Replace("-", "");
						}
					}
					return text;
				}
				finally
				{
					((IDisposable)enumerator)?.Dispose();
				}
			}
			finally
			{
				((IDisposable)instances)?.Dispose();
			}
		}
		catch (Exception)
		{
			return "";
		}
	}

	private static string GetDiskIdentifier()
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Expected O, but got Unknown
		string text = "";
		try
		{
			ManagementObjectCollection instances = new ManagementClass("win32_diskdrive").GetInstances();
			try
			{
				ManagementObjectEnumerator enumerator = instances.GetEnumerator();
				try
				{
					while (enumerator.MoveNext())
					{
						ManagementObject val = (ManagementObject)enumerator.Current;
						if (string.Compare(((ManagementBaseObject)val)["InterfaceType"] as string, "IDE", StringComparison.InvariantCultureIgnoreCase) == 0)
						{
							string text2 = (((ManagementBaseObject)val)["SerialNumber"] as string).Trim(new char[1] { ' ' });
							text += text2.Replace("-", "");
						}
					}
					return text;
				}
				finally
				{
					((IDisposable)enumerator)?.Dispose();
				}
			}
			finally
			{
				((IDisposable)instances)?.Dispose();
			}
		}
		catch (Exception)
		{
			return "";
		}
	}
}
