using Consolation;
using UnityEngine;

public static class ConsoleActivator
{
    public static void Show(string condition, string stackTrace)
    {
        var console = Object.FindFirstObjectByType<Console>();

        if (!console)
        {
            var go = new GameObject("Console");
            console = go.AddComponent<Console>(); // defaults from inspector/fields
        }

        console.Show();
    }
}