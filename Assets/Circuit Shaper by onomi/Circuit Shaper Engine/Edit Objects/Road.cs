using OnomiCircuitShaper.Engine.Data;
using System.Collections.Generic;

namespace OnomiCircuitShaper.Engine.EditRealm
{
    /// <summary>
    /// Represents a live, editable road object. It wraps RoadData and manages the process
    /// of triggering mesh generation when its underlying points change.
    /// </summary>
    public class Road
    {
        /// <summary>
        /// A reference to the editor-wide settings.
        /// </summary>
        public CircuitAndEditorSettings Settings { get; private set; }

        /// <summary>
        /// The raw, underlying data for this road.
        /// </summary>
        public RoadData Data { get; private set; }

        /// <summary>
        /// A dictionary mapping the raw PointData to the live Point objects associated with this road.
        /// </summary>
        public Dictionary<PointData, Point> AssociatedPoints { get; private set; } = new Dictionary<PointData, Point>();

        /// <summary>
        /// A dictionary mapping the raw RailingData to the live Railing objects for this road.
        /// </summary>
        public Dictionary<RailingData, Railing> Railings { get; private set; } = new Dictionary<RailingData, Railing>();

        /// <summary>
        /// The live Bridge object for this road.
        /// </summary>
        public Bridge Bridge { get; private set; }

        /// <summary>
        /// An event that is fired after the road's mesh has been rebuilt.
        /// The road itself is passed as an argument.
        /// </summary>
        public event System.Action<Road> RoadRebuilt;

        /// <summary>
        /// Initializes a new road from a list of points.
        /// </summary>
        public void BuildRoadFromPoints(List<Point> points)
        {
            // To be implemented.
        }
    }
}
