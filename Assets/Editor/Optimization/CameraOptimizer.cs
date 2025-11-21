using UnityEngine;

namespace FPSOptimization
{
    public static class CameraOptimizer
    {
        public static void OptimizeCameras(OptimizationSettings settings)
        {
            Camera[] cameras = Object.FindObjectsOfType<Camera>();
            
            foreach (Camera camera in cameras)
            {
                if (settings.optimizeCameraClearFlags)
                {
                    OptimizeClearFlags(camera);
                }
                
                if (settings.optimizeCullingMask)
                {
                    OptimizeCullingMask(camera);
                }
            }
            
            Debug.Log($"Camera Optimization: Optimized {cameras.Length} cameras");
        }
        
        private static void OptimizeClearFlags(Camera camera)
        {
            if (camera.clearFlags == CameraClearFlags.Skybox)
            {
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.black;
                Debug.Log($"Camera '{camera.name}' clear flags optimized");
            }
        }
        
        private static void OptimizeCullingMask(Camera camera)
        {
            // Remove UI layer from main camera culling mask if it has a separate UI camera
            // WARNING: Only enable this if you have a separate UI camera!
            if (camera.name == "Main Camera")
            {
                // Check if there's a separate UI camera
                Camera[] allCameras = Object.FindObjectsOfType<Camera>();
                bool hasUICamera = false;
                
                foreach (Camera cam in allCameras)
                {
                    if (cam != camera && cam.name.Contains("UI"))
                    {
                        hasUICamera = true;
                        break;
                    }
                }
                
                if (hasUICamera)
                {
                    int uiLayer = LayerMask.NameToLayer("UI");
                    if (uiLayer != -1)
                    {
                        camera.cullingMask &= ~(1 << uiLayer);
                        Debug.Log($"Camera '{camera.name}' culling mask optimized (UI layer removed)");
                    }
                }
                else
                {
                    Debug.LogWarning($"Camera '{camera.name}' culling mask optimization skipped: No separate UI camera found");
                }
            }
        }
    }
}
