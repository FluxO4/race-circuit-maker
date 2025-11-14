using System.Collections.Generic;
using OnomiCircuitShaper.Engine.Data;
using OnomiCircuitShaper.Engine.EditRealm;

namespace OnomiCircuitShaper.Engine
{
    /// <summary>
    /// Static communication channel for road rebuild requests.
    /// Provides a decoupled way for CircuitPoints to signal that roads need rebuilding
    /// without managing event subscriptions. Thread-safe singleton pattern.
    /// </summary>
    public static class RoadRebuildQueue
    {
        private static HashSet<Road> _dirtyRoadIndices = new HashSet<Road>();
        private static object _lock = new object();
        
        /// <summary>
        /// Marks a road as needing rebuild. Safe to call multiple times with the same road.
        /// </summary>
        /// <param name="road">The road to mark as dirty.</param>
        public static void MarkDirty(Road road)
        {
            if (road == null) return;
            
            lock (_lock)
            {
                _dirtyRoadIndices.Add(road);
            }
        }
        
        /// <summary>
        /// Gets all dirty roads and clears the queue atomically.
        /// </summary>
        /// <returns>List of all roads that need rebuilding.</returns>
        public static List<Road> GetAndClearDirtyRoads()
        {
            lock (_lock)
            {
                var result = new List<Road>(_dirtyRoadIndices);
                _dirtyRoadIndices.Clear();
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
                _dirtyRoadIndices.Clear();
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
                return _dirtyRoadIndices.Count > 0;
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
                    return _dirtyRoadIndices.Count;
                }
            }
        }
    }
}
