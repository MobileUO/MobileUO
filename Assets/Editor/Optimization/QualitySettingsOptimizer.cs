using UnityEngine;

namespace FPSOptimization
{
    public static class QualitySettingsOptimizer
    {
        public static void OptimizeQualitySettings(OptimizationSettings settings)
        {
            if (settings.disableShadows)
            {
                QualitySettings.shadows = ShadowQuality.Disable;
                Debug.Log("Quality Settings: Shadows disabled");
            }
            
            if (settings.disableAntiAliasing)
            {
                QualitySettings.antiAliasing = 0;
                Debug.Log("Quality Settings: Anti-aliasing disabled");
            }
            
            if (settings.disableRealtimeReflections)
            {
                QualitySettings.realtimeReflectionProbes = false;
                Debug.Log("Quality Settings: Realtime reflections disabled");
            }
            
            if (settings.reduceSkinWeights)
            {
                QualitySettings.skinWeights = SkinWeights.TwoBones;
                Debug.Log("Quality Settings: Skin weights reduced to 2 bones");
            }
            
            QualitySettings.vSyncCount = 0;
            Debug.Log("Quality Settings: VSync disabled");
        }
    }
}
