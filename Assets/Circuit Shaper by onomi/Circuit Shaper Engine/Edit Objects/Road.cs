using OnomiCircuitShaper.Engine.Data;
using System.Collections.Generic;

namespace OnomiCircuitShaper.Engine.EditRealm
{
    /// <summary>
    /// Represents a live, editable road object. Simple wrapper around RoadData.
    /// Roads are defined by PointAddresses and have mesh generation parameters.
    /// The queue system handles dirty tracking, no events needed.
    /// </summary>
    public class Road
    {
        /// <summary>
        /// The raw, underlying data for this road.
        /// Contains PointAddresses, mesh generation parameters, UV settings.
        /// </summary>
        public RoadData Data { get; private set; }

        public CircuitCurve parentCurve { get; private set; }

        /// <summary>
        /// A reference to the editor-wide settings.
        /// </summary>
        public CircuitAndEditorSettings Settings { get; private set; }


        /// <summary>
        /// A list of live Railing objects for this road.
        /// </summary>
        public List<Railing> Railings { get; private set; } = new List<Railing>();

        /// <summary>
        /// The live Bridge object for this road, if one exists.
        /// </summary>
        public Bridge Bridge { get; private set; }


        public int RoadStartIndex => Data.PointIndexRange.Item1;

        /// <summary>
        /// Constructor - creates a simple wrapper around RoadData.
        /// </summary>
        public Road(RoadData data, CircuitAndEditorSettings settings, CircuitCurve parentCurve)
        {
            Data = data;
            Settings = settings;

            // Create live wrappers for railings
            if (data.Railings != null)
            {
                foreach (var railingData in data.Railings)
                {
                    Railings.Add(new Railing(railingData));
                }
            }

            // Create live wrapper for bridge if it exists
            if (data.Bridge != null)
            {
                Bridge = new Bridge(data.Bridge);
            }
        }
    }
}
