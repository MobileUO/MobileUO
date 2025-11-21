using UnityEngine;
using UnityEditor;

namespace FPSOptimization
{
    public static class PlayerSettingsOptimizer
    {
        public static void OptimizePlayerSettings(OptimizationSettings settings)
        {
            if (settings.disableAccelerometer)
            {
                PlayerSettings.accelerometerFrequency = 0;
                Debug.Log("Player Settings: Accelerometer disabled");
            }
            
            if (settings.enableMultithreadedRendering)
            {
                PlayerSettings.MTRendering = true;
                Debug.Log("Player Settings: Multithreaded rendering enabled");
            }
            
            if (settings.optimizeGraphicsAPI)
            {
                PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new UnityEngine.Rendering.GraphicsDeviceType[] 
                { 
                    UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3,
                    UnityEngine.Rendering.GraphicsDeviceType.Vulkan
                });
                Debug.Log("Player Settings: Graphics API optimized (OpenGLES3, Vulkan)");
            }
            
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            Debug.Log("Player Settings: Target architecture set to ARM64");
        }
    }
}
