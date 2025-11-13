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
        where TPoint : Point<TPointData>
    {

        //The data
        public TData Data { get; private set; }

        /// <summary>
        /// A reference to the editor-wide settings.
        /// </summary>
        public CircuitAndEditorSettings Settings { get; private set; }

        /// <summary>
        /// A list of live, editable point wrappers that mirrors the data list on the
        /// corresponding CurveData. The index of a point in this list corresponds to
        /// the index of its backing PointData in the data model.
        /// </summary>
        public List<TPoint> Points { get; private set; } = new List<TPoint>();

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
        public Curve( TData data, CircuitAndEditorSettings settings)
        {
            Data = data;
            Settings = settings;
        }
    }
}
