using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml.XPath;
using Silk.NET.Core.Native;
using Silk.NET.GLFW;

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
    }
    
    private void MainLoop()
    {
        _window!.Run();
    }

    private void CleanUp()
    {
        if (EnableValidationLayers)
        {
            _debugUtils!.DestroyDebugUtilsMessenger(_instance, _debugMessenger, null);
        }
        
        _khrSurface!.DestroySurface(_instance, _surface, null);
        _vk!.DestroyInstance(_instance, null);
        _vk!.DestroyDevice(_device, null);
        _vk!.DestroyInstance(_instance, null);
        _vk!.Dispose();

        _window?.Dispose();
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

        if ( _vk.CreateInstance(in createInfo, null, out _instance) != Result.Success)
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
        
        return availableLayerNames.All(availableLayerNames.Contains);
    }
}