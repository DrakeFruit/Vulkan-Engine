using System.Runtime.InteropServices;
using Silk.NET.Core.Native;

namespace VulkanEngine;

unsafe partial class Engine
{
    private RenderPass _renderPass;
    
    private PipelineLayout _pipelineLayout;
    private Pipeline _graphicsPipeline;

    private void CreateRenderPass()
    {
        var colorAttachment = new AttachmentDescription();
        colorAttachment.Format = _swapChainImageFormat;
        colorAttachment.Samples = SampleCountFlags.Count1Bit;
        colorAttachment.LoadOp = AttachmentLoadOp.Clear;
        colorAttachment.StoreOp = AttachmentStoreOp.Store;
        colorAttachment.StencilLoadOp = AttachmentLoadOp.DontCare;
        colorAttachment.StencilStoreOp = AttachmentStoreOp.DontCare;
        colorAttachment.InitialLayout = ImageLayout.Undefined;
        colorAttachment.FinalLayout = ImageLayout.PresentSrcKhr;
        
        var colorAttachmentRef = new AttachmentReference();
        colorAttachmentRef.Attachment = 0;
        colorAttachmentRef.Layout = ImageLayout.ColorAttachmentOptimal;
        
        var subpass = new SubpassDescription();
        subpass.PipelineBindPoint = PipelineBindPoint.Graphics;
        subpass.ColorAttachmentCount = 1;
        subpass.PColorAttachments = &colorAttachmentRef;
        
        var renderPassInfo = new RenderPassCreateInfo();
        renderPassInfo.SType = StructureType.RenderPassCreateInfo;
        renderPassInfo.AttachmentCount = 1;
        renderPassInfo.PAttachments = &colorAttachment;
        renderPassInfo.SubpassCount = 1;
        renderPassInfo.PSubpasses = &subpass;

        if ( _vk!.CreateRenderPass(_device, in renderPassInfo, null, out _renderPass) != Result.Success) {
            throw new Exception("failed to create render pass!");
        }
    }
    
    private void CreateGraphicsPipeline()
    {
        var vertShader = File.ReadAllBytes("shaders/vert.spv");
        var fragShader = File.ReadAllBytes("shaders/frag.spv");

        var vertShaderModule = CreateShaderModule(vertShader);
        var fragShaderModule = CreateShaderModule(fragShader);
        
        var vertShaderStageInfo = new PipelineShaderStageCreateInfo
        {
            SType = StructureType.PipelineShaderStageCreateInfo,
            Stage = ShaderStageFlags.VertexBit,
            Module = vertShaderModule,
            PName = (byte*)SilkMarshal.StringToPtr("main")
        };

        var fragShaderStageInfo = new PipelineShaderStageCreateInfo
        {
            SType = StructureType.PipelineShaderStageCreateInfo,
            Stage = ShaderStageFlags.FragmentBit,
            Module = fragShaderModule,
            PName = (byte*)SilkMarshal.StringToPtr("main")
        };

        var shaderStages = stackalloc[]
        {
            vertShaderStageInfo,
            fragShaderStageInfo
        };
        
        var vertexInputInfo = new PipelineVertexInputStateCreateInfo
        {
            SType = StructureType.PipelineVertexInputStateCreateInfo,
            VertexBindingDescriptionCount = 0,
            PVertexBindingDescriptions = null,
            VertexAttributeDescriptionCount = 0,
            PVertexAttributeDescriptions = null
        };

        var inputAssembly = new PipelineInputAssemblyStateCreateInfo
        {
            SType = StructureType.PipelineInputAssemblyStateCreateInfo,
            Topology = PrimitiveTopology.TriangleList,
            PrimitiveRestartEnable = false
        };

        var viewport = new Viewport
        {
            X = 0.0f,
            Y = 0.0f,
            Width = _swapChainExtent.Width,
            Height = _swapChainExtent.Height,
            MinDepth = 0.0f,
            MaxDepth = 1.0f
        };
        
        var scissor = new Rect2D
        {
            Offset = { X = 0, Y = 0 },
            Extent = _swapChainExtent
        };
        
        var viewportState = new PipelineViewportStateCreateInfo
        {
            SType = StructureType.PipelineViewportStateCreateInfo,
            ViewportCount = 1,
            PViewports = &viewport,
            ScissorCount = 1,
            PScissors = &scissor
        };

        var rasterizer = new PipelineRasterizationStateCreateInfo
        {
            SType = StructureType.PipelineRasterizationStateCreateInfo,
            DepthClampEnable = false,
            RasterizerDiscardEnable = false,
            PolygonMode = PolygonMode.Fill,
            LineWidth = 1.0f,
            CullMode = CullModeFlags.BackBit,
            FrontFace = FrontFace.Clockwise,
            DepthBiasEnable = false,
            DepthBiasConstantFactor = 0.0f,
            DepthBiasClamp = 0.0f,
            DepthBiasSlopeFactor = 0.0f
        };

        var multisampling = new PipelineMultisampleStateCreateInfo
        {
            SType = StructureType.PipelineMultisampleStateCreateInfo,
            SampleShadingEnable = false,
            RasterizationSamples = SampleCountFlags.Count1Bit,
            MinSampleShading = 1.0f,
            PSampleMask = null,
            AlphaToCoverageEnable = false,
            AlphaToOneEnable = false
        };

        var colorBlendAttachment = new PipelineColorBlendAttachmentState
        {
            ColorWriteMask = ColorComponentFlags.RBit | ColorComponentFlags.GBit | 
                             ColorComponentFlags.BBit | ColorComponentFlags.ABit,
            BlendEnable = false,
            SrcColorBlendFactor = BlendFactor.One,
            DstColorBlendFactor = BlendFactor.Zero,
            ColorBlendOp = BlendOp.Add,
            SrcAlphaBlendFactor = BlendFactor.One,
            DstAlphaBlendFactor = BlendFactor.Zero,
            AlphaBlendOp = BlendOp.Add
        };

        var colorBlending = new PipelineColorBlendStateCreateInfo
        {
            SType = StructureType.PipelineColorBlendStateCreateInfo,
            LogicOpEnable = false,
            LogicOp = LogicOp.Copy,
            AttachmentCount = 1,
            PAttachments = &colorBlendAttachment
        };
        
        colorBlending.BlendConstants[0] = 0.0f;
        colorBlending.BlendConstants[1] = 0.0f;
        colorBlending.BlendConstants[2] = 0.0f;
        colorBlending.BlendConstants[3] = 0.0f;
        
        var pipelineLayoutInfo = new PipelineLayoutCreateInfo
        {
            SType = StructureType.PipelineLayoutCreateInfo,
            SetLayoutCount = 0,
            PSetLayouts = null,
            PushConstantRangeCount = 0,
            PPushConstantRanges = null
        };

        if ( _vk!.CreatePipelineLayout(_device, in pipelineLayoutInfo, null, out _pipelineLayout) != Result.Success) {
            throw new Exception("failed to create pipeline layout!");
        }

        var pipelineInfo = new GraphicsPipelineCreateInfo
        {
            SType = StructureType.GraphicsPipelineCreateInfo,
            StageCount = 2,
            PStages = shaderStages, 
            PVertexInputState = &vertexInputInfo,
            PInputAssemblyState = &inputAssembly,
            PViewportState = &viewportState,
            PRasterizationState = &rasterizer,
            PMultisampleState = &multisampling,
            PDepthStencilState = null,
            PColorBlendState = &colorBlending,
            Layout = _pipelineLayout,
            RenderPass = _renderPass,
            Subpass = 0,
            BasePipelineHandle = default,
        };
        
        if (_vk!.CreateGraphicsPipelines(_device, default, 1, 
                in pipelineInfo, null, out _graphicsPipeline) != Result.Success) {
            throw new Exception("failed to create graphics pipeline!");
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