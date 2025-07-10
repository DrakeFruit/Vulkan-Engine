using System.Numerics;
using System.Runtime.InteropServices;
using Silk.NET.Core.Native;
using Image = Silk.NET.GLFW.Image;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace VulkanEngine;

unsafe partial class Engine
{
    private const int Width = 800;
    private const int Height = 600;

    private readonly string[] _validationLayers =
    [
        "VK_LAYER_KHRONOS_validation"
    ];

    private readonly string[] _deviceExtensions =
    [
        KhrSwapchain.ExtensionName
    ];

#if DEBUG
    private const bool EnableValidationLayers = true;
#else
        private const bool EnableValidationLayers = false;
#endif

    private Vk? _vk;
    private IWindow? _window;
    private Instance _instance;

    private KhrSurface? _khrSurface;
    private SurfaceKHR _surface;

    public static void Main(string[] args)
    {
        var app = new Engine();
        app.Run();
    }

    public void Run()
    {
        InitWindow();
        InitVulkan();
        MainLoop();
        CleanUp();
    }

    private void InitWindow()
    {
        var options = WindowOptions.DefaultVulkan with
        {
            Size = new Vector2D<int>(Width, Height),
            Title = "Vulkan Engine",
            VSync = true,
        };

        _window = Window.Create(options);
        _window.Initialize();

        if (_window.VkSurface is null)
        {
            throw new Exception("Windowing platform doesn't support Vulkan.");
        }
    }

    private void InitVulkan()
    {
        CreateInstance();
        SetupDebugMessenger();
        CreateSurface();
        PickPhysicalDevice();
        CreateLogicalDevice();
        CreateSwapChain();
        CreateImageViews();
        CreateRenderPass();
        CreateGraphicsPipeline();
        CreateFrameBuffers();
        CreateCommandPool();
        CreateCommandBuffers();
        CreateSyncObjects();
    }

    private void MainLoop()
    {
        _window!.Render += DrawFrame;
        _window!.Run();
        _vk!.DeviceWaitIdle(_device);
    }

    private void CleanUp()
    {
        for (int i = 0; i < MAX_FRAMES_IN_FLIGHT; i++)
        {
            _vk!.DestroySemaphore(_device, _renderFinishedSemaphores![i], null);
            _vk!.DestroySemaphore(_device, _imageAvailableSemaphores![i], null);
            _vk!.DestroyFence(_device, _inFlightFences![i], null);
        }
        foreach (var framebuffer in _swapChainFramebuffers!)
        {
            _vk!.DestroyFramebuffer(_device, framebuffer, null);
        }
        foreach (var imageView in _swapChainImageViews!)
        {
            _vk!.DestroyImageView(_device, imageView, null);
        }
        
        _vk!.DestroyCommandPool(_device, _commandPool, null);
        _vk!.DestroyPipeline(_device, _graphicsPipeline, null);
        _vk!.DestroyPipelineLayout(_device, _pipelineLayout, null);
        _vk!.DestroyRenderPass(_device, _renderPass, null);
        _vk!.DestroyDevice(_device, null);
        _khrSwapChain!.DestroySwapchain(_device, _swapChain, null);

        if (EnableValidationLayers)
        {
            //DestroyDebugUtilsMessenger equivilant to method DestroyDebugUtilsMessengerEXT from original tutorial.
            _debugUtils!.DestroyDebugUtilsMessenger(_instance, _debugMessenger, null);
        }

        _khrSurface!.DestroySurface(_instance, _surface, null);
        _vk!.DestroyInstance(_instance, null);
        _vk!.Dispose();

        _window?.Dispose();
    }
    
    private void CreateSyncObjects() {
        _imageAvailableSemaphores = new Semaphore[MAX_FRAMES_IN_FLIGHT];
        _renderFinishedSemaphores = new Semaphore[MAX_FRAMES_IN_FLIGHT];
        _inFlightFences = new Fence[MAX_FRAMES_IN_FLIGHT];
        _imagesInFlight = new Fence[_swapChainImages!.Length];

        SemaphoreCreateInfo semaphoreInfo = new()
        {
            SType = StructureType.SemaphoreCreateInfo,
        };

        FenceCreateInfo fenceInfo = new()
        {
            SType = StructureType.FenceCreateInfo,
            Flags = FenceCreateFlags.SignaledBit,
        };

        for (var i = 0; i < MAX_FRAMES_IN_FLIGHT; i++)
        {
            if (_vk!.CreateSemaphore(_device, in semaphoreInfo, null, out _imageAvailableSemaphores[i]) != Result.Success ||
                _vk!.CreateSemaphore(_device, in semaphoreInfo, null, out _renderFinishedSemaphores[i]) != Result.Success ||
                _vk!.CreateFence(_device, in fenceInfo, null, out _inFlightFences[i]) != Result.Success)
            {
                throw new Exception("failed to create synchronization objects for a frame!");
            }
        }
    }

    private void CreateSurface()
    {
        if (!_vk!.TryGetInstanceExtension<KhrSurface>(_instance, out _khrSurface))
        {
            throw new NotSupportedException("KHR_surface extension not found.");
        }

        _surface = _window!.VkSurface!.Create<AllocationCallbacks>(_instance.ToHandle(), null).ToSurface();
    }

    private void CreateInstance()
    {
        _vk = Vk.GetApi();

        if (EnableValidationLayers && !ValidationLayersSupported())
        {
            throw new Exception("Validation layers requested are not available");
        }

        ApplicationInfo appInfo;
        appInfo.SType = StructureType.ApplicationInfo;
        appInfo.PApplicationName = (byte*)Marshal.StringToHGlobalAnsi("Vulkan Engine");
        appInfo.ApplicationVersion = new Version32(1, 0, 0);
        appInfo.PEngineName = (byte*)Marshal.StringToHGlobalAnsi("No Engine");
        appInfo.EngineVersion = new Version32(1, 0, 0);
        appInfo.ApiVersion = Vk.Version12;

        InstanceCreateInfo createInfo = new();
        createInfo.SType = StructureType.InstanceCreateInfo;
        createInfo.PApplicationInfo = &appInfo;

        var requiredExtensions = GetRequiredExtensions();
        createInfo.EnabledExtensionCount = (uint)requiredExtensions.Length;
        createInfo.PpEnabledExtensionNames = (byte**)SilkMarshal.StringArrayToPtr(requiredExtensions);

        if (EnableValidationLayers)
        {
            createInfo.EnabledLayerCount = (uint)_validationLayers.Length;
            createInfo.PpEnabledLayerNames = (byte**)SilkMarshal.StringArrayToPtr(_validationLayers);

            DebugUtilsMessengerCreateInfoEXT debugCreateInfo = new();
            PopulateDebugMessengerCreateInfo(ref debugCreateInfo);
            createInfo.PNext = &debugCreateInfo;
        }
        else
        {
            createInfo.EnabledLayerCount = 0;
            createInfo.PNext = null;
        }

        if (_vk.CreateInstance(in createInfo, null, out _instance) != Result.Success)
        {
            throw new Exception("failed to create instance!");
        }

        Marshal.FreeHGlobal((IntPtr)appInfo.PApplicationName);
        Marshal.FreeHGlobal((IntPtr)appInfo.PEngineName);

        if (EnableValidationLayers)
        {
            SilkMarshal.Free((nint)createInfo.PpEnabledLayerNames);
        }
    }

    private string[] GetRequiredExtensions()
    {
        var requiredExtensions = _window!.VkSurface!.GetRequiredExtensions(out var glfwExtensionCount);
        var extensions = SilkMarshal.PtrToStringArray((nint)requiredExtensions, (int)glfwExtensionCount);

        if (EnableValidationLayers)
        {
            return extensions.Append(ExtDebugUtils.ExtensionName).ToArray();
        }

        return extensions;
    }

    bool ValidationLayersSupported()
    {
        uint layerCount = 0;
        _vk?.EnumerateInstanceLayerProperties(ref layerCount, null);

        var availableLayers = new LayerProperties[layerCount];
        fixed (LayerProperties* availableLayersPtr = availableLayers)
        {
            _vk!.EnumerateInstanceLayerProperties(ref layerCount, availableLayersPtr);
        }

        var availableLayerNames =
            availableLayers.Select(layer => Marshal.PtrToStringAnsi((IntPtr)layer.LayerName)).ToHashSet();

        return _validationLayers.All(availableLayerNames.Contains);
    }
}