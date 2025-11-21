using System;

namespace FPSOptimization
{
    [Serializable]
    public class ProfilingData
    {
        public float fps;
        public int drawCalls;
        public int triangles;
        public long gcAllocations;
        public DateTime timestamp;
    }
}
