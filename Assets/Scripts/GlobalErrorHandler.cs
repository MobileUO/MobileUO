using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using UnityEngine;

public class GlobalErrorHook : MonoBehaviour
{
    static readonly ConcurrentQueue<Action> _dispatch = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        Application.logMessageReceived -= OnLog;
        Application.logMessageReceivedThreaded -= OnLogThreaded;
        AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
        TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;
        while (_dispatch.TryDequeue(out _)) { }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        Application.logMessageReceived += OnLog;
        Application.logMessageReceivedThreaded += OnLogThreaded;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        // Ensure a host exists to run Update()
        new GameObject(nameof(GlobalErrorHook)).AddComponent<GlobalErrorHook>();
    }

    void Update()
    {
        while (_dispatch.TryDequeue(out var a)) 
            a();
    }

    static void OnLog(string condition, string stackTrace, LogType type)
    {
        if (UserPreferences.ShowErrorDetails.CurrentValue == (int)PreferenceEnums.ShowErrorDetails.On && (type == LogType.Exception || type == LogType.Error || type == LogType.Assert))
            _dispatch.Enqueue(() => ConsoleActivator.Show());
    }

    static void OnLogThreaded(string condition, string stackTrace, LogType type)
    {
        if (UserPreferences.ShowErrorDetails.CurrentValue == (int)PreferenceEnums.ShowErrorDetails.On && (type == LogType.Exception || type == LogType.Error || type == LogType.Assert))
            _dispatch.Enqueue(() => ConsoleActivator.Show());
    }

    static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        // Log to feed back into the handlers above
        if (e.ExceptionObject is Exception ex) 
            _dispatch.Enqueue(() => Debug.LogException(ex));
    }

    static void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
    {
        // Mark observed so it won’t crash later; still log it
        e.SetObserved();
        foreach (var ex in e.Exception.Flatten().InnerExceptions)
            _dispatch.Enqueue(() => Debug.LogException(ex));
    }
}