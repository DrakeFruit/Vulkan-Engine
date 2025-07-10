namespace VulkanEngine;

unsafe partial class Engine
{
    private Framebuffer[]? _swapChainFramebuffers;
    private CommandPool _commandPool;
    private CommandBuffer[]? _commandBuffers;
    
    private void CreateFrameBuffers()
    {
        _swapChainFramebuffers = new Framebuffer[_swapChainImageViews!.Length];

        for (int i = 0; i < _swapChainImageViews.Length; i++)
        {
            var attachment = _swapChainImageViews[i];

            FramebufferCreateInfo framebufferInfo = new()
            {
                SType = StructureType.FramebufferCreateInfo,
                RenderPass = _renderPass,
                AttachmentCount = 1,
                PAttachments = &attachment,
                Width = _swapChainExtent.Width,
                Height = _swapChainExtent.Height,
                Layers = 1,
            };

            if (_vk!.CreateFramebuffer(_device, in framebufferInfo, null, out _swapChainFramebuffers[i]) != Result.Success)
            {
                throw new Exception("failed to create framebuffer!");
            }
        }
    }

    private void CreateCommandPool()
    {
        var queueFamiliyIndicies = FindQueueFamilies(_physicalDevice);

        CommandPoolCreateInfo poolInfo = new()
        {
            SType = StructureType.CommandPoolCreateInfo,
            QueueFamilyIndex = queueFamiliyIndicies.GraphicsFamily!.Value,
        };

        if (_vk!.CreateCommandPool(_device, in poolInfo, null, out _commandPool) != Result.Success)
        {
            throw new Exception("failed to create command pool!");
        }
    }

    private void CreateCommandBuffers()
    {
        _commandBuffers = new CommandBuffer[_swapChainFramebuffers!.Length];

        CommandBufferAllocateInfo allocInfo = new()
        {
            SType = StructureType.CommandBufferAllocateInfo,
            CommandPool = _commandPool,
            Level = CommandBufferLevel.Primary,
            CommandBufferCount = (uint)_commandBuffers.Length,
        };

        fixed (CommandBuffer* commandBuffersPtr = _commandBuffers)
        {
            if (_vk!.AllocateCommandBuffers(_device, in allocInfo, commandBuffersPtr) != Result.Success)
            {
                throw new Exception("failed to allocate command buffers!");
            }
        }

        for (var i = 0; i < _commandBuffers.Length; i++)
        {
            CommandBufferBeginInfo beginInfo = new()
            {
                SType = StructureType.CommandBufferBeginInfo,
            };

            if (_vk!.BeginCommandBuffer(_commandBuffers[i], in beginInfo) != Result.Success)
            {
                throw new Exception("failed to begin recording command buffer!");
            }

            RenderPassBeginInfo renderPassInfo = new()
            {
                SType = StructureType.RenderPassBeginInfo,
                RenderPass = _renderPass,
                Framebuffer = _swapChainFramebuffers[i],
                RenderArea =
                {
                    Offset = { X = 0, Y = 0 },
                    Extent = _swapChainExtent,
                }
            };

            ClearValue clearColor = new()
            {
                Color = new ClearColorValue{ Float32_0 = 0, Float32_1 = 0, Float32_2 = 0, Float32_3 = 1 }
            };

            renderPassInfo.ClearValueCount = 1;
            renderPassInfo.PClearValues = &clearColor;

            _vk!.CmdBeginRenderPass(_commandBuffers[i], &renderPassInfo, SubpassContents.Inline);
            _vk!.CmdBindPipeline(_commandBuffers[i], PipelineBindPoint.Graphics, _graphicsPipeline);
            _vk!.CmdDraw(_commandBuffers[i], 3, 1, 0, 0);
            _vk!.CmdEndRenderPass(_commandBuffers[i]);

            if (_vk!.EndCommandBuffer(_commandBuffers[i]) != Result.Success)
            {
                throw new Exception("failed to record command buffer!");
            }
        }
    }
}