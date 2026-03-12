// Editor & Player safe; ENABLE_PROFILER turns real scopes on/off.
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Profiling;

namespace MobileUO.Profiling
{
    internal static class UnityProfiler
    {
        public static readonly ProfilerMarker Mk_Flush = new("UnityBatcher.Flush");
        public static readonly ProfilerMarker Mk_FlushMesh = new("UnityBatcher.FlushMeshBatch");
        public static readonly ProfilerMarker Mk_DrawRun = new("UnityBatcher.DrawRun");
        public static readonly ProfilerMarker Mk_ApplyStates = new("UnityBatcher.ApplyStates");
        public static readonly ProfilerMarker Mk_CollectMesh = new("UnityBatcher.CollectMeshPath");
        public static readonly ProfilerMarker Mk_DrawTexture = new("UnityBatcher.DrawTexturePath");
        public static readonly ProfilerMarker Mk_ComputeRects = new("UnityBatcher.ComputeRects");
        public static readonly ProfilerMarker Mk_MeshPopulate = new("MeshHolder.Populate");
        public static readonly ProfilerMarker Mk_EnsureCap = new("MeshHolder.EnsureCapacity");
        public static readonly ProfilerMarker Mk_SetVB = new("Mesh.SetVertexBufferData");
        public static readonly ProfilerMarker Mk_SetIB = new("Mesh.SetIndexBufferData");
        public static readonly ProfilerMarker Mk_SetSM = new("Mesh.SetSubMesh");

#if ENABLE_PROFILER
        private static readonly Dictionary<string, ProfilerMarker> _cache = new();
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AutoScopeGuard Auto(in ProfilerMarker marker)
        {
#if ENABLE_PROFILER
            return new AutoScopeGuard(marker);
#else
            return default;
#endif
        }

        // Only use this in non-hot paths
        public static AutoScopeGuard Auto(string name)
        {
#if ENABLE_PROFILER
            if (!_cache.TryGetValue(name, out var m))
            {
                m = new ProfilerMarker(name);
                _cache[name] = m;
            }
            return new AutoScopeGuard(m);
#else
            return default;
#endif
        }

        public readonly struct AutoScopeGuard : IDisposable
        {
#if ENABLE_PROFILER
            private readonly ProfilerMarker.AutoScope _scope;
            public AutoScopeGuard(ProfilerMarker marker) => _scope = marker.Auto();
            public void Dispose() => _scope.Dispose();
#else
            public void Dispose() { }
#endif
        }
    }
}
