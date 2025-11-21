using UnityEngine;
using UnityEngine.UI;

namespace FPSOptimization
{
    public static class CanvasOptimizer
    {
        public static void OptimizeCanvases(OptimizationSettings settings)
        {
            Canvas[] canvases = Object.FindObjectsOfType<Canvas>();
            
            foreach (Canvas canvas in canvases)
            {
                if (settings.optimizeCanvasRenderMode)
                {
                    OptimizeRenderMode(canvas);
                }
            }
            
            Debug.Log($"Canvas Optimization: Optimized {canvases.Length} canvases");
        }
        
        private static void OptimizeRenderMode(Canvas canvas)
        {
            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                Camera mainCamera = Camera.main;
                if (mainCamera == null)
                {
                    Debug.LogWarning($"Canvas '{canvas.name}' optimization skipped: No main camera found");
                    return;
                }
                
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = mainCamera;
                Debug.Log($"Canvas '{canvas.name}' render mode changed to ScreenSpaceCamera");
            }
        }
    }
}
