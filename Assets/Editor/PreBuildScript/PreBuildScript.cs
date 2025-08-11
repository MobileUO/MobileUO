using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class PreBuildScript : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    static PreBuildScript()
    {
        // Register event handlers for play mode state changes
        // MobileUO: TODO: does not seem to work unless you restart Unity - commenting out for now
        //EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    public void OnPreprocessBuild(BuildReport report)
    {
        // Apply settings at build time
        ApplySettingsFromFile(report.summary.platform);
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            // Apply settings when entering play mode
            ApplySettingsFromFile(EditorUserBuildSettings.activeBuildTarget);
        }
    }

    private static void ApplySettingsFromFile(BuildTarget platform)
    {
        string baseVersionNumber = File.ReadAllText("Assets/Scripts/VersionNumber.txt").Trim();
        string buildNumber = GetCommandLineArg("-GITHUB_BUILD_NUMBER", "0");
        string fullVersionNumber = $"{baseVersionNumber}+{buildNumber}";

        Debug.Log($"Version Number: {fullVersionNumber}");

        PlayerSettings.bundleVersion = fullVersionNumber;
        PlayerSettings.Android.bundleVersionCode = int.Parse(buildNumber);
        PlayerSettings.iOS.buildNumber = buildNumber;

        if (platform == BuildTarget.Android)
        {
            var environment = "Production";

            var isDevelopmentEnvironment = EditorUserBuildSettings.development;
            var buildEnvironment = GetCommandLineArg("-BUILD_ENV");

            Debug.Log($"IsDevelopmentEnvironment: {isDevelopmentEnvironment}");
            Debug.Log($"BuildEnvironment: {buildEnvironment}");

            if (isDevelopmentEnvironment)
            {
                environment = "Development";
            } 
            else if(!string.IsNullOrEmpty(buildEnvironment))
            {
                environment = buildEnvironment;
            }

            if (buildEnvironment == "Development" || buildEnvironment == "Staging")
            {
                Debug.Log($"Setting EditorUserBuildSettings to development: {buildEnvironment}");
                EditorUserBuildSettings.development = true;
                EditorUserBuildSettings.connectProfiler = true;
                EditorUserBuildSettings.allowDebugging = true;
            }

            string jsonFileName = $"appsettings.{environment}.json";

            Debug.Log($"Environment: {environment}");
            Debug.Log($"Reading settings from: {jsonFileName}");

            string jsonPath = Path.Combine(Application.dataPath, jsonFileName);
            Debug.Log($"Appsettings path: {jsonPath}");

            if (File.Exists(jsonPath))
            {
                string jsonContent = File.ReadAllText(jsonPath);
                AppSettings settings = JsonUtility.FromJson<AppSettings>(jsonContent);

                Debug.Log($"Applying settings: AndroidPackageName = {settings.AndroidPackageName}, ProductName = {settings.ProductName}, AndroidUseCustomKeystore = {settings.AndroidUseCustomKeystore}");

                PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, settings.AndroidPackageName);
                PlayerSettings.productName = settings.ProductName;
                PlayerSettings.Android.useCustomKeystore = settings.AndroidUseCustomKeystore;
            }
            else
            {
                Debug.LogWarning($"{jsonFileName} file not found.");
            }
        }
    }

    private static string GetCommandLineArg(string name, string defaultValue = null)
    {
        var args = Environment.GetCommandLineArgs();
        int index = Array.IndexOf(args, name);
        return (index >= 0 && index < args.Length - 1) ? args[index + 1] : defaultValue;
    }

    [System.Serializable]
    private class AppSettings
    {
        public string AndroidPackageName;
        public string ProductName;
        public bool AndroidUseCustomKeystore;
    }
}