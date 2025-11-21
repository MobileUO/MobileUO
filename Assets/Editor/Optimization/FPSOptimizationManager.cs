using UnityEngine;
using UnityEditor;

namespace FPSOptimization
{
    public class FPSOptimizationManager : EditorWindow
    {
        private OptimizationSettings settings = new OptimizationSettings();
        private Vector2 scrollPosition;
        
        [MenuItem("Tools/FPS Optimization Manager")]
        public static void ShowWindow()
        {
            GetWindow<FPSOptimizationManager>("FPS Optimization");
        }
        
        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            GUILayout.Label("FPS Optimization Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            // Canvas Settings
            GUILayout.Label("Canvas Settings", EditorStyles.boldLabel);
            settings.optimizeCanvasRenderMode = EditorGUILayout.Toggle("Optimize Render Mode", settings.optimizeCanvasRenderMode);
            settings.consolidateCanvases = EditorGUILayout.Toggle("Consolidate Canvases (RISKY)", settings.consolidateCanvases);
            EditorGUILayout.HelpBox("Canvas consolidation can break UI layout. Use with caution!", MessageType.Warning);
            EditorGUILayout.Space();
            
            // Camera Settings
            GUILayout.Label("Camera Settings", EditorStyles.boldLabel);
            settings.optimizeCameraClearFlags = EditorGUILayout.Toggle("Optimize Clear Flags", settings.optimizeCameraClearFlags);
            settings.optimizeCullingMask = EditorGUILayout.Toggle("Optimize Culling Mask", settings.optimizeCullingMask);
            EditorGUILayout.Space();
            
            // Quality Settings
            GUILayout.Label("Quality Settings", EditorStyles.boldLabel);
            settings.disableShadows = EditorGUILayout.Toggle("Disable Shadows", settings.disableShadows);
            settings.disableAntiAliasing = EditorGUILayout.Toggle("Disable Anti-Aliasing", settings.disableAntiAliasing);
            settings.disableRealtimeReflections = EditorGUILayout.Toggle("Disable Realtime Reflections", settings.disableRealtimeReflections);
            settings.reduceSkinWeights = EditorGUILayout.Toggle("Reduce Skin Weights", settings.reduceSkinWeights);
            EditorGUILayout.Space();
            
            // Player Settings
            GUILayout.Label("Player Settings", EditorStyles.boldLabel);
            settings.disableAccelerometer = EditorGUILayout.Toggle("Disable Accelerometer", settings.disableAccelerometer);
            settings.optimizeGraphicsAPI = EditorGUILayout.Toggle("Optimize Graphics API", settings.optimizeGraphicsAPI);
            settings.enableMultithreadedRendering = EditorGUILayout.Toggle("Enable Multithreaded Rendering", settings.enableMultithreadedRendering);
            EditorGUILayout.Space();
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Apply All Optimizations", GUILayout.Height(40)))
            {
                ApplyOptimizations();
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        private void ApplyOptimizations()
        {
            if (EditorUtility.DisplayDialog("Apply FPS Optimizations", 
                "This will modify your project settings and scene objects. Make sure you have a backup!\n\nContinue?", 
                "Yes", "Cancel"))
            {
                Debug.Log("=== Starting FPS Optimization ===");
                
                CanvasOptimizer.OptimizeCanvases(settings);
                CameraOptimizer.OptimizeCameras(settings);
                QualitySettingsOptimizer.OptimizeQualitySettings(settings);
                PlayerSettingsOptimizer.OptimizePlayerSettings(settings);
                
                Debug.Log("=== FPS Optimization Complete ===");
                EditorUtility.DisplayDialog("Success", "FPS optimizations applied successfully!", "OK");
            }
        }
    }
}
