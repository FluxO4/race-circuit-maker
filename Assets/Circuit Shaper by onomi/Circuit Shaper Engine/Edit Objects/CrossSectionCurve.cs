using OnomiCircuitShaper.Engine.Data;
using System.Numerics;
using System.Collections.Generic;

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

        public event System.Action CurveStateChanged;

        public void ChangeCrossSectionPointCount(int newCount)
        {
            // To be implemented.
            OnCurveStateChanged();
        }

        /// <summary>
        /// A handler that listens for changes in its child points and automatically
        /// recalculates all control points to maintain a smooth shape.
        /// </summary>

        public void HandleCrossSectionPointChanged()
        {
            AutoSetAllControlPoints();
        }

        /// <summary>
        /// Constructor using raw data and settings.
        /// </summary>
        public CrossSectionCurve(CrossSectionCurveData data, CircuitAndEditorSettings settings)
        {
            Data = data;
            Settings = settings;
        }

        public virtual void AutoSetAllControlPoints()
        {
            // Implementation may be provided by processors or overridden by derived classes.
        }

        protected void OnCurveStateChanged()
        {
            CurveStateChanged?.Invoke();
        }
    }
}
