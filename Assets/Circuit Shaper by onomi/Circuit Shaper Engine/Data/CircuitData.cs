using System.Collections.Generic;
using System.Numerics;

namespace OnomiCircuitShaper.Engine.Data
{
    /// <summary>
    /// Contains all the raw, serializable data for a single race circuit.
    /// This class is the root of the geometric data model and is completely engine-agnostic.
    /// It holds the collections of curves (the track's path) and roads (the visible meshes)
    /// that together define the entire track layout.
    /// </summary>
    [System.Serializable]
    public class CircuitData
    {
        /// <summary>
        /// A list of all the <see cref="CircuitCurveData"/> objects that define the splines of the circuit.
        /// These curves form the central backbone of the track's layout.
        /// </summary>
        // Main circuit curves — use the specialized CircuitCurveData type so higher
        // layers can clearly distinguish main-path splines from other types like cross-sections.
        public List<CircuitCurveData> CircuitCurves = new List<CircuitCurveData>();

        /// <summary>
        /// A list of all the <see cref="RoadData"/> objects. Each road defines a visible mesh
        /// that is generated along a sequence of points from the curves, effectively creating
        /// the track surface.
        /// </summary>
        public List<RoadData> CircuitRoads = new List<RoadData>();
    }
}
