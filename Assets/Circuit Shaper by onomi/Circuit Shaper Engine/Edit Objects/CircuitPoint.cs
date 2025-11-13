using System;
using OnomiCircuitShaper.Engine.Data;
using System.Numerics;

namespace OnomiCircuitShaper.Engine.EditRealm
{
    /// <summary>
    /// Represents a live, editable point on the main circuit spline. It extends the base Point
    /// with functionality and properties specific to a point that defines the track's path,
    /// such as having its own cross-section.
    /// </summary>
    public class CircuitPoint : Point
    {
        /// <summary>
        /// The live, editable cross-section curve associated with this circuit point.
        /// </summary>
        public CrossSectionCurve CrossSection { get; private set; }

        /// <summary>
        /// Helper that attempts to read an IndependentControlPoints boolean from the
        /// settings object via reflection. If the property is missing we assume
        /// independent control points are enabled (no mirroring).
        /// </summary>
        private bool IndependentControlPointsEnabled()
        {
            if (Settings == null) return true;
            var prop = Settings.GetType().GetProperty("IndependentControlPoints");
            if (prop != null && prop.PropertyType == typeof(bool))
            {
                var value = prop.GetValue(Settings);
                if (value is bool b) return b;
            }
            return true;
        }

        //Constructor
        public CircuitPoint(CircuitPointData data, CircuitAndEditorSettings settings, CrossSectionCurve crossSection) : base(data, settings)
        {
            CrossSection = crossSection;
        }

        #region Properties

        /// <summary>
        /// Gets the world-space position of the leftmost point of the cross-section curve.
        /// </summary>
        public Vector3 GetLeftEndPointPosition
        {
            get
            {
                // If no useful cross-section is present, fall back to the circuit point position.
                if (CrossSection == null || CrossSection.Data == null || CrossSection.Data.CurvePoints == null || CrossSection.Data.CurvePoints.Count == 0)
                    return Data.PointPosition;

                // Cross-section points are stored in local across/up coords relative to this circuit point.
                var first = CrossSection.Data.CurvePoints[0].PointPosition; // x = across, y = up
                return (Vector3)Data.PointPosition + first.x * GetRightVector + first.y * GetUpVector;
            }
        }

        /// <summary>
        /// Gets the world-space position of the rightmost point of the cross-section curve.
        /// </summary>
        public Vector3 GetRightEndPointPosition
        {
            get
            {
                if (CrossSection == null || CrossSection.Data == null || CrossSection.Data.CurvePoints == null || CrossSection.Data.CurvePoints.Count == 0)
                    return Data.PointPosition;

                var pts = CrossSection.Data.CurvePoints;
                var last = pts[pts.Count - 1].PointPosition;
                return (Vector3)Data.PointPosition + last.x * GetRightVector + last.y * GetUpVector;
            }
        }

        #endregion

        /// <summary>
        /// Updates the position of this circuit point and its control points.
        /// </summary>
        /// <param name="newPosition">The target world-space position.</param>
        public void MoveCircuitPoint(Vector3 newPosition)
        {
            SerializableVector3 delta = (SerializableVector3)newPosition - Data.PointPosition;
            Data.PointPosition = newPosition;
            Data.ForwardControlPointPosition += delta;
            Data.BackwardControlPointPosition += delta;
            OnPointStateChanged();
        }

        /// <summary>
        /// Updates the position of the forward control point.
        /// If control points are not independent, it will also adjust the backward control point.
        /// </summary>
        public void MoveForwardControlPoint(Vector3 newPosition)
        {
            Data.ForwardControlPointPosition = newPosition;
            if (!IndependentControlPointsEnabled())
            {
                SerializableVector3 dir = Vector3.Normalize(Data.PointPosition - (SerializableVector3)newPosition);
                float dist = Vector3.Distance(Data.PointPosition, Data.BackwardControlPointPosition);
                Data.BackwardControlPointPosition = Data.PointPosition + dir * dist;
            }
            OnPointStateChanged();
        }

        /// <summary>
        /// Updates the position of the backward control point.
        /// If control points are not independent, it will also adjust the forward control point.
        /// </summary>
        public void MoveBackwardControlPoint(Vector3 newPosition)
        {
            Data.BackwardControlPointPosition = newPosition;
            if (!IndependentControlPointsEnabled())
            {
                SerializableVector3 dir = Vector3.Normalize(Data.PointPosition - (SerializableVector3)newPosition);
                float dist = Vector3.Distance(Data.PointPosition, Data.ForwardControlPointPosition);
                Data.ForwardControlPointPosition = Data.PointPosition + dir * dist;
            }
            OnPointStateChanged();
        }

        /// <summary>
        /// Rotates the point's orientation (its up and forward vectors) by a given delta.
        /// </summary>
        /// <summary>
        /// Rotates the point's orientation (its up and forward vectors) by a given global Euler delta (degrees).
        /// This updates the stored up direction and rotates the control points around the point position.
        /// </summary>
        public void RotateCircuitPoint(Vector3 globalEulerDelta)
        {
            // Convert degrees to radians and create a quaternion. The mapping uses
            // (pitch=x, yaw=y, roll=z) for CreateFromYawPitchRoll which takes (yaw, pitch, roll).
            // Use System.Math fallback for environments that don't expose MathF
            float degToRad = (float)(Math.PI / 180.0);
            float pitch = globalEulerDelta.X * degToRad;
            float yaw = globalEulerDelta.Y * degToRad;
            float roll = globalEulerDelta.Z * degToRad;

            var rotation = Quaternion.CreateFromYawPitchRoll(yaw, pitch, roll);

            Data.UpDirection = Vector3.Transform(Data.UpDirection, rotation);

            var forwardControlRelative = Data.ForwardControlPointPosition - Data.PointPosition;
            var backwardControlRelative = Data.BackwardControlPointPosition - Data.PointPosition;

            Data.ForwardControlPointPosition = Data.PointPosition + (SerializableVector3)Vector3.Transform(forwardControlRelative, rotation);
            Data.BackwardControlPointPosition = Data.PointPosition + (SerializableVector3)Vector3.Transform(backwardControlRelative, rotation);

            OnPointStateChanged();
        }
    }
}
