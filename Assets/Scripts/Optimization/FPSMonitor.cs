using UnityEngine;
using System;
using System.Text;

namespace FPSOptimization
{
    public class FPSMonitor : MonoBehaviour
    {
        [Header("Display Settings")]
        public bool showOverlay = true;
        public Color textColor = Color.green;
        public int fontSize = 24;
        public Vector2 position = new Vector2(10, 10);
        public bool showTimestamp = false;

        [Header("Profiling Settings")]
        [Tooltip("Enables extra profiling stats like GC allocations.")]
        public bool enableProfiling = true;

        [Tooltip("How often to update GC and other heavy stats (in seconds).")]
        public float updateInterval = 1.0f;

        [Tooltip("Show advanced GC info: gen counts, allocation rate, etc.")]
        public bool showAdvancedGC = true;

        // Smoothed FPS
        private float deltaTime = 0.0f;

        // GUI style
        private GUIStyle style;

        // Basic profiling data
        private ProfilingData currentData = new ProfilingData();
        private float profilerTimer = 0f;

        // Advanced GC tracking
        private long lastTotalMemory = 0;
        private long allocationDelta = 0;
        private long peakMemory = 0;

        private float allocationRateMBPerSec = 0f;

        private int[] gcCollectionCounts = new int[3];
        private int[] prevGcCollectionCounts = new int[3];
        private int gcEventsSinceLastSample = 0;

        private float timeSinceLastGc = 0f;

        // GC interval stats
        private float lastGcInterval = 0f;
        private float totalGcInterval = 0f;
        private int gcIntervalCount = 0;

        private void Start()
        {
            style = new GUIStyle
            {
                alignment = TextAnchor.UpperLeft,
                fontSize = fontSize
            };
            style.normal.textColor = textColor;

            currentData.timestamp = DateTime.Now;

            for (int gen = 0; gen <= 2; gen++)
            {
                prevGcCollectionCounts[gen] = GC.CollectionCount(gen);
                gcCollectionCounts[gen] = prevGcCollectionCounts[gen];
            }

            lastTotalMemory = GC.GetTotalMemory(false);
            peakMemory = lastTotalMemory;
        }

        private void Update()
        {
            // Smooth FPS
            deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
            // Track time since last GC
            timeSinceLastGc += Time.unscaledDeltaTime;

            if (!enableProfiling)
                return;

            profilerTimer += Time.deltaTime;

            if (profilerTimer >= updateInterval)
            {
                CollectProfilingData();
                profilerTimer = 0f;
            }
        }

        private void CollectProfilingData()
        {
            float fps = deltaTime > 0f ? (1.0f / deltaTime) : 0f;
            long totalMemory = GC.GetTotalMemory(false);

            allocationDelta = totalMemory - lastTotalMemory;
            peakMemory = Math.Max(peakMemory, totalMemory);

            if (updateInterval > 0f)
            {
                allocationRateMBPerSec = (allocationDelta / (1024f * 1024f)) / updateInterval;
            }
            else
            {
                allocationRateMBPerSec = 0f;
            }

            gcEventsSinceLastSample = 0;

            for (int gen = 0; gen <= 2; gen++)
            {
                int count = GC.CollectionCount(gen);
                gcCollectionCounts[gen] = count;

                int delta = count - prevGcCollectionCounts[gen];
                if (delta > 0)
                {
                    gcEventsSinceLastSample += delta;
                }

                prevGcCollectionCounts[gen] = count;
            }

            // GC happened during this interval
            if (gcEventsSinceLastSample > 0)
            {
                // How long it took since the previous GC
                lastGcInterval = timeSinceLastGc;

                totalGcInterval += lastGcInterval;
                gcIntervalCount++;

                // Reset timer since last GC
                timeSinceLastGc = 0f;
            }

            lastTotalMemory = totalMemory;

            currentData.fps = fps;
            currentData.drawCalls = 0;
            currentData.triangles = 0;
            currentData.gcAllocations = totalMemory;
            currentData.timestamp = DateTime.Now;
        }

        // Reset stats back to a "fresh" baseline
        private void ResetStats()
        {
            lastTotalMemory = GC.GetTotalMemory(false);
            peakMemory = lastTotalMemory;

            allocationDelta = 0;
            allocationRateMBPerSec = 0f;

            timeSinceLastGc = 0f;

            lastGcInterval = 0f;
            totalGcInterval = 0f;
            gcIntervalCount = 0;

            for (int gen = 0; gen <= 2; gen++)
            {
                prevGcCollectionCounts[gen] = GC.CollectionCount(gen);
                gcCollectionCounts[gen] = prevGcCollectionCounts[gen];
            }

            currentData.timestamp = DateTime.Now;
        }

        // Force a GC sweep and then reset stats
        private void ForceGcAndReset()
        {
            // Full GC sweep
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            // After forcing GC, reset all our tracking
            ResetStats();
        }

        private void OnGUI()
        {
            if (!showOverlay)
                return;

            float fps = deltaTime > 0f ? (1.0f / deltaTime) : 0f;

            StringBuilder sb = new StringBuilder(256);
            sb.AppendFormat("FPS: {0:0.}\n", fps);

            if (enableProfiling)
            {
                float gcMb = currentData.gcAllocations / (1024f * 1024f);
                float peakMb = peakMemory / (1024f * 1024f);
                float deltaMb = allocationDelta / (1024f * 1024f);

                sb.AppendFormat("Managed: {0:0.00} MB\n", gcMb);
                sb.AppendFormat("Peak:    {0:0.00} MB\n", peakMb);

                if (showAdvancedGC)
                {
                    float avgGcInterval = gcIntervalCount > 0
                        ? totalGcInterval / gcIntervalCount
                        : 0f;

                    sb.AppendFormat("ΔAlloc:  {0:+0.000;-0.000;0.000} MB\n", deltaMb);
                    sb.AppendFormat("Rate:    {0:+0.000;-0.000;0.000} MB/s\n", allocationRateMBPerSec);
                    sb.AppendFormat("GC Gen0: {0}\n", gcCollectionCounts[0]);
                    sb.AppendFormat("GC Gen1: {0}\n", gcCollectionCounts[1]);
                    sb.AppendFormat("GC Gen2: {0}\n", gcCollectionCounts[2]);
                    sb.AppendFormat("GC events (last {0:0.0}s): {1}\n", updateInterval, gcEventsSinceLastSample);
                    sb.AppendFormat("Time since last GC: {0:0.0}s\n", timeSinceLastGc);
                    sb.AppendFormat("Last GC interval:   {0:0.0}s\n", lastGcInterval);
                    sb.AppendFormat("Avg GC interval:    {0:0.0}s\n", avgGcInterval);
                }

                if (showTimestamp)
                {
                    sb.AppendFormat("Last Sample: {0:HH:mm:ss}\n", currentData.timestamp);
                }
            }

            string text = sb.ToString();
            Vector2 size = style.CalcSize(new GUIContent(text));
            Rect rect = new Rect(position.x, position.y, size.x + 10, size.y + 10);

            GUI.Label(rect, text, style);

            // Buttons to the right of the overlay
            Rect resetRect = new Rect(rect.xMax + 10, rect.y, 80, 30);
            if (GUI.Button(resetRect, "Reset"))
            {
                ResetStats();
            }

            Rect gcRect = new Rect(resetRect.xMax + 5, rect.y, 100, 30);
            if (GUI.Button(gcRect, "GC+Reset"))
            {
                ForceGcAndReset();
            }
        }
    }
}
