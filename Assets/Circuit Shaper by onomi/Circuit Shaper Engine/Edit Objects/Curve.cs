using OnomiCircuitShaper.Engine.Data;
using System;
using System.Collections.Generic;

namespace OnomiCircuitShaper.Engine.EditRealm
{
    /// <summary>
    /// The base class for a live, editable curve. It wraps CurveData and manages a collection
    /// of live Point objects. It provides methods for modifying the curve's structure.
    /// </summary>
    public class Curve
    {
        /// <summary>
        /// A reference to the editor-wide settings.
        /// </summary>
        public CircuitAndEditorSettings Settings { get; private set; }

        /// <summary>
        /// The raw, underlying data for this curve.
        /// </summary>
        public CurveData Data { get; private set; }

        /// <summary>
        /// A dictionary mapping the raw PointData to the live, editable Point objects.
        /// </summary>
        public Dictionary<PointData, Point> Points { get; private set; } = new Dictionary<PointData, Point>();

        /// <summary>
        /// An event that is fired whenever the curve's structure or properties change.
        /// </summary>
        public event Action<Curve> CurveStateChanged;

        /// <summary>
        /// Iterates through all points in the curve and instructs them to auto-set their control points.
        /// </summary>
        public void AutoSetAllControlPoints()
        {
            // Implementation will be in the CurveProcessor.
        }

        /// <summary>
        /// Fires the CurveStateChanged event.
        /// </summary>
        protected void OnCurveStateChanged()
        {
            CurveStateChanged?.Invoke(this);
        }
    }
}
