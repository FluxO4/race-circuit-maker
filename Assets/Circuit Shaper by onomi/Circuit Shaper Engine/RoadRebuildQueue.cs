using System.Collections.Generic;
using OnomiCircuitShaper.Engine.Data;

namespace OnomiCircuitShaper.Engine
{
    /// <summary>
    /// Static communication channel for road rebuild requests.
    /// Provides a decoupled way for CircuitPoints to signal that roads need rebuilding
    /// without managing event subscriptions. Thread-safe singleton pattern.
    /// </summary>
    public static class RoadRebuildQueue
    {
        private static HashSet<RoadData> _dirtyRoads = new HashSet<RoadData>();
        private static object _lock = new object();
        
        /// <summary>
        /// Marks a road as needing rebuild. Safe to call multiple times with the same road.
        /// </summary>
        /// <param name="roadData">The road data to mark as dirty.</param>
        public static void MarkDirty(RoadData roadData)
        {
            if (roadData == null) return;
            
            lock (_lock)
            {
                _dirtyRoads.Add(roadData);
            }
        }
        
        /// <summary>
        /// Gets all dirty roads and clears the queue atomically.
        /// </summary>
        /// <returns>List of all roads that need rebuilding.</returns>
        public static List<RoadData> GetAndClearDirtyRoads()
        {
            lock (_lock)
            {
                var result = new List<RoadData>(_dirtyRoads);
                _dirtyRoads.Clear();
                return result;
            }
        }
        
        /// <summary>
        /// Clears all pending rebuild requests. Call this when edit session ends.
        /// </summary>
        public static void Clear()
        {
            lock (_lock)
            {
                _dirtyRoads.Clear();
            }
        }
        
        /// <summary>
        /// Checks if there are any dirty roads without consuming them.
        /// </summary>
        /// <returns>True if there are roads waiting to be rebuilt.</returns>
        public static bool HasDirtyRoads()
        {
            lock (_lock)
            {
                return _dirtyRoads.Count > 0;
            }
        }
        
        /// <summary>
        /// Gets the count of dirty roads without consuming them.
        /// </summary>
        public static int DirtyRoadCount
        {
            get
            {
                lock (_lock)
                {
                    return _dirtyRoads.Count;
                }
            }
        }
    }
}
