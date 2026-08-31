using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension.Standalone.Native.Windows;

namespace TaleWorlds.TwoDimension.Standalone;

public class DirectXGraphicsContext : IDisposable
{
	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	private struct SimpleMaterialCB
	{
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
		public float[] InputColor;

		public float ColorFactor;

		public float AlphaFactor;

		public float HueFactor;

		public float SaturationFactor;

		public float ValueFactor;

		public int OverlayEnabled;

		public float StartCoordX;

		public float StartCoordY;

		public float SizeX;

		public float SizeY;

		public float OverlayOffsetX;

		public float OverlayOffsetY;

		public int CircularMaskingEnabled;

		public float MaskingCenterX;

		public float MaskingCenterY;

		public float MaskingRadius;

		public float MaskingSmoothingRadius;

		public float _pad0;

		public float _pad1;

		public float _pad2;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	private struct TextMaterialCB
	{
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
		public float[] InputColor;

		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
		public float[] GlowColor;

		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
		public float[] OutlineColor;

		public float OutlineAmount;

		public float ScaleFactor;

		public float SmoothingConstant;

		public float GlowRadius;

		public float Blur;

		public float ShadowOffset;

		public float ShadowAngle;

		public float ColorFactor;

		public float AlphaFactor;

		public float _pad0;

		public float _pad1;

		public float _pad2;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	private struct PrimitivePolygonCB
	{
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
		public float[] Color;
	}

	public const int MaxFrameRate = 60;

	public readonly int MaxTimeToRenderOneFrame;

	private const int FailedRenderFramesFatalThreshold = 180;

	private IntPtr _device;

	private IntPtr _context;

	private IntPtr _swapChain;

	private IntPtr _renderTargetView;

	private IntPtr _offscreenRT;

	private IntPtr _blendStateAlpha;

	private IntPtr _blendStateOpaque;

	private IntPtr _rasterizerScissor;

	private IntPtr _rasterizerNoScissor;

	private IntPtr _samplerLinear;

	private IntPtr _samplerLinearClamp;

	private DirectXVertexBuffer _vertexBuffer;

	private IntPtr _cbMVP;

	private IntPtr _cbMaterial;

	private Dictionary<string, DirectXShader> _loadedShaders;

	private MatrixFrame _modelMatrix = MatrixFrame.Identity.Filled();

	private MatrixFrame _viewMatrix = MatrixFrame.Identity.Filled();

	private MatrixFrame _projectionMatrix = MatrixFrame.Identity.Filled();

	private int _screenWidth;

	private int _screenHeight;

	private bool _scissorEnabled;

	private bool _blendingEnabled;

	private bool _anyInvalidMatricesThisFrame;

	private int _failedRenderFrames;

	private bool _deviceLost;

	private bool _isShuttingDown;

	private Stopwatch _stopwatch;

	private ResourceDepot _resourceDepot;

	private static readonly Guid IID_ID3D11Texture2D = new Guid("6F15AAF2-D208-4E89-9AB4-489535D34F9C");

	internal Dictionary<string, DirectXTexture> LoadedTextures { get; private set; }

	public MatrixFrame ProjectionMatrix
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

	public MatrixFrame ViewMatrix
	{
		get
		{
			return _viewMatrix;
		}
		set
		{
			_viewMatrix = value;
		}
	}

	public MatrixFrame ModelMatrix
	{
		get
		{
			return _modelMatrix;
		}
		set
		{
			_modelMatrix = value;
		}
	}

	public static DirectXGraphicsContext Active { get; private set; }

	public IntPtr DeviceHandle => _device;

	public IntPtr DeviceContextHandle => _context;

	public bool IsDeviceLost => _deviceLost;

	public bool IsLayeredWindow { get; set; }

	public DirectXGraphicsContext()
	{
		_loadedShaders = new Dictionary<string, DirectXShader>();
		LoadedTextures = new Dictionary<string, DirectXTexture>();
		_stopwatch = new Stopwatch();
		MaxTimeToRenderOneFrame = 16;
	}

	public void CreateContext(IntPtr hwnd, ResourceDepot resourceDepot)
	{
		_resourceDepot = resourceDepot;
		IntPtr intPtr = SelectBestAdapter();
		int driverType = ((!(intPtr != IntPtr.Zero)) ? 1 : 0);
		int[] array = new int[1] { 45056 };
		int pFeatureLevel;
		if (IsLayeredWindow)
		{
			int num = D3D11Native.D3D11CreateDevice(intPtr, driverType, IntPtr.Zero, 32u, array, array.Length, 7, out _device, out pFeatureLevel, out _context);
			if (intPtr != IntPtr.Zero)
			{
				ComRelease.Release(intPtr);
			}
			if (num < 0)
			{
				StandaloneApplicationUtility.TerminateWithMessageBox("DirectX error", $"D3D11CreateDevice failed (0x{num:X8}).\n" + "Your system may not support DirectX 11. Please update your graphics drivers.");
				return;
			}
			_screenWidth = 1;
			_screenHeight = 1;
			CreateOffscreenRenderTarget();
		}
		else
		{
			DXGI_SWAP_CHAIN_DESC dXGI_SWAP_CHAIN_DESC = default(DXGI_SWAP_CHAIN_DESC);
			dXGI_SWAP_CHAIN_DESC.BufferDesc = new DXGI_MODE_DESC
			{
				Width = 0u,
				Height = 0u,
				Format = 28u,
				RefreshRate = new DXGI_RATIONAL
				{
					Numerator = 0u,
					Denominator = 1u
				}
			};
			dXGI_SWAP_CHAIN_DESC.SampleDesc = new DXGI_SAMPLE_DESC
			{
				Count = 1u,
				Quality = 0u
			};
			dXGI_SWAP_CHAIN_DESC.BufferUsage = 32u;
			dXGI_SWAP_CHAIN_DESC.BufferCount = 1u;
			dXGI_SWAP_CHAIN_DESC.OutputWindow = hwnd;
			dXGI_SWAP_CHAIN_DESC.Windowed = 1;
			dXGI_SWAP_CHAIN_DESC.SwapEffect = 0u;
			dXGI_SWAP_CHAIN_DESC.Flags = 0u;
			DXGI_SWAP_CHAIN_DESC pSwapChainDesc = dXGI_SWAP_CHAIN_DESC;
			int num = D3D11Native.D3D11CreateDeviceAndSwapChain(intPtr, driverType, IntPtr.Zero, 32u, array, array.Length, 7, ref pSwapChainDesc, out _swapChain, out _device, out pFeatureLevel, out _context);
			if (intPtr != IntPtr.Zero)
			{
				ComRelease.Release(intPtr);
			}
			if (num < 0)
			{
				StandaloneApplicationUtility.TerminateWithMessageBox("DirectX error", $"D3D11CreateDeviceAndSwapChain failed (0x{num:X8}).\n" + "Your system may not support DirectX 11. Please update your graphics drivers.");
				return;
			}
			CreateRenderTargetView();
		}
		Watchdog.LogProperty("crash_tags.txt", "Runtime", "D3D11FeatureLevel", $"0x{pFeatureLevel:X4}");
		Active = this;
		CreateRenderStates();
		CreateConstantBuffers();
		_vertexBuffer = DirectXVertexBuffer.Create(_device);
		if (_vertexBuffer == null)
		{
			StandaloneApplicationUtility.TerminateWithMessageBox("DirectX error", "Failed to create vertex buffers.");
			return;
		}
		ProjectionMatrix = MatrixFrame.Identity.Filled();
		ViewMatrix = MatrixFrame.Identity.Filled();
		ModelMatrix = MatrixFrame.Identity.Filled();
	}

	private void CreateOffscreenRenderTarget()
	{
		if (_renderTargetView != IntPtr.Zero)
		{
			ComRelease.Release(_renderTargetView);
			_renderTargetView = IntPtr.Zero;
		}
		if (_offscreenRT != IntPtr.Zero)
		{
			ComRelease.Release(_offscreenRT);
			_offscreenRT = IntPtr.Zero;
		}
		D3D11_TEXTURE2D_DESC d3D11_TEXTURE2D_DESC = default(D3D11_TEXTURE2D_DESC);
		d3D11_TEXTURE2D_DESC.Width = (uint)_screenWidth;
		d3D11_TEXTURE2D_DESC.Height = (uint)_screenHeight;
		d3D11_TEXTURE2D_DESC.MipLevels = 1u;
		d3D11_TEXTURE2D_DESC.ArraySize = 1u;
		d3D11_TEXTURE2D_DESC.Format = 87u;
		d3D11_TEXTURE2D_DESC.SampleDesc = new DXGI_SAMPLE_DESC
		{
			Count = 1u,
			Quality = 0u
		};
		d3D11_TEXTURE2D_DESC.Usage = 0u;
		d3D11_TEXTURE2D_DESC.BindFlags = 32u;
		d3D11_TEXTURE2D_DESC.CPUAccessFlags = 0u;
		d3D11_TEXTURE2D_DESC.MiscFlags = 0u;
		D3D11_TEXTURE2D_DESC desc = d3D11_TEXTURE2D_DESC;
		if (D3D11Device.CreateTexture2DEmpty(_device, ref desc, out _offscreenRT) >= 0)
		{
			D3D11Device.CreateRenderTargetView(_device, _offscreenRT, out _renderTargetView);
			_ = 0;
		}
	}

	private static IntPtr SelectBestAdapter()
	{
		IntPtr factory = IntPtr.Zero;
		try
		{
			DXGI.CreateDXGIFactory(ref DXGI.IID_IDXGIFactory, out factory);
			if (factory == IntPtr.Zero)
			{
				return IntPtr.Zero;
			}
			IntPtr intPtr = IntPtr.Zero;
			IntPtr intPtr2 = IntPtr.Zero;
			ulong num = 0uL;
			IntPtr adapter;
			for (uint num2 = 0u; DXGIFactory.EnumAdapters(factory, num2, out adapter) == 0; num2++)
			{
				DXGIAdapter.GetDesc(adapter, out var desc);
				ulong num3 = (ulong)desc.DedicatedVideoMemory;
				bool flag = desc.VendorId == 5140 && desc.DeviceId == 140;
				if (!flag && num3 > num)
				{
					if (intPtr != IntPtr.Zero)
					{
						ComRelease.Release(intPtr);
					}
					intPtr = adapter;
					num = num3;
					if (intPtr2 != IntPtr.Zero)
					{
						ComRelease.Release(intPtr2);
						intPtr2 = IntPtr.Zero;
					}
					Watchdog.LogProperty("crash_tags.txt", "Runtime", "D3D11SelectedAdapter", desc.Description);
				}
				else if (!flag && intPtr == IntPtr.Zero && intPtr2 == IntPtr.Zero)
				{
					intPtr2 = adapter;
				}
				else
				{
					ComRelease.Release(adapter);
				}
			}
			ComRelease.Release(factory);
			factory = IntPtr.Zero;
			if (intPtr != IntPtr.Zero)
			{
				return intPtr;
			}
			if (intPtr2 != IntPtr.Zero)
			{
				Watchdog.LogProperty("crash_tags.txt", "Runtime", "D3D11SelectedAdapter", "FirstHardwareFallback");
				return intPtr2;
			}
			return IntPtr.Zero;
		}
		catch (Exception)
		{
			ComRelease.Release(factory);
			return IntPtr.Zero;
		}
	}

	private void CreateRenderTargetView()
	{
		if (_renderTargetView != IntPtr.Zero)
		{
			ComRelease.Release(_renderTargetView);
			_renderTargetView = IntPtr.Zero;
		}
		Guid riid = IID_ID3D11Texture2D;
		if (DXGISwapChain.GetBuffer(_swapChain, ref riid, out var surface) >= 0 && !(surface == IntPtr.Zero))
		{
			D3D11Device.CreateRenderTargetView(_device, surface, out _renderTargetView);
			ComRelease.Release(surface);
			_ = 0;
		}
	}

	private void CreateRenderStates()
	{
		D3D11_RENDER_TARGET_BLEND_DESC d3D11_RENDER_TARGET_BLEND_DESC = default(D3D11_RENDER_TARGET_BLEND_DESC);
		d3D11_RENDER_TARGET_BLEND_DESC.BlendEnable = 1;
		d3D11_RENDER_TARGET_BLEND_DESC.SrcBlend = 5u;
		d3D11_RENDER_TARGET_BLEND_DESC.DestBlend = 6u;
		d3D11_RENDER_TARGET_BLEND_DESC.BlendOp = 1u;
		d3D11_RENDER_TARGET_BLEND_DESC.SrcBlendAlpha = 2u;
		d3D11_RENDER_TARGET_BLEND_DESC.DestBlendAlpha = 2u;
		d3D11_RENDER_TARGET_BLEND_DESC.BlendOpAlpha = 1u;
		d3D11_RENDER_TARGET_BLEND_DESC.RenderTargetWriteMask = 15;
		D3D11_RENDER_TARGET_BLEND_DESC rT = d3D11_RENDER_TARGET_BLEND_DESC;
		D3D11_BLEND_DESC d3D11_BLEND_DESC = default(D3D11_BLEND_DESC);
		d3D11_BLEND_DESC.AlphaToCoverageEnable = 0;
		d3D11_BLEND_DESC.IndependentBlendEnable = 0;
		d3D11_BLEND_DESC.RT0 = rT;
		D3D11_BLEND_DESC desc = d3D11_BLEND_DESC;
		D3D11Device.CreateBlendState(_device, ref desc, out _blendStateAlpha);
		d3D11_RENDER_TARGET_BLEND_DESC = default(D3D11_RENDER_TARGET_BLEND_DESC);
		d3D11_RENDER_TARGET_BLEND_DESC.BlendEnable = 0;
		d3D11_RENDER_TARGET_BLEND_DESC.SrcBlend = 2u;
		d3D11_RENDER_TARGET_BLEND_DESC.DestBlend = 1u;
		d3D11_RENDER_TARGET_BLEND_DESC.BlendOp = 1u;
		d3D11_RENDER_TARGET_BLEND_DESC.SrcBlendAlpha = 2u;
		d3D11_RENDER_TARGET_BLEND_DESC.DestBlendAlpha = 1u;
		d3D11_RENDER_TARGET_BLEND_DESC.BlendOpAlpha = 1u;
		d3D11_RENDER_TARGET_BLEND_DESC.RenderTargetWriteMask = 15;
		D3D11_RENDER_TARGET_BLEND_DESC rT2 = d3D11_RENDER_TARGET_BLEND_DESC;
		d3D11_BLEND_DESC = default(D3D11_BLEND_DESC);
		d3D11_BLEND_DESC.AlphaToCoverageEnable = 0;
		d3D11_BLEND_DESC.IndependentBlendEnable = 0;
		d3D11_BLEND_DESC.RT0 = rT2;
		D3D11_BLEND_DESC desc2 = d3D11_BLEND_DESC;
		D3D11Device.CreateBlendState(_device, ref desc2, out _blendStateOpaque);
		D3D11_RASTERIZER_DESC d3D11_RASTERIZER_DESC = default(D3D11_RASTERIZER_DESC);
		d3D11_RASTERIZER_DESC.FillMode = 3u;
		d3D11_RASTERIZER_DESC.CullMode = 1u;
		d3D11_RASTERIZER_DESC.FrontCounterClockwise = 0;
		d3D11_RASTERIZER_DESC.DepthBias = 0;
		d3D11_RASTERIZER_DESC.DepthBiasClamp = 0f;
		d3D11_RASTERIZER_DESC.SlopeScaledDepthBias = 0f;
		d3D11_RASTERIZER_DESC.DepthClipEnable = 0;
		d3D11_RASTERIZER_DESC.ScissorEnable = 1;
		d3D11_RASTERIZER_DESC.MultisampleEnable = 0;
		d3D11_RASTERIZER_DESC.AntialiasedLineEnable = 0;
		D3D11_RASTERIZER_DESC desc3 = d3D11_RASTERIZER_DESC;
		D3D11Device.CreateRasterizerState(_device, ref desc3, out _rasterizerScissor);
		D3D11_RASTERIZER_DESC desc4 = desc3;
		desc4.ScissorEnable = 0;
		D3D11Device.CreateRasterizerState(_device, ref desc4, out _rasterizerNoScissor);
		D3D11_SAMPLER_DESC d3D11_SAMPLER_DESC = default(D3D11_SAMPLER_DESC);
		d3D11_SAMPLER_DESC.Filter = 21u;
		d3D11_SAMPLER_DESC.AddressU = 1u;
		d3D11_SAMPLER_DESC.AddressV = 1u;
		d3D11_SAMPLER_DESC.AddressW = 1u;
		d3D11_SAMPLER_DESC.MipLODBias = 0f;
		d3D11_SAMPLER_DESC.MaxAnisotropy = 1u;
		d3D11_SAMPLER_DESC.ComparisonFunc = 1u;
		d3D11_SAMPLER_DESC.BorderColor0 = 0f;
		d3D11_SAMPLER_DESC.BorderColor1 = 0f;
		d3D11_SAMPLER_DESC.BorderColor2 = 0f;
		d3D11_SAMPLER_DESC.BorderColor3 = 0f;
		d3D11_SAMPLER_DESC.MinLOD = 0f;
		d3D11_SAMPLER_DESC.MaxLOD = float.MaxValue;
		D3D11_SAMPLER_DESC desc5 = d3D11_SAMPLER_DESC;
		D3D11Device.CreateSamplerState(_device, ref desc5, out _samplerLinear);
		D3D11_SAMPLER_DESC desc6 = desc5;
		desc6.AddressU = 3u;
		desc6.AddressV = 3u;
		desc6.AddressW = 3u;
		D3D11Device.CreateSamplerState(_device, ref desc6, out _samplerLinearClamp);
	}

	private void CreateConstantBuffers()
	{
		D3D11_BUFFER_DESC d3D11_BUFFER_DESC = default(D3D11_BUFFER_DESC);
		d3D11_BUFFER_DESC.ByteWidth = 64u;
		d3D11_BUFFER_DESC.Usage = 2u;
		d3D11_BUFFER_DESC.BindFlags = 4u;
		d3D11_BUFFER_DESC.CPUAccessFlags = 65536u;
		d3D11_BUFFER_DESC.MiscFlags = 0u;
		d3D11_BUFFER_DESC.StructureByteStride = 0u;
		D3D11_BUFFER_DESC desc = d3D11_BUFFER_DESC;
		int num = D3D11Device.CreateBuffer(_device, ref desc, out _cbMVP);
		if (num < 0)
		{
			StandaloneApplicationUtility.TerminateWithMessageBox("DirectX error", $"Failed to create MVP constant buffer (0x{num:X8}).");
			return;
		}
		d3D11_BUFFER_DESC = default(D3D11_BUFFER_DESC);
		d3D11_BUFFER_DESC.ByteWidth = 256u;
		d3D11_BUFFER_DESC.Usage = 2u;
		d3D11_BUFFER_DESC.BindFlags = 4u;
		d3D11_BUFFER_DESC.CPUAccessFlags = 65536u;
		d3D11_BUFFER_DESC.MiscFlags = 0u;
		d3D11_BUFFER_DESC.StructureByteStride = 0u;
		D3D11_BUFFER_DESC desc2 = d3D11_BUFFER_DESC;
		num = D3D11Device.CreateBuffer(_device, ref desc2, out _cbMaterial);
		if (num < 0)
		{
			StandaloneApplicationUtility.TerminateWithMessageBox("DirectX error", $"Failed to create material constant buffer (0x{num:X8}).");
		}
	}

	public void BeginFrame(int width, int height)
	{
		if (_isShuttingDown)
		{
			return;
		}
		_anyInvalidMatricesThisFrame = false;
		_stopwatch.Start();
		if (_deviceLost)
		{
			_anyInvalidMatricesThisFrame = true;
			return;
		}
		Resize(width, height);
		if (_renderTargetView == IntPtr.Zero)
		{
			_anyInvalidMatricesThisFrame = true;
			return;
		}
		D3D11Context.OMSetRenderTargets(_context, _renderTargetView);
		D3D11Context.RSSetState(_context, _rasterizerNoScissor);
		_scissorEnabled = false;
		D3D11_VIEWPORT d3D11_VIEWPORT = default(D3D11_VIEWPORT);
		d3D11_VIEWPORT.TopLeftX = 0f;
		d3D11_VIEWPORT.TopLeftY = 0f;
		d3D11_VIEWPORT.Width = width;
		d3D11_VIEWPORT.Height = height;
		d3D11_VIEWPORT.MinDepth = 0f;
		d3D11_VIEWPORT.MaxDepth = 1f;
		D3D11_VIEWPORT vp = d3D11_VIEWPORT;
		D3D11Context.RSSetViewports(_context, vp);
		D3D11Context.ClearRenderTargetView(_context, _renderTargetView, new float[4]);
		D3D11Context.PSSetSamplers(_context, _samplerLinear);
		SetBlending(enable: false);
	}

	internal void Resize(int width, int height)
	{
		if ((width == _screenWidth && height == _screenHeight) || width <= 0 || height <= 0)
		{
			return;
		}
		_screenWidth = width;
		_screenHeight = height;
		if (IsLayeredWindow)
		{
			CreateOffscreenRenderTarget();
			return;
		}
		if (_renderTargetView != IntPtr.Zero)
		{
			ComRelease.Release(_renderTargetView);
			_renderTargetView = IntPtr.Zero;
		}
		int num = DXGISwapChain.ResizeBuffers(_swapChain, (uint)width, (uint)height);
		if (num < 0)
		{
			if (num == -2005270523 || num == -2005270521)
			{
				_deviceLost = true;
			}
		}
		else
		{
			CreateRenderTargetView();
		}
	}

	public void SwapBuffers()
	{
		if (_isShuttingDown)
		{
			return;
		}
		int num = (int)_stopwatch.ElapsedMilliseconds;
		int num2 = MaxTimeToRenderOneFrame - num;
		if (num2 > 0)
		{
			Thread.Sleep(num2);
		}
		if (!IsLayeredWindow)
		{
			int num3 = DXGISwapChain.Present(_swapChain, 0u, 0u);
			if (num3 == -2005270523 || num3 == -2005270521)
			{
				_deviceLost = true;
				_anyInvalidMatricesThisFrame = true;
			}
			else if (num3 < 0)
			{
				_anyInvalidMatricesThisFrame = true;
			}
		}
		_stopwatch.Restart();
		if (_anyInvalidMatricesThisFrame)
		{
			_failedRenderFrames++;
		}
		else
		{
			_failedRenderFrames = 0;
		}
		if (_failedRenderFrames >= 180)
		{
			Watchdog.LogProperty("crash_tags.txt", "Runtime", "LauncherRenderFailure", "ConsecutiveFrameThresholdExceeded");
			StandaloneApplicationUtility.TerminateWithMessageBox("Launcher render error", "The launcher encountered too many consecutive render failures and must close.\nPlease update your graphics drivers and try again.");
		}
	}

	public void DestroyContext()
	{
		_isShuttingDown = true;
		Active = null;
		foreach (DirectXShader value in _loadedShaders.Values)
		{
			value?.Dispose();
		}
		foreach (DirectXTexture value2 in LoadedTextures.Values)
		{
			value2?.Dispose();
		}
		_vertexBuffer?.Dispose();
		ComRelease.Release(_cbMVP);
		ComRelease.Release(_cbMaterial);
		ComRelease.Release(_samplerLinear);
		ComRelease.Release(_samplerLinearClamp);
		ComRelease.Release(_blendStateAlpha);
		ComRelease.Release(_blendStateOpaque);
		ComRelease.Release(_rasterizerScissor);
		ComRelease.Release(_rasterizerNoScissor);
		ComRelease.Release(_renderTargetView);
		ComRelease.Release(_offscreenRT);
		ComRelease.Release(_swapChain);
		if (_context != IntPtr.Zero)
		{
			D3D11Context.ClearState(_context);
			D3D11Context.Flush(_context);
		}
		ComRelease.Release(_context);
		ComRelease.Release(_device);
	}

	public void Dispose()
	{
		DestroyContext();
	}

	public void ReportDeviceLost(int triggerHr)
	{
		if (!_isShuttingDown)
		{
			int num = ((_device != IntPtr.Zero) ? D3D11Device.GetDeviceRemovedReason(_device) : triggerHr);
			Watchdog.LogProperty("crash_tags.txt", "Runtime", "D3D11DeviceRemoved", $"trigger=0x{triggerHr:X8} reason=0x{num:X8}");
			_deviceLost = true;
			_anyInvalidMatricesThisFrame = true;
			StandaloneApplicationUtility.TerminateWithMessageBox("Graphics device lost", "The graphics device was lost (driver crash, GPU reset, or power-down).\nPlease close and relaunch the launcher.");
		}
	}

	public IntPtr GetCurrentBackBuffer()
	{
		if (_isShuttingDown)
		{
			return IntPtr.Zero;
		}
		if (IsLayeredWindow)
		{
			if (_offscreenRT == IntPtr.Zero)
			{
				return IntPtr.Zero;
			}
			ComAddRef.AddRef(_offscreenRT);
			return _offscreenRT;
		}
		if (_swapChain == IntPtr.Zero)
		{
			return IntPtr.Zero;
		}
		Guid riid = IID_ID3D11Texture2D;
		if (DXGISwapChain.GetBuffer(_swapChain, ref riid, out var surface) < 0 || surface == IntPtr.Zero)
		{
			return IntPtr.Zero;
		}
		return surface;
	}

	public void SetScissor(ScissorTestInfo scissorTestInfo)
	{
		SimpleRectangle simpleRectangle = scissorTestInfo.GetSimpleRectangle();
		D3D11_RECT d3D11_RECT = default(D3D11_RECT);
		d3D11_RECT.left = (int)simpleRectangle.X;
		d3D11_RECT.top = (int)simpleRectangle.Y;
		d3D11_RECT.right = (int)(simpleRectangle.X + simpleRectangle.Width);
		d3D11_RECT.bottom = (int)(simpleRectangle.Y + simpleRectangle.Height);
		D3D11_RECT rect = d3D11_RECT;
		D3D11Context.RSSetScissorRects(_context, rect);
		if (!_scissorEnabled)
		{
			D3D11Context.RSSetState(_context, _rasterizerScissor);
			_scissorEnabled = true;
		}
	}

	public void ResetScissor()
	{
		if (_scissorEnabled)
		{
			D3D11Context.RSSetState(_context, _rasterizerNoScissor);
			_scissorEnabled = false;
		}
	}

	public void SetBlending(bool enable)
	{
		if (_blendingEnabled != enable)
		{
			_blendingEnabled = enable;
			D3D11Context.OMSetBlendState(_context, enable ? _blendStateAlpha : _blendStateOpaque);
		}
	}

	public DirectXShader GetOrLoadShader(string shaderName)
	{
		if (_loadedShaders.ContainsKey(shaderName))
		{
			return _loadedShaders[shaderName];
		}
		try
		{
			string hlslSource = File.ReadAllText(_resourceDepot.GetFilePath(shaderName + ".hlsl"));
			DirectXShader directXShader = DirectXShader.CreateShader(_device, hlslSource, shaderName);
			_loadedShaders[shaderName] = directXShader;
			return directXShader;
		}
		catch (Exception)
		{
			_loadedShaders[shaderName] = null;
			return null;
		}
	}

	public void DrawImage(SimpleMaterial material, in ImageDrawObject drawObject)
	{
		if (!_isShuttingDown)
		{
			DirectXShader directXShader = PrepareRender(material, in drawObject.Rectangle);
			if (directXShader != null)
			{
				DrawImageAux(directXShader, material, in drawObject);
			}
		}
	}

	public void DrawText(TextMaterial material, in TextDrawObject drawObject)
	{
		if (!_isShuttingDown && PrepareRender(material, in drawObject.Rectangle) != null)
		{
			DrawTextAux(material, in drawObject);
		}
	}

	public void DrawPolygon(PrimitivePolygonMaterial material, in ImageDrawObject drawObject)
	{
		if (!_isShuttingDown && PrepareRender(material, in drawObject.Rectangle) != null)
		{
			DrawPolygonAux(material, in drawObject);
		}
	}

	private DirectXShader PrepareRender(Material material, in Rectangle2D rect)
	{
		DirectXShader orLoadShader = GetOrLoadShader(material.GetType().Name);
		if (orLoadShader == null)
		{
			_anyInvalidMatricesThisFrame = true;
			return null;
		}
		if (_screenWidth <= 0 || _screenHeight <= 0)
		{
			_anyInvalidMatricesThisFrame = true;
			return null;
		}
		MatrixFrame cachedVisualMatrixFrame = rect.GetCachedVisualMatrixFrame();
		if (cachedVisualMatrixFrame.AreAllComponentsValid() && !cachedVisualMatrixFrame.IsZero)
		{
			ModelMatrix = cachedVisualMatrixFrame;
		}
		else
		{
			ModelMatrix = ValidateModelMatrix(cachedVisualMatrixFrame);
			_anyInvalidMatricesThisFrame = true;
		}
		Matrix4x4 matrix4x = ValidateModelMatrix(_modelMatrix).ToMatrix4x4();
		Matrix4x4 matrix4x2 = ValidateViewMatrix(in _viewMatrix).ToMatrix4x4();
		Matrix4x4 matrix4x3 = ValidateProjectionMatrix(in _projectionMatrix).ToMatrix4x4();
		Matrix4x4 mvp = matrix4x * matrix4x2 * matrix4x3;
		UploadMVP(in mvp);
		orLoadShader.Use(_context);
		D3D11Context.VSSetConstantBuffers(_context, 0u, _cbMVP);
		D3D11Context.PSSetConstantBuffers(_context, 0u, _cbMVP);
		return orLoadShader;
	}

	private void DrawImageAux(DirectXShader shader, SimpleMaterial material, in ImageDrawObject drawObject)
	{
		SetBlending(material.Blending);
		IntPtr srv = IntPtr.Zero;
		IntPtr srv2 = IntPtr.Zero;
		bool flag = false;
		if (material.Texture != null)
		{
			DirectXTexture obj = material.Texture.PlatformTexture as DirectXTexture;
			srv = obj?.ShaderResourceView ?? IntPtr.Zero;
			flag = obj?.ClampToEdge ?? false;
		}
		D3D11Context.PSSetSamplers(_context, flag ? _samplerLinearClamp : _samplerLinear);
		D3D11Context.PSSetShaderResources(_context, 0u, srv);
		if (material.OverlayEnabled && material.OverlayTexture != null)
		{
			srv2 = (material.OverlayTexture.PlatformTexture as DirectXTexture)?.ShaderResourceView ?? IntPtr.Zero;
		}
		D3D11Context.PSSetShaderResources(_context, 1u, srv2);
		float hueFactor = Clamp(material.HueFactor / 360f, -0.5f, 0.5f);
		float saturationFactor = Clamp(material.SaturationFactor / 360f, -0.5f, 0.5f);
		float valueFactor = Clamp(material.ValueFactor / 360f, -0.5f, 0.5f);
		SimpleMaterialCB simpleMaterialCB = default(SimpleMaterialCB);
		simpleMaterialCB.InputColor = ColorToFloat4(material.Color);
		simpleMaterialCB.ColorFactor = material.ColorFactor;
		simpleMaterialCB.AlphaFactor = material.AlphaFactor;
		simpleMaterialCB.HueFactor = hueFactor;
		simpleMaterialCB.SaturationFactor = saturationFactor;
		simpleMaterialCB.ValueFactor = valueFactor;
		simpleMaterialCB.OverlayEnabled = (material.OverlayEnabled ? 1 : 0);
		simpleMaterialCB.StartCoordX = material.StartCoordinate.X;
		simpleMaterialCB.StartCoordY = material.StartCoordinate.Y;
		simpleMaterialCB.SizeX = material.Size.X;
		simpleMaterialCB.SizeY = material.Size.Y;
		simpleMaterialCB.OverlayOffsetX = material.OverlayXOffset;
		simpleMaterialCB.OverlayOffsetY = material.OverlayYOffset;
		simpleMaterialCB.CircularMaskingEnabled = (material.CircularMaskingEnabled ? 1 : 0);
		simpleMaterialCB.MaskingCenterX = material.CircularMaskingCenter.X;
		simpleMaterialCB.MaskingCenterY = material.CircularMaskingCenter.Y;
		simpleMaterialCB.MaskingRadius = material.CircularMaskingRadius;
		simpleMaterialCB.MaskingSmoothingRadius = material.CircularMaskingSmoothingRadius;
		SimpleMaterialCB data = simpleMaterialCB;
		UploadMaterialCB(ref data, Marshal.SizeOf<SimpleMaterialCB>());
		D3D11Context.PSSetConstantBuffers(_context, 1u, _cbMaterial);
		Vector2 vector = new Vector2(drawObject.Uvs.x, drawObject.Uvs.y);
		Vector2 vector2 = new Vector2(drawObject.Uvs.z, drawObject.Uvs.w);
		float[] obj2 = new float[16]
		{
			0f, 0f, 0f, 0f, 0f, 1f, 0f, 0f, 1f, 1f,
			0f, 0f, 1f, 0f, 0f, 0f
		};
		obj2[2] = vector.X;
		obj2[3] = vector.Y;
		obj2[6] = vector.X;
		obj2[7] = vector2.Y;
		obj2[10] = vector2.X;
		obj2[11] = vector2.Y;
		obj2[14] = vector2.X;
		obj2[15] = vector.Y;
		float[] interleavedData = obj2;
		uint[] array = new uint[6] { 0u, 1u, 2u, 0u, 2u, 3u };
		_vertexBuffer.LoadVertexData(_context, interleavedData);
		_vertexBuffer.LoadIndexData(_context, array);
		_vertexBuffer.Bind(_context);
		D3D11Context.DrawIndexed(_context, array.Length);
		D3D11Context.PSClearShaderResource(_context, 0u);
		D3D11Context.PSClearShaderResource(_context, 1u);
	}

	private void DrawTextAux(TextMaterial material, in TextDrawObject drawObject)
	{
		SetBlending(material.Blending);
		D3D11Context.PSSetSamplers(_context, _samplerLinear);
		IntPtr srv = IntPtr.Zero;
		if (material.Texture != null)
		{
			srv = (material.Texture.PlatformTexture as DirectXTexture)?.ShaderResourceView ?? IntPtr.Zero;
		}
		D3D11Context.PSSetShaderResources(_context, 0u, srv);
		TextMaterialCB textMaterialCB = default(TextMaterialCB);
		textMaterialCB.InputColor = ColorToFloat4(material.Color);
		textMaterialCB.GlowColor = ColorToFloat4(material.GlowColor);
		textMaterialCB.OutlineColor = ColorToFloat4(material.OutlineColor);
		textMaterialCB.OutlineAmount = material.OutlineAmount;
		textMaterialCB.ScaleFactor = 1.5f / material.ScaleFactor;
		textMaterialCB.SmoothingConstant = material.SmoothingConstant;
		textMaterialCB.GlowRadius = material.GlowRadius;
		textMaterialCB.Blur = material.Blur;
		textMaterialCB.ShadowOffset = material.ShadowOffset;
		textMaterialCB.ShadowAngle = material.ShadowAngle;
		textMaterialCB.ColorFactor = material.ColorFactor;
		textMaterialCB.AlphaFactor = material.AlphaFactor;
		TextMaterialCB data = textMaterialCB;
		UploadMaterialCB(ref data, Marshal.SizeOf<TextMaterialCB>());
		D3D11Context.PSSetConstantBuffers(_context, 1u, _cbMaterial);
		float[] text_Vertices = drawObject.Text_Vertices;
		float[] text_TextureCoordinates = drawObject.Text_TextureCoordinates;
		uint[] text_Indices = drawObject.Text_Indices;
		int num = text_Vertices.Length / 2;
		float[] array = new float[num * 4];
		for (int i = 0; i < num; i++)
		{
			array[i * 4] = text_Vertices[i * 2];
			array[i * 4 + 1] = text_Vertices[i * 2 + 1];
			array[i * 4 + 2] = text_TextureCoordinates[i * 2];
			array[i * 4 + 3] = text_TextureCoordinates[i * 2 + 1];
		}
		_vertexBuffer.LoadVertexData(_context, array);
		_vertexBuffer.LoadIndexData(_context, text_Indices);
		_vertexBuffer.Bind(_context);
		D3D11Context.DrawIndexed(_context, text_Indices.Length);
		D3D11Context.PSClearShaderResource(_context, 0u);
	}

	private void DrawPolygonAux(PrimitivePolygonMaterial material, in ImageDrawObject drawObject)
	{
		SetBlending(material.Blending);
		PrimitivePolygonCB primitivePolygonCB = default(PrimitivePolygonCB);
		primitivePolygonCB.Color = ColorToFloat4(material.Color);
		PrimitivePolygonCB data = primitivePolygonCB;
		UploadMaterialCB(ref data, Marshal.SizeOf<PrimitivePolygonCB>());
		D3D11Context.PSSetConstantBuffers(_context, 1u, _cbMaterial);
		float[] interleavedData = new float[16]
		{
			0f, 0f, 0f, 0f, 0f, 1f, 0f, 0f, 1f, 1f,
			0f, 0f, 1f, 0f, 0f, 0f
		};
		uint[] array = new uint[6] { 0u, 1u, 2u, 0u, 2u, 3u };
		_vertexBuffer.LoadVertexData(_context, interleavedData);
		_vertexBuffer.LoadIndexData(_context, array);
		_vertexBuffer.Bind(_context);
		D3D11Context.DrawIndexed(_context, array.Length);
	}

	private void UploadMVP(in Matrix4x4 mvp)
	{
		if (D3D11Context.Map(_context, _cbMVP, 4u, out var mapped) < 0)
		{
			return;
		}
		try
		{
			Marshal.Copy(new float[16]
			{
				mvp.M11, mvp.M12, mvp.M13, mvp.M14, mvp.M21, mvp.M22, mvp.M23, mvp.M24, mvp.M31, mvp.M32,
				mvp.M33, mvp.M34, mvp.M41, mvp.M42, mvp.M43, mvp.M44
			}, 0, mapped.pData, 16);
		}
		finally
		{
			D3D11Context.Unmap(_context, _cbMVP);
		}
	}

	private void UploadMaterialCB<T>(ref T data, int size) where T : struct
	{
		if (D3D11Context.Map(_context, _cbMaterial, 4u, out var mapped) < 0)
		{
			return;
		}
		try
		{
			IntPtr intPtr = Marshal.AllocHGlobal(size);
			try
			{
				Marshal.StructureToPtr(data, intPtr, fDeleteOld: false);
				byte[] array = new byte[size];
				Marshal.Copy(intPtr, array, 0, size);
				Marshal.Copy(array, 0, mapped.pData, size);
			}
			finally
			{
				Marshal.FreeHGlobal(intPtr);
			}
		}
		finally
		{
			D3D11Context.Unmap(_context, _cbMaterial);
		}
	}

	public void LoadTextureUsing(DirectXTexture texture, ResourceDepot resourceDepot, string name)
	{
		if (!LoadedTextures.ContainsKey(name))
		{
			texture.LoadFromFile(_device, resourceDepot, name);
			LoadedTextures.Add(name, texture);
		}
		else
		{
			texture.CopyFrom(LoadedTextures[name]);
		}
	}

	public DirectXTexture LoadTexture(ResourceDepot resourceDepot, string name)
	{
		if (LoadedTextures.TryGetValue(name, out var value))
		{
			if (value != null && value.IsLoaded())
			{
				return value;
			}
			LoadedTextures.Remove(name);
		}
		DirectXTexture directXTexture = DirectXTexture.FromFile(_device, resourceDepot, name);
		if (directXTexture == null || !directXTexture.IsLoaded())
		{
			return null;
		}
		LoadedTextures.Add(name, directXTexture);
		return directXTexture;
	}

	public DirectXTexture GetTexture(string textureName)
	{
		LoadedTextures.TryGetValue(textureName, out var value);
		return value;
	}

	private static MatrixFrame ValidateModelMatrix(MatrixFrame m)
	{
		if (!m.origin.IsValidXYZW)
		{
			m.origin = new Vec3(0f, 0f, 0f, 0f);
		}
		if (!m.rotation.s.IsValidXYZW)
		{
			m.rotation.s = new Vec3(100f, 0f, 0f, 0f);
		}
		if (!m.rotation.f.IsValidXYZW)
		{
			m.rotation.f = new Vec3(0f, 100f, 0f, 0f);
		}
		if (!m.rotation.u.IsValidXYZW)
		{
			m.rotation.u = new Vec3(0f, 0f, 1f, 0f);
		}
		m.Fill();
		return m;
	}

	private static MatrixFrame ValidateViewMatrix(in MatrixFrame m)
	{
		if (!m.AreAllComponentsValid())
		{
			return MatrixFrame.CreateLookAt(in Vec3.Up, in Vec3.Zero, in Vec3.Forward);
		}
		return m;
	}

	private static MatrixFrame ValidateProjectionMatrix(in MatrixFrame m)
	{
		if (!m.AreAllComponentsValid())
		{
			return MatrixExtensions.CreateOrthographicOffCenter(0f, 900f, 600f, 0f, 0f, 1f);
		}
		return m;
	}

	private static float[] ColorToFloat4(Color c)
	{
		return new float[4] { c.Red, c.Green, c.Blue, c.Alpha };
	}

	private static float Clamp(float v, float min, float max)
	{
		if (!(v < min))
		{
			if (!(v > max))
			{
				return v;
			}
			return max;
		}
		return min;
	}
}
