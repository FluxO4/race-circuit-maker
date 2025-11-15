using OnomiCircuitShaper.Engine.Data;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;
using OnomiCircuitShaper.Engine.Processors;
using OnomiCircuitShaper.Engine.Presets;

namespace OnomiCircuitShaper.Engine.EditRealm
{
    /// <summary>
    /// Represents a live, editable cross-section curve. These are always open.
    /// </summary>
    public class CrossSectionCurve
    {

        /// <summary>
        /// Changes the number of points in the cross-section while attempting to preserve its shape
        /// by interpolating new point positions.
        /// </summary>

        public CrossSectionCurveData Data { get; private set; }
        public CircuitAndEditorSettings Settings { get; private set; }
        public List<CrossSectionPoint> Points { get; private set; } = new List<CrossSectionPoint>();

        public CircuitPoint parentCircuitPoint;

        public event System.Action CurveStateChanged;

        public void ChangeCrossSectionPointCount(int newCount)
        {
            if (newCount < 2) return;
            if (Points.Count == newCount) return;


            if (Points.Count < 2)
            {
                // Handle initial creation: use the FlatPreset as the base for new points.
                SetPreset(CrossSectionPresets.FlatPreset);
                return;
            }
            else
            {
                var newPositions = new List<Vector3>();
                // Resample the existing curve shape to the new point count.
                for (int i = 0; i < newCount; i++)
                {
                    float t = (newCount > 1) ? i / (float)(newCount - 1) : 0;
                    Vector3 newPos = CurveProcessor.LerpAlongCurve(Data, t);
                    newPositions.Add(newPos);
                }
                SetPointsFromLocalPositions(newPositions);
            }

            parentCircuitPoint.OnCrossSectionChanged();
            
        }

        /// <summary>
        /// Rebuilds the cross-section from a new list of local-space points.
        /// This can be used for presets or procedural modifications.
        /// </summary>
        public void SetPointsFromLocalPositions(List<Vector3> localPositions)
        {
            // Cross-section curves are ALWAYS open
            Data.IsClosed = false;
            
            // Unsubscribe from old points to prevent memory leaks
            foreach (var point in Points)
            {
                point.PointStateChanged -= OnPointChanged;
            }

            Points.Clear();
            Data.CurvePoints.Clear();

            // Create new points from the provided positions
            foreach (var pos in localPositions)
            {
                var pointData = new CrossSectionPointData { PointPosition = pos };
                var crossSectionPoint = new CrossSectionPoint(pointData, Settings, parentCircuitPoint, this);
                crossSectionPoint.PointStateChanged += OnPointChanged; // Subscribe to changes
                Points.Add(crossSectionPoint);
                Data.CurvePoints.Add(pointData);
            }

            UpdateNeighborReferences();
            HandleCrossSectionPointChanged();
            OnCurveStateChanged();
            
        }

        /// <summary>
        /// Applies a predefined cross-section preset, replacing all existing points.
        /// </summary>
        /// <param name="preset">An array of local-space Vector3 positions defining the preset shape.</param>
        public void SetPreset(Vector3[] preset)
        {
            if (preset == null || preset.Length < 2)
            {
                // Handle invalid preset: default to flat.
                SetPointsFromLocalPositions(CrossSectionPresets.FlatPreset.ToList());
                return;
            }
            SetPointsFromLocalPositions(preset.ToList());

    
        }

        /// <summary>
        /// A handler that listens for changes in its child points and automatically
        /// recalculates all control points to maintain a smooth shape.
        /// </summary>

        public void HandleCrossSectionPointChanged()
        {
            AutoSetAllControlPoints();
            NormaliseCrossSectionPoints();
            parentCircuitPoint.OnCrossSectionChanged();
        }

        private void OnPointChanged(Point<CrossSectionPointData> point)
        {
            HandleCrossSectionPointChanged();
        }

        /// <summary>
        /// Constructor using raw data and settings.
        /// </summary>
        public CrossSectionCurve(CrossSectionCurveData data, CircuitAndEditorSettings settings, CircuitPoint parentCircuitPoint)
        {
            Data = data;
            Settings = settings;
            this.parentCircuitPoint = parentCircuitPoint;
            
            // Cross-section curves are ALWAYS open (never closed loops)
            Data.IsClosed = false;

            // Create live CrossSectionPoint objects for each data point
            for (int i = 0; i < Data.CurvePoints.Count; i++)
            {
                CrossSectionPointData pointData = Data.CurvePoints[i];
                CrossSectionPoint crossSectionPoint = new CrossSectionPoint(pointData, Settings, parentCircuitPoint, this);
                crossSectionPoint.PointStateChanged += OnPointChanged;
                Points.Add(crossSectionPoint);
            }

            UpdateNeighborReferences();
            //Normalise the point positions along the curve
            NormaliseCrossSectionPoints();
        }

        //Update point normalasied positions along curve
        public void NormaliseCrossSectionPoints()
        {
            // Implementation may be provided by processors or overridden by derived classes.
            CurveProcessor.NormaliseCurvePoints(Data);
        }

        private void UpdateNeighborReferences()
        {
            for (int i = 0; i < Points.Count; i++)
            {
                Points[i].NextPoint = (i < Points.Count - 1) ? Points[i+1] : null;
                Points[i].PreviousPoint = (i > 0) ? Points[i - 1] : null;
            }
        }



        public void AutoSetAllControlPoints()
        {
            // Call autoset function for each point in the cross-section, but do the end points last
            for (int i = 1; i < Points.Count - 1; i++)
            {
                Points[i].AutoSetControlPoints();
            }
            if (Points.Count > 0)
            {
                Points[0].AutoSetControlPoints();
            }
            if (Points.Count > 1)
            {
                Points[^1].AutoSetControlPoints();
            }
            OnCurveStateChanged(); // <--- ADDED THIS LINE
        }

        protected void OnCurveStateChanged()
        {
            CurveStateChanged?.Invoke();
        }
    }
}
