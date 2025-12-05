using System;

namespace TaleWorlds.TwoDimension.Standalone.Native.OpenGL;

public class OpenGlLoadException : Exception
{
	public OpenGlLoadException()
	{
	}

	public OpenGlLoadException(string message)
		: base(message)
	{
	}

	public OpenGlLoadException(string message, Exception innerException)
		: base(message, innerException)
	{
	}
}
