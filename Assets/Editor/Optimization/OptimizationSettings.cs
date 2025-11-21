using UnityEngine;

namespace FPSOptimization
{
    [System.Serializable]
    public class OptimizationSettings
    {
        [Header("Canvas Settings")]
        public bool optimizeCanvasRenderMode = true;
        public bool consolidateCanvases = false; // Disabled by default - can break UI
        
        [Header("Camera Settings")]
        public bool optimizeCameraClearFlags = true;
        public bool optimizeCullingMask = true;
        
        [Header("Quality Settings")]
        public bool disableShadows = true;
        public bool disableAntiAliasing = true;
        public bool disableRealtimeReflections = true;
        public bool reduceSkinWeights = true;
        
        [Header("Player Settings")]
        public bool disableAccelerometer = true;
        public bool optimizeGraphicsAPI = true;
        public bool enableMultithreadedRendering = true;
        
        [Header("Build Settings")]
        public bool stripEngineCode = true;
        public bool enableManagedStripping = true;
    }
}
