using OnomiCircuitShaper.Engine.Data;
using System.Numerics;

namespace OnomiCircuitShaper.Engine.EditRealm
{
    /// <summary>
    /// Represents a live, editable point on a cross-section curve. Its position is stored
    /// relative to its parent CircuitPoint (X = across, Y = up, Z = 0).
    /// </summary>
    public class CrossSectionPoint : Point<CrossSectionPointData>
    {
        /// <summary>
        /// The parent circuit point that this cross-section point is attached to.
        /// Must be set by the owning curve when creating the live edit objects.
        /// </summary>
        public CircuitPoint ParentCircuitPoint { get; set; }


        //parent cross-section curve can be added if needed
        public CrossSectionCurve ParentCrossSectionCurve { get; set; }

        //Constructor
        public CrossSectionPoint(CrossSectionPointData data, CircuitAndEditorSettings settings, CircuitPoint parentCircuitPoint, CrossSectionCurve parentCrossSectionCurve) : base(data, settings)
        {
            ParentCircuitPoint = parentCircuitPoint;
            ParentCrossSectionCurve = parentCrossSectionCurve;
        }

        /// <summary>
        /// Moves the cross-section point. The input position is converted from world-space
        /// to the local 2D coordinate system of the parent circuit point (across/up),
        /// then stored with z=0 as per CrossSectionPointData conventions.
        /// </summary>
        public void MoveCrossSectionPoint(Vector3 newPosition)
        {
            if (ParentCircuitPoint == null)
            {
                // If no parent is available, fallback to storing the raw position.
                Data.PointPosition = new Vector3(newPosition.X, newPosition.Y, newPosition.Z);
                OnPointStateChanged();
                return;
            }

            // Compute delta relative to parent world position
            var delta = newPosition - ParentCircuitPoint.PointPosition;

            // Project onto parent's across and up vectors to get local 2D coords
            var across = ParentCircuitPoint.GetRightVector;
            var up = ParentCircuitPoint.GetUpVector;

            float localX = Vector3.Dot(delta, across);
            float localY = Vector3.Dot(delta, up);

            // Cross-section data uses z == 0 and x/y represent across/up offsets
            // If the backing data is a CrossSectionPointData (derived), write to it;
            // otherwise write into the generic PointData position with z=0.
            if (Data is CrossSectionPointData csd)
            {
                csd.PointPosition = new Vector3(localX, localY, 0f);
            }
            else
            {
                Data.PointPosition = new Vector3(localX, localY, 0f);
            }

            // Notify listeners/owners that the cross-section point changed. Owners
            // such as the CrossSectionCurve should listen and auto-set control points.
            OnPointStateChanged();
            
        }

        /// <summary>
        /// Gets the world-space position of this cross-section point, based on its
        /// local across/up coordinates relative to its parent circuit point.
        /// </summary>
        public Vector3 GetWorldPosition()
        {
            if (ParentCircuitPoint == null)
            {
                return Data.PointPosition;
            }

            var across = ParentCircuitPoint.GetRightVector;
            var up = ParentCircuitPoint.GetUpVector;
            var basePosition = ParentCircuitPoint.PointPosition;

            return basePosition + across * Data.PointPosition.x + up * Data.PointPosition.y;
        }

        /// <summary>
        /// Gets the world-space position of this cross-section point's forward control point,
        /// based on its local across/up coordinates relative to its parent circuit point.
        /// </summary>
        public Vector3 GetWorldForwardControlPointPosition()
        {
            if (ParentCircuitPoint == null)
            {
                return Data.ForwardControlPointPosition;
            }

            var across = ParentCircuitPoint.GetRightVector;
            var up = ParentCircuitPoint.GetUpVector;
            var basePosition = ParentCircuitPoint.PointPosition;

            return basePosition + across * Data.ForwardControlPointPosition.x + up * Data.ForwardControlPointPosition.y;
        }

        /// <summary>
        /// Gets the world-space position of this cross-section point's backward control point,
        /// based on its local across/up coordinates relative to its parent circuit point.
        /// </summary>
        public Vector3 GetWorldBackwardControlPointPosition()
        {
            if (ParentCircuitPoint == null)
            {
                return Data.BackwardControlPointPosition;
            }

            var across = ParentCircuitPoint.GetRightVector;
            var up = ParentCircuitPoint.GetUpVector;
            var basePosition = ParentCircuitPoint.PointPosition;

            return basePosition + across * Data.BackwardControlPointPosition.x + up * Data.BackwardControlPointPosition.y;
        }
    }
}