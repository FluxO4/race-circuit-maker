using System.Collections.Generic;
using System.Numerics;
using OnomiCircuitShaper.Engine.Data;

namespace OnomiCircuitShaper.Engine.Edit
{
    /// <summary>
    /// Represents a single editable point in 3D space within the circuit shaper engine.
    /// Points are used to define the geometry of curves and roads.
    /// </summary>
    [System.Serializable]
    public class Point
    {
        /// <summary>
        /// The Data
        /// </summary>
        public PointData Data = new PointData();

        
    }
}