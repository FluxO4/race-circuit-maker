using OnomiCircuitShaper.Engine.Data;
using System.Numerics;

namespace OnomiCircuitShaper.Engine.EditRealm
{
    /// <summary>
    /// Represents a live, editable cross-section curve. These are always open.
    /// </summary>
    public class CrossSectionCurve : Curve<CrossSectionCurveData, CrossSectionPointData, CrossSectionPoint>
    {
        public CrossSectionCurveData Data;


        /// <summary>
        /// Changes the number of points in the cross-section while attempting to preserve its shape
        /// by interpolating new point positions.
        /// </summary>
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
        public CrossSectionCurve(CrossSectionCurveData data, CircuitAndEditorSettings settings) : base(settings)
        {
            Data = data;
        }
    }
}
