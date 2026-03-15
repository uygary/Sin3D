using System;
using System.Diagnostics;
using System.Reflection;
using Microsoft.Xna.Framework.Graphics;

namespace Sin3d;

/// <summary>
/// Extracts raw Vulkan handles from MonoGame's <see cref="GraphicsDevice"/>
/// using reflection. All reflection is centralized here so that if MonoGame
/// internals change between versions, only this file needs to be updated.
/// </summary>
public static class VulkanHandleExtractor
{
    /// <summary>
    /// The extracted Vulkan handles needed for OpenXR graphics binding.
    /// </summary>
    public readonly struct VulkanHandles
    {
        public readonly IntPtr VkInstance;
        public readonly IntPtr VkPhysicalDevice;
        public readonly IntPtr VkDevice;
        public readonly uint QueueFamilyIndex;
        public readonly uint QueueIndex;

        public VulkanHandles(IntPtr vkInstance, IntPtr vkPhysicalDevice, IntPtr vkDevice,
                             uint queueFamilyIndex, uint queueIndex)
        {
            VkInstance = vkInstance;
            VkPhysicalDevice = vkPhysicalDevice;
            VkDevice = vkDevice;
            QueueFamilyIndex = queueFamilyIndex;
            QueueIndex = queueIndex;
        }
    }

    /// <summary>
    /// Attempts to extract Vulkan handles from the given <see cref="GraphicsDevice"/>.
    /// Returns null if extraction fails (e.g. not running on the Vulkan backend).
    /// </summary>
    public static VulkanHandles? TryExtract(GraphicsDevice graphicsDevice)
    {
        try
        {
            var gdType = graphicsDevice.GetType();
            const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;

            // MonoGame DesktopVK stores the Vulkan context in various private fields.
            // The exact names depend on the MonoGame version; we try known candidates.
            IntPtr vkInstance = GetIntPtrField(gdType, graphicsDevice, flags,
                "_vkInstance", "VkInstance", "_vulkanInstance");

            IntPtr vkPhysicalDevice = GetIntPtrField(gdType, graphicsDevice, flags,
                "_vkPhysicalDevice", "VkPhysicalDevice", "_physicalDevice");

            IntPtr vkDevice = GetIntPtrField(gdType, graphicsDevice, flags,
                "_vkDevice", "VkDevice", "_logicalDevice", "_device");

            uint queueFamilyIndex = GetUintField(gdType, graphicsDevice, flags,
                "_queueFamilyIndex", "_graphicsQueueFamilyIndex", "QueueFamilyIndex");

            uint queueIndex = GetUintField(gdType, graphicsDevice, flags,
                "_queueIndex", "_graphicsQueueIndex", "QueueIndex");

            if (vkInstance == IntPtr.Zero || vkPhysicalDevice == IntPtr.Zero || vkDevice == IntPtr.Zero)
            {
                Debug.WriteLine("[VulkanHandleExtractor] Failed: one or more Vulkan handles are null.");
                return null;
            }

            Debug.WriteLine($"[VulkanHandleExtractor] Extracted: VkInstance={vkInstance}, " +
                            $"VkPhysicalDevice={vkPhysicalDevice}, VkDevice={vkDevice}, " +
                            $"QueueFamily={queueFamilyIndex}, Queue={queueIndex}");
            return new VulkanHandles(vkInstance, vkPhysicalDevice, vkDevice, queueFamilyIndex, queueIndex);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[VulkanHandleExtractor] Reflection failed: {ex.Message}");
            return null;
        }
    }

    private static IntPtr GetIntPtrField(Type type, object instance, BindingFlags flags,
                                          params string[] candidates)
    {
        foreach (var name in candidates)
        {
            var field = type.GetField(name, flags);
            if (field is not null)
            {
                var value = field.GetValue(instance);
                if (value is IntPtr ptr)
                {
                    return ptr;
                }
            }

            var prop = type.GetProperty(name, flags | BindingFlags.Public);
            if (prop is not null)
            {
                var value = prop.GetValue(instance);
                if (value is IntPtr ptr)
                {
                    return ptr;
                }
            }
        }

        return IntPtr.Zero;
    }

    private static uint GetUintField(Type type, object instance, BindingFlags flags,
                                      params string[] candidates)
    {
        foreach (var name in candidates)
        {
            var field = type.GetField(name, flags);
            if (field is not null)
            {
                var value = field.GetValue(instance);
                if (value is uint u)
                {
                    return u;
                }
                if (value is int i)
                {
                    return (uint)i;
                }
            }

            var prop = type.GetProperty(name, flags | BindingFlags.Public);
            if (prop is not null)
            {
                var value = prop.GetValue(instance);
                if (value is uint u)
                {
                    return u;
                }
                if (value is int i)
                {
                    return (uint)i;
                }
            }
        }

        return 0;
    }
}
