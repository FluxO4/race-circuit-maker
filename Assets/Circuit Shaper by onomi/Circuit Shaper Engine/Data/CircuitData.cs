using System.Collections.Generic;
using System.Numerics;

namespace OnomiCircuitShaper.Engine.Data
{
    /// <summary>
    /// Contains all the raw, serializable data for a single race circuit.
    /// This class is the root of the data model and is completely engine-agnostic.
    /// It holds the collections of curves and roads that together define the entire track layout.
    /// </summary>
    [System.Serializable]
    public class CircuitData
    {
        /// <summary>
        /// A list of all the CurveData objects that define the splines of the circuit.
        /// These curves form the backbone of the track.
        /// </summary>
        public List<CurveData> CircuitCurves = new List<CurveData>();

        /// <summary>
        /// A list of all the RoadData objects. Each road defines a visible mesh
        /// that is generated along a sequence of points from the curves.
        /// </summary>
        public List<RoadData> CircuitRoads = new List<RoadData>();
    }
}
