using OnomiCircuitShaper.Engine.Data;
using System.Collections.Generic;

namespace OnomiCircuitShaper.Engine.EditRealm
{
    /// <summary>
    /// Represents a live, editable road object. It wraps RoadData and manages the process
    /// of triggering mesh generation when its underlying points change.
    /// Roads are built along sequences of circuit points, with their geometry defined by
    /// the cross-sections at each point being lofted/interpolated together.
    /// </summary>
    /// <remarks>
    /// [Look here onomi] Roads listen to PointStateChanged events from their associated points
    /// and automatically trigger RoadRebuilt when changes occur. This event-driven architecture
    /// ensures the visual mesh stays synchronized with the data.
    /// The mesh generation uses WidthWiseVertexCount and LengthWiseVertexCountPerUnitWidthWiseVertexCount
    /// to determine tessellation density.
    /// </remarks>
    public class Road
    {
        /// <summary>
        /// Indicates whether this road needs to be rebuilt due to point changes.
        /// </summary>
        public bool IsDirty { get; private set; }

        /// <summary>
        /// Marks the road as needing a rebuild.
        /// </summary>
        public void MarkDirty()
        {
            IsDirty = true;
        }

        /// <summary>
        /// Clears the dirty flag after the road has been rebuilt.
        /// </summary>
        public void ClearDirty()
        {
            IsDirty = false;
        }

        /// <summary>
        /// A reference to the editor-wide settings.
        /// </summary>
        public CircuitAndEditorSettings Settings { get; private set; }

        /// <summary>
        /// The raw, underlying data for this road.
        /// Contains mesh generation parameters, UV settings, and references to associated point data.
        /// </summary>
        public RoadData Data { get; private set; }


        /// <summary>
        /// A list of live Point objects associated with this road. The order should match the underlying data list.
        /// These are the actual live edit-realm objects whose PointStateChanged events this road subscribes to.
        /// </summary>
        /// <remarks>
        /// [Look here onomi] This list must be populated by finding the live wrappers that correspond
        /// to the PointData objects in Data.AssociatedPoints. The Circuit class should provide
        /// helper methods to resolve these references during BeginEditFromData.
        /// </remarks>
        public List<CircuitPoint> AssociatedPoints { get; private set; } = new List<CircuitPoint>();

        /// <summary>
        /// A list of live Railing objects for this road. The order should match the underlying data list.
        /// </summary>
        public List<Railing> Railings { get; private set; } = new List<Railing>();

        /// <summary>
        /// The live Bridge object for this road, if one exists.
        /// Can be null if the road has no bridge.
        /// </summary>
        public Bridge Bridge { get; private set; }

        /// <summary>
        /// An event that is fired after the road's mesh has been rebuilt.
        /// The road itself is passed as an argument.
        /// Unity layer subscribes to this to update visual meshes via RoadProcessor.BuildRoadMesh.
        /// </summary>
        public event System.Action<Road> RoadRebuilt;

        /// <summary>
        /// Initializes a new road from a list of points.
        /// </summary>
        /// <remarks>
        /// [Look here onomi] This should populate Data.AssociatedPoints with the PointData from the provided points,
        /// then call a method to establish the live references and event subscriptions.
        /// </remarks>
        public void BuildRoadFromPoints(List<CircuitPoint> points)
        {
            if (points == null || points.Count < 2)
            {
                return; // Need at least 2 points to build a road
            }

            // Populate the data with point references
            Data.AssociatedPoints.Clear();
            foreach (var point in points)
            {
                Data.AssociatedPoints.Add(point.Data);
            }

            // Establish live references and event subscriptions
            PopulateAssociatedPointsAndSubscribe(points);

            // Trigger initial rebuild
            OnRoadRebuilt();
        }

        /// <summary>
        /// Populates AssociatedPoints with live point references and subscribes to their change events.
        /// </summary>
        private void PopulateAssociatedPointsAndSubscribe(List<CircuitPoint> points)
        {
            // Unsubscribe from any existing points
            UnsubscribeFromAllPoints();

            // Clear and repopulate
            AssociatedPoints.Clear();
            foreach (CircuitPoint point in points)
            {
                AssociatedPoints.Add(point);
                point.PointStateChanged += OnPointStateChanged;
            }
        }

        /// <summary>
        /// Unsubscribes from all point change events.
        /// </summary>
        private void UnsubscribeFromAllPoints()
        {
            foreach (var point in AssociatedPoints)
            {
                if (point != null)
                {
                    point.PointStateChanged -= OnPointStateChanged;
                }
            }
        }

        /// <summary>
        /// Called when any associated point changes state.
        /// </summary>
        private void OnPointStateChanged(Point<CircuitPointData> point)
        {
            MarkDirty();
        }

        /// <summary>
        /// Triggers the RoadRebuilt event to signal that the mesh needs regeneration.
        /// </summary>
        private void OnRoadRebuilt()
        {
            RoadRebuilt?.Invoke(this);
        }

        //Constructor
        public Road(RoadData data, CircuitAndEditorSettings settings, Circuit parentCircuit)
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

            // Note: AssociatedPoints must be populated later via BuildRoadFromPoints or manual setup
            // because we need the live Circuit objects to resolve PointData -> Point references
        }
    }
}
