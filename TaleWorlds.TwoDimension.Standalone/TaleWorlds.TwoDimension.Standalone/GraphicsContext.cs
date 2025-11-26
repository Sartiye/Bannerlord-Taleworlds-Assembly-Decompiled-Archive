using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension.Standalone.Native;
using TaleWorlds.TwoDimension.Standalone.Native.OpenGL;
using TaleWorlds.TwoDimension.Standalone.Native.Windows;

namespace TaleWorlds.TwoDimension.Standalone;

public class GraphicsContext
{
	public const int MaxFrameRate = 60;

	public readonly int MaxTimeToRenderOneFrame;

	private IntPtr _handleDeviceContext;

	private IntPtr _handleRenderContext;

	private int[] _scissorParameters = new int[4];

	private Matrix4x4 _projectionMatrix = Matrix4x4.Identity;

	private Matrix4x4 _modelMatrix = Matrix4x4.Identity;

	private Matrix4x4 _viewMatrix = Matrix4x4.Identity;

	private Matrix4x4 _modelViewMatrix = Matrix4x4.Identity;

	private Stopwatch _stopwatch;

	private Dictionary<string, Shader> _loadedShaders;

	private VertexArrayObject _simpleVAO;

	private VertexArrayObject _textureVAO;

	private int _screenWidth;

	private int _screenHeight;

	private ResourceDepot _resourceDepot;

	private bool _blendingMode;

	private bool _vertexArrayClientState;

	private bool _textureCoordArrayClientState;

	internal WindowsForm Control { get; set; }

	public static GraphicsContext Active { get; private set; }

	internal Dictionary<string, OpenGLTexture> LoadedTextures { get; private set; }

	public Matrix4x4 ProjectionMatrix
	{
		get
		{
			return _projectionMatrix;
		}
		set
		{
			_projectionMatrix = value;
		}
	}

	public Matrix4x4 ViewMatrix
	{
		get
		{
			return _viewMatrix;
		}
		set
		{
			_viewMatrix = value;
			_modelViewMatrix = _viewMatrix * _modelMatrix;
		}
	}

	public Matrix4x4 ModelMatrix
	{
		get
		{
			return _modelMatrix;
		}
		set
		{
			_modelMatrix = value;
			_modelViewMatrix = _viewMatrix * _modelMatrix;
		}
	}

	public bool IsActive => Active == this;

	public GraphicsContext()
	{
		LoadedTextures = new Dictionary<string, OpenGLTexture>();
		_loadedShaders = new Dictionary<string, Shader>();
		_stopwatch = new Stopwatch();
		MaxTimeToRenderOneFrame = 16;
	}

	public void CreateContext(ResourceDepot resourceDepot)
	{
		_resourceDepot = resourceDepot;
		_handleDeviceContext = User32.GetDC(Control.Handle);
		if (_handleDeviceContext == IntPtr.Zero)
		{
			TaleWorlds.Library.Debug.Print("Can't get device context");
		}
		if (!Opengl32.wglMakeCurrent(_handleDeviceContext, IntPtr.Zero))
		{
			TaleWorlds.Library.Debug.Print("Can't reset context");
		}
		PixelFormatDescriptor ppfd = default(PixelFormatDescriptor);
		Marshal.SizeOf(typeof(PixelFormatDescriptor));
		ppfd.nSize = (ushort)Marshal.SizeOf(typeof(PixelFormatDescriptor));
		ppfd.nVersion = 1;
		ppfd.dwFlags = 32805u;
		ppfd.iPixelType = 0;
		ppfd.cColorBits = 32;
		ppfd.cRedBits = 0;
		ppfd.cRedShift = 0;
		ppfd.cGreenBits = 0;
		ppfd.cGreenShift = 0;
		ppfd.cBlueBits = 0;
		ppfd.cBlueShift = 0;
		ppfd.cAlphaBits = 8;
		ppfd.cAlphaShift = 0;
		ppfd.cAccumBits = 0;
		ppfd.cAccumRedBits = 0;
		ppfd.cAccumGreenBits = 0;
		ppfd.cAccumBlueBits = 0;
		ppfd.cAccumAlphaBits = 0;
		ppfd.cDepthBits = 24;
		ppfd.cStencilBits = 0;
		ppfd.cAuxBuffers = 0;
		ppfd.iLayerType = 0;
		ppfd.bReserved = 0;
		ppfd.dwLayerMask = 0u;
		ppfd.dwVisibleMask = 0u;
		ppfd.dwDamageMask = 0u;
		int iPixelFormat = Gdi32.ChoosePixelFormat(_handleDeviceContext, ref ppfd);
		if (!Gdi32.SetPixelFormat(_handleDeviceContext, iPixelFormat, ref ppfd))
		{
			TaleWorlds.Library.Debug.Print("can't set pixel format");
		}
		_handleRenderContext = Opengl32.wglCreateContext(_handleDeviceContext);
		SetActive();
		Opengl32ARB.LoadExtensions();
		IntPtr handleRenderContext = _handleRenderContext;
		_handleRenderContext = IntPtr.Zero;
		Active = null;
		int[] array = new int[10];
		int num = 0;
		array[num++] = 8337;
		array[num++] = 3;
		array[num++] = 8338;
		array[num++] = 3;
		array[num++] = 37158;
		array[num++] = 1;
		array[num++] = 0;
		_handleRenderContext = Opengl32ARB.wglCreateContextAttribs(_handleDeviceContext, IntPtr.Zero, array);
		SetActive();
		Opengl32.wglDeleteContext(handleRenderContext);
		Opengl32.ShadeModel(ShadingModel.Smooth);
		Opengl32.ClearColor(0f, 0f, 0f, 0f);
		Opengl32.ClearDepth(1.0);
		Opengl32.Disable(Target.DepthTest);
		Opengl32.Hint(3152u, 4354u);
		ProjectionMatrix = Matrix4x4.Identity;
		ModelMatrix = Matrix4x4.Identity;
		ViewMatrix = Matrix4x4.Identity;
		_simpleVAO = VertexArrayObject.Create();
		_textureVAO = VertexArrayObject.CreateWithUVBuffer();
	}

	public void SetActive()
	{
		if (Active != this)
		{
			if (Opengl32.wglMakeCurrent(_handleDeviceContext, _handleRenderContext))
			{
				Active = this;
			}
			else
			{
				TaleWorlds.Library.Debug.Print("Can't activate context");
			}
		}
	}

	public void BeginFrame(int width, int height)
	{
		_stopwatch.Start();
		Resize(width, height);
		Opengl32.Clear(AttribueMask.ColorBufferBit);
		Opengl32.ClearDepth(1.0);
		Opengl32.Disable(Target.DepthTest);
		Opengl32.Disable(Target.SCISSOR_TEST);
		Opengl32.Disable(Target.STENCIL_TEST);
		Opengl32.Disable(Target.Blend);
	}

	public void SwapBuffers()
	{
		int num = (int)_stopwatch.ElapsedMilliseconds;
		int num2 = 0;
		if (MaxTimeToRenderOneFrame > num)
		{
			num2 = MaxTimeToRenderOneFrame - num;
		}
		if (num2 > 0)
		{
			Thread.Sleep(num2);
		}
		Gdi32.SwapBuffers(_handleDeviceContext);
		_stopwatch.Restart();
	}

	public void DestroyContext()
	{
		Opengl32.wglMakeCurrent(IntPtr.Zero, IntPtr.Zero);
		Opengl32.wglDeleteContext(_handleRenderContext);
		User32.ReleaseDC(Control.Handle, _handleDeviceContext);
	}

	public void SetScissor(ScissorTestInfo scissorTestInfo)
	{
		Opengl32.GetInteger(Target.VIEWPORT, _scissorParameters);
		SimpleRectangle simpleRectangle = scissorTestInfo.GetSimpleRectangle();
		Opengl32.Scissor((int)simpleRectangle.X, _scissorParameters[3] - (int)simpleRectangle.Height - (int)simpleRectangle.Y, (int)simpleRectangle.Width, (int)simpleRectangle.Height);
		Opengl32.Enable(Target.SCISSOR_TEST);
	}

	public void ResetScissor()
	{
		Opengl32.Disable(Target.SCISSOR_TEST);
	}

	public Shader GetOrLoadShader(string shaderName)
	{
		if (!_loadedShaders.ContainsKey(shaderName))
		{
			string filePath = _resourceDepot.GetFilePath(shaderName + ".vert");
			string filePath2 = _resourceDepot.GetFilePath(shaderName + ".frag");
			string vertexShaderCode = File.ReadAllText(filePath);
			string fragmentShaderCode = File.ReadAllText(filePath2);
			Shader shader = Shader.CreateShader(this, vertexShaderCode, fragmentShaderCode);
			_loadedShaders.Add(shaderName, shader);
			return shader;
		}
		return _loadedShaders[shaderName];
	}

	public void DrawImage(SimpleMaterial material, in ImageDrawObject drawObject)
	{
		Shader shader = PrepareRender(material, in drawObject.Rectangle);
		DrawImageAux(shader, material, in drawObject);
		VertexArrayObject.UnBind();
		shader.StopUsing();
	}

	public void DrawText(TextMaterial material, in TextDrawObject drawObject)
	{
		Shader shader = PrepareRender(material, in drawObject.Rectangle);
		DrawTextAux(shader, material, in drawObject);
		VertexArrayObject.UnBind();
		shader.StopUsing();
	}

	public void DrawPolygon(PrimitivePolygonMaterial material, in ImageDrawObject drawObject)
	{
		Shader shader = PrepareRender(material, in drawObject.Rectangle);
		DrawPolygonAux(shader, material, in drawObject);
		VertexArrayObject.UnBind();
		shader.StopUsing();
	}

	private Shader PrepareRender(Material material, in Rectangle2D rect)
	{
		Shader orLoadShader = GetOrLoadShader(material.GetType().Name);
		MatrixFrame cachedVisualMatrixFrame = rect.GetCachedVisualMatrixFrame();
		ModelMatrix = new Matrix4x4(cachedVisualMatrixFrame.rotation.s.x, cachedVisualMatrixFrame.rotation.s.y, cachedVisualMatrixFrame.rotation.s.z, cachedVisualMatrixFrame.rotation.s.w, cachedVisualMatrixFrame.rotation.f.x, cachedVisualMatrixFrame.rotation.f.y, cachedVisualMatrixFrame.rotation.f.z, cachedVisualMatrixFrame.rotation.f.w, cachedVisualMatrixFrame.rotation.u.x, cachedVisualMatrixFrame.rotation.u.y, cachedVisualMatrixFrame.rotation.u.z, cachedVisualMatrixFrame.rotation.u.w, cachedVisualMatrixFrame.origin.x, cachedVisualMatrixFrame.origin.y, 0f, cachedVisualMatrixFrame.origin.w);
		orLoadShader.Use();
		Matrix4x4 matrix = _modelMatrix * _viewMatrix * _projectionMatrix;
		orLoadShader.SetMatrix("MVP", matrix);
		return orLoadShader;
	}

	private void DrawImageAux(Shader shader, SimpleMaterial material, in ImageDrawObject drawObject)
	{
		if (material.Texture != null)
		{
			OpenGLTexture texture = material.Texture.PlatformTexture as OpenGLTexture;
			shader.SetTexture("Texture", texture);
		}
		shader.SetBoolean("OverlayEnabled", material.OverlayEnabled);
		if (material.OverlayEnabled)
		{
			OpenGLTexture texture2 = material.OverlayTexture.PlatformTexture as OpenGLTexture;
			shader.SetVector2("StartCoord", material.StartCoordinate);
			shader.SetVector2("Size", material.Size);
			shader.SetTexture("OverlayTexture", texture2);
			shader.SetVector2("OverlayOffset", new Vector2(material.OverlayXOffset, material.OverlayYOffset));
		}
		float value = TaleWorlds.Library.MathF.Clamp(material.HueFactor / 360f, -0.5f, 0.5f);
		float value2 = TaleWorlds.Library.MathF.Clamp(material.SaturationFactor / 360f, -0.5f, 0.5f);
		float value3 = TaleWorlds.Library.MathF.Clamp(material.ValueFactor / 360f, -0.5f, 0.5f);
		shader.SetColor("InputColor", material.Color);
		shader.SetFloat("ColorFactor", material.ColorFactor);
		shader.SetFloat("AlphaFactor", material.AlphaFactor);
		shader.SetFloat("HueFactor", value);
		shader.SetFloat("SaturationFactor", value2);
		shader.SetFloat("ValueFactor", value3);
		_textureVAO.Bind();
		if (material.CircularMaskingEnabled)
		{
			shader.SetBoolean("CircularMaskingEnabled", value: true);
			shader.SetVector2("MaskingCenter", material.CircularMaskingCenter);
			shader.SetFloat("MaskingRadius", material.CircularMaskingRadius);
			shader.SetFloat("MaskingSmoothingRadius", material.CircularMaskingSmoothingRadius);
		}
		else
		{
			shader.SetBoolean("CircularMaskingEnabled", value: false);
		}
		Vector2 vector = new Vector2(drawObject.Uvs.x, drawObject.Uvs.y);
		Vector2 vector2 = new Vector2(drawObject.Uvs.z, drawObject.Uvs.w);
		float[] vertices = new float[8] { 0f, 0f, 0f, 1f, 1f, 1f, 1f, 0f };
		uint[] indices = new uint[6] { 0u, 1u, 2u, 0u, 2u, 3u };
		float[] uvs = new float[8] { vector.X, vector.Y, vector.X, vector2.Y, vector2.X, vector2.Y, vector2.X, vector.Y };
		_textureVAO.LoadVertexData(vertices);
		_textureVAO.LoadUVData(uvs);
		_textureVAO.LoadIndexData(indices);
		DrawElements(indices, material.Blending);
	}

	private void DrawTextAux(Shader shader, TextMaterial textMaterial, in TextDrawObject drawObject)
	{
		if (textMaterial.Texture != null)
		{
			OpenGLTexture texture = textMaterial.Texture.PlatformTexture as OpenGLTexture;
			shader.SetTexture("Texture", texture);
		}
		shader.SetColor("InputColor", textMaterial.Color);
		shader.SetColor("GlowColor", textMaterial.GlowColor);
		shader.SetColor("OutlineColor", textMaterial.OutlineColor);
		shader.SetFloat("OutlineAmount", textMaterial.OutlineAmount);
		shader.SetFloat("ScaleFactor", 1.5f / textMaterial.ScaleFactor);
		shader.SetFloat("SmoothingConstant", textMaterial.SmoothingConstant);
		shader.SetFloat("GlowRadius", textMaterial.GlowRadius);
		shader.SetFloat("Blur", textMaterial.Blur);
		shader.SetFloat("ShadowOffset", textMaterial.ShadowOffset);
		shader.SetFloat("ShadowAngle", textMaterial.ShadowAngle);
		shader.SetFloat("ColorFactor", textMaterial.ColorFactor);
		shader.SetFloat("AlphaFactor", textMaterial.AlphaFactor);
		_textureVAO.Bind();
		_textureVAO.LoadVertexData(drawObject.Text_Vertices);
		_textureVAO.LoadUVData(drawObject.Text_TextureCoordinates);
		_textureVAO.LoadIndexData(drawObject.Text_Indices);
		DrawElements(drawObject.Text_Indices, textMaterial.Blending);
	}

	private void DrawPolygonAux(Shader shader, PrimitivePolygonMaterial material, in ImageDrawObject drawObject)
	{
		Color color = material.Color;
		shader.SetColor("Color", color);
		new Vector2(drawObject.Uvs.x, drawObject.Uvs.y);
		new Vector2(drawObject.Uvs.z, drawObject.Uvs.w);
		float[] vertices = new float[8] { 0f, 0f, 0f, 1f, 1f, 1f, 1f, 0f };
		uint[] indices = new uint[6] { 0u, 1u, 2u, 0u, 2u, 3u };
		_simpleVAO.Bind();
		_textureVAO.LoadVertexData(vertices);
		DrawElements(indices, material.Blending);
	}

	private void DrawElements(uint[] indices, bool blending)
	{
		SetBlending(blending);
		using (new AutoPinner(indices))
		{
			Opengl32.DrawElements(BeginMode.Triangles, indices.Length, DataType.UnsignedInt, null);
		}
	}

	internal void Resize(int width, int height)
	{
		if (!IsActive)
		{
			SetActive();
		}
		_screenWidth = width;
		_screenHeight = height;
		Opengl32.Viewport(0, 0, width, height);
	}

	public void LoadTextureUsing(OpenGLTexture texture, ResourceDepot resourceDepot, string name)
	{
		if (!LoadedTextures.ContainsKey(name))
		{
			texture.LoadFromFile(resourceDepot, name);
			LoadedTextures.Add(name, texture);
		}
		else
		{
			texture.CopyFrom(LoadedTextures[name]);
		}
	}

	public OpenGLTexture LoadTexture(ResourceDepot resourceDepot, string name)
	{
		OpenGLTexture openGLTexture = null;
		if (LoadedTextures.ContainsKey(name))
		{
			openGLTexture = LoadedTextures[name];
		}
		else
		{
			openGLTexture = OpenGLTexture.FromFile(resourceDepot, name);
			LoadedTextures.Add(name, openGLTexture);
		}
		return openGLTexture;
	}

	public OpenGLTexture GetTexture(string textureName)
	{
		OpenGLTexture result = null;
		if (LoadedTextures.ContainsKey(textureName))
		{
			result = LoadedTextures[textureName];
		}
		return result;
	}

	public void SetBlending(bool enable)
	{
		_blendingMode = enable;
		if (_blendingMode)
		{
			Opengl32.Enable(Target.Blend);
			Opengl32ARB.BlendFuncSeparate(BlendingSourceFactor.SourceAlpha, BlendingDestinationFactor.OneMinusSourceAlpha, BlendingSourceFactor.One, BlendingDestinationFactor.One);
		}
		else
		{
			Opengl32.Disable(Target.Blend);
		}
	}

	public void SetVertexArrayClientState(bool enable)
	{
		if (_vertexArrayClientState != enable)
		{
			_vertexArrayClientState = enable;
			if (_vertexArrayClientState)
			{
				Opengl32.EnableClientState(32884u);
			}
			else
			{
				Opengl32.DisableClientState(32884u);
			}
		}
	}

	public void SetTextureCoordArrayClientState(bool enable)
	{
		if (_textureCoordArrayClientState != enable)
		{
			_textureCoordArrayClientState = enable;
			if (_textureCoordArrayClientState)
			{
				Opengl32.EnableClientState(32888u);
			}
			else
			{
				Opengl32.DisableClientState(32888u);
			}
		}
	}
}
