using Consolation;
using UnityEngine;

public static class ConsoleActivator
{
    public static void Show(bool openLastStackTrace = true)
    {
        var console = Object.FindFirstObjectByType<Console>();

        if (!console)
        {
            var go = new GameObject("Console");
            console = go.AddComponent<Console>(); // defaults from inspector/fields
        }

        console.Show(openLastStackTrace);
    }
}