namespace VulkanEngine;

unsafe partial class Engine
{
    private SwapchainKHR _swapChain;
    private KhrSwapchain? _khrSwapChain;
    private Image[]? _swapChainImages;
    private Format _swapChainImageFormat;
    private Extent2D _swapChainExtent;
    private ImageView[]? _swapChainImageViews;
    
    private void CreateSwapChain()
    {
        var swapChainSupport = QuerySwapChainSupport(_physicalDevice);

        var surfaceFormat = ChooseSwapSurfaceFormat(swapChainSupport.Formats);
        var presentMode = ChooseSwapPresentMode(swapChainSupport.PresentModes);
        var extent = ChooseSwapExtent(swapChainSupport.Capabilities);
        
        uint imageCount = swapChainSupport.Capabilities.MinImageCount + 1;
        if (swapChainSupport.Capabilities.MaxImageCount > 0 
            && imageCount > swapChainSupport.Capabilities.MaxImageCount) {
            imageCount = swapChainSupport.Capabilities.MaxImageCount;
        }

        var createInfo = new SwapchainCreateInfoKHR();
        createInfo.SType = StructureType.SwapchainCreateInfoKhr;
        createInfo.Surface = _surface;
        createInfo.MinImageCount = imageCount;
        createInfo.ImageFormat = surfaceFormat.Format;
        createInfo.ImageColorSpace = surfaceFormat.ColorSpace;
        createInfo.ImageExtent = extent;
        createInfo.ImageArrayLayers = 1;
        createInfo.ImageUsage = ImageUsageFlags.ColorAttachmentBit;
        
        QueueFamilyIndices indices = FindQueueFamilies(_physicalDevice);
        var queueFamilyIndices = stackalloc[] { indices.GraphicsFamily!.Value, indices.PresentFamily!.Value };

        if (indices.GraphicsFamily != indices.PresentFamily) 
        {
            createInfo.ImageSharingMode = SharingMode.Concurrent;
            createInfo.QueueFamilyIndexCount = 2;
            createInfo.PQueueFamilyIndices = queueFamilyIndices;
        } 
        else 
        {
            createInfo.ImageSharingMode = SharingMode.Exclusive;
            createInfo.QueueFamilyIndexCount = 0; // Optional
            createInfo.PQueueFamilyIndices = null; // Optional
        }
        
        createInfo.PreTransform = swapChainSupport.Capabilities.CurrentTransform;
        createInfo.CompositeAlpha = CompositeAlphaFlagsKHR.OpaqueBitKhr;
        createInfo.PresentMode = presentMode;
        createInfo.Clipped = true;
        createInfo.OldSwapchain = default;
        
        if ( !_vk!.TryGetDeviceExtension(_instance, _device, out _khrSwapChain))
        {
            throw new NotSupportedException("VK_KHR_swapchain extension not found.");
        }

        if (_khrSwapChain!.CreateSwapchain(_device, in createInfo, null, out _swapChain) != Result.Success)
        {
            throw new Exception("failed to create swap chain!");
        }

        _khrSwapChain.GetSwapchainImages(_device, _swapChain, ref imageCount, null);
        _swapChainImages = new Image[imageCount];
        fixed (Image* swapChainImagesPtr = _swapChainImages)
        {
            _khrSwapChain.GetSwapchainImages(_device, _swapChain, ref imageCount, swapChainImagesPtr);
        }

        _swapChainImageFormat = surfaceFormat.Format;
        _swapChainExtent = extent;
    }

    private void CreateImageViews()
    {
        _swapChainImageViews = new ImageView[_swapChainImages!.Length];

        for (var i = 0; i < _swapChainImages.Length; i++)
        {
            var createInfo = new ImageViewCreateInfo();
            createInfo.SType = StructureType.ImageViewCreateInfo;
            createInfo.Image = _swapChainImages[i];
            createInfo.ViewType = ImageViewType.Type2D;
            createInfo.Format = _swapChainImageFormat;
            
            createInfo.Components.R = ComponentSwizzle.Identity;
            createInfo.Components.G = ComponentSwizzle.Identity;
            createInfo.Components.B = ComponentSwizzle.Identity;
            createInfo.Components.A = ComponentSwizzle.Identity;
            
            createInfo.SubresourceRange.AspectMask = ImageAspectFlags.ColorBit;
            createInfo.SubresourceRange.BaseMipLevel = 0;
            createInfo.SubresourceRange.LevelCount = 1;
            createInfo.SubresourceRange.BaseArrayLayer = 0;
            createInfo.SubresourceRange.LayerCount = 1;
            
            if ( _vk!.CreateImageView(_device, in createInfo, null, out _swapChainImageViews[i]) 
                 != Result.Success) {
                throw new Exception("failed to create image views!");
            }
        }
    }
}