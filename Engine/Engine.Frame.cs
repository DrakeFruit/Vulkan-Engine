using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace VulkanEngine;

unsafe partial class Engine
{
    const int MAX_FRAMES_IN_FLIGHT = 2;
    
    private Semaphore[]? _imageAvailableSemaphores;
    private Semaphore[]? _renderFinishedSemaphores;
    private Fence[]? _inFlightFences;
    private Fence[]? _imagesInFlight;
    private int _currentFrame = 0;

    private void DrawFrame(double delta)
    {
        _vk!.WaitForFences(_device, 1, in _inFlightFences![_currentFrame], true, ulong.MaxValue);

        uint imageIndex = 0;
        _khrSwapChain!.AcquireNextImage(_device, _swapChain, ulong.MaxValue, _imageAvailableSemaphores![_currentFrame], default, ref imageIndex);

        if (_imagesInFlight![imageIndex].Handle != default)
        {
            _vk!.WaitForFences(_device, 1, in _imagesInFlight[imageIndex], true, ulong.MaxValue);
        }
        _imagesInFlight[imageIndex] = _inFlightFences[_currentFrame];

        SubmitInfo submitInfo = new()
        {
            SType = StructureType.SubmitInfo,
        };

        var waitSemaphores = stackalloc[] { _imageAvailableSemaphores[_currentFrame] };
        var waitStages = stackalloc[] { PipelineStageFlags.ColorAttachmentOutputBit };

        var buffer = _commandBuffers![imageIndex];

        submitInfo = submitInfo with
        {
            WaitSemaphoreCount = 1,
            PWaitSemaphores = waitSemaphores,
            PWaitDstStageMask = waitStages,

            CommandBufferCount = 1,
            PCommandBuffers = &buffer
        };

        var signalSemaphores = stackalloc[] { _renderFinishedSemaphores![_currentFrame] };
        submitInfo = submitInfo with
        {
            SignalSemaphoreCount = 1,
            PSignalSemaphores = signalSemaphores,
        };

        _vk!.ResetFences(_device, 1, in _inFlightFences[_currentFrame]);

        if (_vk!.QueueSubmit(_graphicsQueue, 1, in submitInfo, _inFlightFences[_currentFrame]) != Result.Success)
        {
            throw new Exception("failed to submit draw command buffer!");
        }

        var swapChains = stackalloc[] { _swapChain };
        PresentInfoKHR presentInfo = new()
        {
            SType = StructureType.PresentInfoKhr,

            WaitSemaphoreCount = 1,
            PWaitSemaphores = signalSemaphores,

            SwapchainCount = 1,
            PSwapchains = swapChains,

            PImageIndices = &imageIndex
        };

        _khrSwapChain.QueuePresent(_presentQueue, in presentInfo);

        _currentFrame = (_currentFrame + 1) % MAX_FRAMES_IN_FLIGHT;

    }
}