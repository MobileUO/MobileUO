using UnityEngine;
using SQLitePCL;

public static class SqliteBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        // Wire up SQLitePCLRaw for Android/iOS
        Batteries_V2.Init();
        Debug.Log("[SqliteBootstrap] SQLitePCLRaw initialized");
    }
}