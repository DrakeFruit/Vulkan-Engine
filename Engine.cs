using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using Silk.NET.Core.Native;
using Silk.NET.GLFW;

namespace VulkanEngine;

unsafe class Engine()
{
    private const int Width = 800;
    private const int Height = 600;

    private readonly string[] _validationLayers =
    [
        "VK_LAYER_KHRONOS_validation"
    ];
    
    #if DEBUG
        private const bool EnableValidationLayers = true;
    #else
        private const bool EnableValidationLayers = false;
    #endif

    
    private Vk? _vk;
    private IWindow? _window;
    private Instance _instance;
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

        var requiredExtensions = _window!.VkSurface!.GetRequiredExtensions( out var extensionCount );
        createInfo.EnabledExtensionCount = extensionCount;
        createInfo.PpEnabledExtensionNames = requiredExtensions;
        
        if (EnableValidationLayers) 
        {
            createInfo.EnabledLayerCount = (uint)_validationLayers.Length;
            createInfo.PpEnabledLayerNames = (byte**)SilkMarshal.StringArrayToPtr(_validationLayers);
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

    private void MainLoop()
    {
        _window!.Run();
    }

    private void CleanUp()
    {
        _vk!.DestroyInstance(_instance, null);
        _vk!.Dispose();

        _window?.Dispose();
    }
}