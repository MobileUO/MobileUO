using UnityEngine;
using SQLitePCL;

public static class SqliteBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        // Wire up SQLitePCLRaw for Android/iOS
        // Force SQLitePCLRaw to use the e_sqlite3 provider
        raw.SetProvider(new SQLite3Provider_e_sqlite3());
        //Batteries_V2.Init();

        Debug.Log("[SqliteBootstrap] SQLitePCLRaw initialized");
    }
}