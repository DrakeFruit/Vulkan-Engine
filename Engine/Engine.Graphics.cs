using System.ComponentModel;
using System.Runtime.InteropServices;
using Silk.NET.Core.Native;

namespace VulkanEngine;

unsafe partial class Engine
{
    PipelineLayout _pipelineLayout;
    
    private void CreateGraphicsPipeline()
    {
        var vertShader = File.ReadAllBytes("shaders/vert.spv");
        var fragShader = File.ReadAllBytes("shaders/frag.spv");

        var vertShaderModule = CreateShaderModule(vertShader);
        var fragShaderModule = CreateShaderModule(fragShader);
        
        var vertShaderStageInfo = new PipelineShaderStageCreateInfo();
        vertShaderStageInfo.SType = StructureType.PipelineShaderStageCreateInfo;
        vertShaderStageInfo.Stage = ShaderStageFlags.VertexBit;
        vertShaderStageInfo.Module = vertShaderModule;
        vertShaderStageInfo.PName = (byte*)Marshal.StringToHGlobalAnsi("main");
        
        var fragShaderStageInfo = new PipelineShaderStageCreateInfo();
        fragShaderStageInfo.SType = StructureType.PipelineShaderStageCreateInfo;
        fragShaderStageInfo.Stage = ShaderStageFlags.FragmentBit;
        fragShaderStageInfo.Module = fragShaderModule;
        fragShaderStageInfo.PName = (byte*)Marshal.StringToHGlobalAnsi("main");
        
        PipelineShaderStageCreateInfo[] shaderStages = [vertShaderStageInfo, fragShaderStageInfo];
        
        var vertexInputInfo = new PipelineVertexInputStateCreateInfo();
        vertexInputInfo.SType = StructureType.PipelineVertexInputStateCreateInfo;
        vertexInputInfo.VertexBindingDescriptionCount = 0;
        vertexInputInfo.PVertexBindingDescriptions = null; // Optional
        vertexInputInfo.VertexAttributeDescriptionCount = 0;
        vertexInputInfo.PVertexAttributeDescriptions = null; 
        
        var inputAssembly = new PipelineInputAssemblyStateCreateInfo();
        inputAssembly.SType = StructureType.PipelineInputAssemblyStateCreateInfo;
        inputAssembly.Topology = PrimitiveTopology.TriangleList;
        inputAssembly.PrimitiveRestartEnable = false;

        var viewport = new Viewport();
        viewport.X = 0.0f;
        viewport.X = 0.0f;
        viewport.Width = _swapChainExtent.Width;
        viewport.Height = _swapChainExtent.Height;
        viewport.MinDepth = 0.0f;
        viewport.MaxDepth = 1.0f;
        
        var scissor = new Rect2D();
        scissor.Offset = new Offset2D(0, 0);
        scissor.Extent = _swapChainExtent;
        
        DynamicState[] dynamicStates = {
            DynamicState.Viewport,
            DynamicState.Scissor
        };

        var dynamicState = new PipelineDynamicStateCreateInfo();
        dynamicState.SType = StructureType.PipelineDynamicStateCreateInfo;
        dynamicState.DynamicStateCount = (uint)dynamicStates.Length;
        fixed (DynamicState* dynamicStatePtr = &dynamicStates[0])
        {
            dynamicState.PDynamicStates = dynamicStatePtr;
        }
        
        var viewportState = new PipelineViewportStateCreateInfo();
        viewportState.SType = StructureType.PipelineViewportStateCreateInfo;
        viewportState.ViewportCount = 1;
        viewportState.PViewports = &viewport;
        viewportState.ScissorCount = 1;
        viewportState.PScissors = &scissor;
        
        var rasterizer = new PipelineRasterizationStateCreateInfo();
        rasterizer.SType = StructureType.PipelineRasterizationStateCreateInfo;
        rasterizer.DepthClampEnable = false;
        rasterizer.RasterizerDiscardEnable = false;
        rasterizer.PolygonMode = PolygonMode.Fill;
        rasterizer.LineWidth = 1.0f;
        rasterizer.CullMode = CullModeFlags.BackBit;
        rasterizer.FrontFace = FrontFace.Clockwise;
        
        rasterizer.DepthBiasEnable = false;
        rasterizer.DepthBiasConstantFactor = 0.0f;
        rasterizer.DepthBiasClamp = 0.0f;
        rasterizer.DepthBiasSlopeFactor = 0.0f;
        
        var multisampling = new PipelineMultisampleStateCreateInfo();
        multisampling.SType = StructureType.PipelineMultisampleStateCreateInfo;
        multisampling.SampleShadingEnable = false;
        multisampling.RasterizationSamples = SampleCountFlags.Count1Bit;
        multisampling.MinSampleShading = 1.0f;
        multisampling.PSampleMask = null;
        multisampling.AlphaToCoverageEnable = false;
        multisampling.AlphaToOneEnable = false;
        
        var colorBlendAttachment = new PipelineColorBlendAttachmentState();
        colorBlendAttachment.ColorWriteMask = 
            ColorComponentFlags.RBit | ColorComponentFlags.GBit | 
            ColorComponentFlags.BBit | ColorComponentFlags.ABit;
        colorBlendAttachment.BlendEnable = false;
        colorBlendAttachment.SrcColorBlendFactor = BlendFactor.One;
        colorBlendAttachment.DstColorBlendFactor = BlendFactor.Zero;
        colorBlendAttachment.ColorBlendOp = BlendOp.Add;
        colorBlendAttachment.SrcAlphaBlendFactor = BlendFactor.One;
        colorBlendAttachment.DstAlphaBlendFactor = BlendFactor.Zero;
        colorBlendAttachment.AlphaBlendOp = BlendOp.Add;

        var colorBlending = new PipelineColorBlendStateCreateInfo();
        colorBlending.SType = StructureType.PipelineColorBlendStateCreateInfo;
        colorBlending.LogicOpEnable = false;
        colorBlending.LogicOp = LogicOp.Copy;
        colorBlending.AttachmentCount = 1;
        colorBlending.PAttachments = &colorBlendAttachment;
        colorBlending.BlendConstants[0] = 0.0f;
        colorBlending.BlendConstants[1] = 0.0f;
        colorBlending.BlendConstants[2] = 0.0f;
        colorBlending.BlendConstants[3] = 0.0f;
        
        var pipelineLayoutInfo = new PipelineLayoutCreateInfo();
        pipelineLayoutInfo.SType = StructureType.PipelineLayoutCreateInfo;
        pipelineLayoutInfo.SetLayoutCount = 0;
        pipelineLayoutInfo.PSetLayouts = null;
        pipelineLayoutInfo.PushConstantRangeCount = 0;
        pipelineLayoutInfo.PPushConstantRanges = null;

        if ( _vk.CreatePipelineLayout(_device, in pipelineLayoutInfo, null, out _pipelineLayout) != Result.Success) {
            throw new Exception("failed to create pipeline layout!");
        }
        
        _vk!.DestroyShaderModule(_device, fragShaderModule, null);
        _vk!.DestroyShaderModule(_device, vertShaderModule, null);
        
        Marshal.FreeHGlobal((IntPtr)vertShaderStageInfo.PName);
        Marshal.FreeHGlobal((IntPtr)fragShaderStageInfo.PName);
    }

    private ShaderModule CreateShaderModule(byte[] shaderBytes)
    {
        var createInfo = new ShaderModuleCreateInfo();
        createInfo.SType = StructureType.ShaderModuleCreateInfo;
        createInfo.CodeSize = (uint)shaderBytes.Length;
        fixed (byte* pShaderBytes = shaderBytes)
        {
            createInfo.PCode = (uint*)pShaderBytes;
        }
        
        if (_vk!.CreateShaderModule(_device, in createInfo, null, out var shaderModule) != Result.Success)
        {
            throw new Exception("failed to create shader module!");
        }
        
        return shaderModule;
    }
}