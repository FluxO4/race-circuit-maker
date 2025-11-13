using OnomiCircuitShaper.Engine.Data;
using System;
using System.Collections.Generic;

namespace OnomiCircuitShaper.Engine.EditRealm
{
    /// <summary>
    /// The base class for a live, editable curve. It wraps CurveData and manages a collection
    /// of live Point objects. It provides methods for modifying the curve's structure.
    /// </summary>
    public abstract class Curve<TData, TPointData, TPoint>
        where TData : CurveData<TPointData>
        where TPointData : PointData
        where TPoint : Point
    {
        /// <summary>
        /// A reference to the editor-wide settings.
        /// </summary>
        public CircuitAndEditorSettings Settings { get; private set; }

        /// <summary>
        /// A dictionary mapping the raw PointData to the live, editable Point objects.
        /// Strongly-typed so derived curves cannot accidentally mix point types.
        /// </summary>
        public Dictionary<TPointData, TPoint> Points { get; private set; } = new Dictionary<TPointData, TPoint>();

        /// <summary>
        /// An event that is fired whenever the curve's structure or properties change.
        /// No arguments to keep subscribers simple and avoid coupling to the generic type.
        /// </summary>
        public event Action CurveStateChanged;

        /// <summary>
        /// Iterates through all points in the curve and instructs them to auto-set their control points.
        /// </summary>
        public virtual void AutoSetAllControlPoints()
        {
            // Implementation may be provided by processors or overridden by derived classes.
        }

        /// <summary>
        /// Fires the CurveStateChanged event.
        /// </summary>
        protected void OnCurveStateChanged()
        {
            CurveStateChanged?.Invoke();
        }

        //Constructor
        public Curve(CircuitAndEditorSettings settings)
        {
            Settings = settings;
        }
    }
}
