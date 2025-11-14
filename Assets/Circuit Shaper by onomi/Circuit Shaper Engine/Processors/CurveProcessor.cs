using OnomiCircuitShaper.Engine.Data;
using System.Numerics;
using System;


namespace OnomiCircuitShaper.Engine.Processors
{
    /// <summary>
    /// A static class containing logic for processing and modifying Curve objects.
    /// </summary>
    public static class CurveProcessor
    {
        /// <summary>
        /// Calculates the total length of the curve and assigns a normalized position (0 to 1)
        /// to each point along the curve's length. This pre-calculates distances for fast lookups.
        /// </summary>
        public static void NormaliseCurvePoints<TPointData>(CurveData<TPointData> curve, int approximationQuality = 10) where TPointData : PointData, new()
        {
            if (curve == null || curve.CurvePoints.Count < 2)
            {
                if (curve?.CurvePoints.Count == 1)
                {
                    curve.CurvePoints[0].NormalizedPosition01 = 0;
                }
                return;
            }

            var numPoints = curve.CurvePoints.Count;
            var cumulativeDistances = new float[numPoints];
            cumulativeDistances[0] = 0;
            float totalLength = 0;

            for (int i = 0; i < numPoints - 1 + (curve.IsClosed ? 1 : 0); i++)
            {
                var p1 = curve.CurvePoints[i];
                var p2 = curve.CurvePoints[(i + 1) % numPoints];

                float segmentLength = 0;
                Vector3 lastPoint = p1.PointPosition;

                for (int j = 1; j <= approximationQuality; j++)
                {
                    float t = j / (float)approximationQuality;
                    Vector3 currentPoint = CircuitMathematics.BezierEvaluateCubic(p1.PointPosition, p1.ForwardControlPointPosition, p2.BackwardControlPointPosition, p2.PointPosition, t);
                    segmentLength += Vector3.Distance(lastPoint, currentPoint);
                    lastPoint = currentPoint;
                }

                totalLength += segmentLength;
                cumulativeDistances[(i + 1) % numPoints] = totalLength;
            }

            if (totalLength > 1e-6f)
            {
                for (int i = 0; i < numPoints; i++)
                {
                    curve.CurvePoints[i].NormalizedPosition01 = cumulativeDistances[i] / totalLength;
                }
            }
            else
            {
                // If total length is zero, just set normalized positions to zero.
                for (int i = 0; i < numPoints; i++)
                {
                    curve.CurvePoints[i].NormalizedPosition01 = 0;
                }
            }
        }

        /// <summary>
        /// Gets a point at a normalized distance along the entire curve using the pre-calculated values.
        /// This is very fast as it relies on the data from NormaliseCurvePoints.
        /// </summary>
        public static Vector3 LerpAlongCurve<TPointData>(CurveData<TPointData> curve, float value01) where TPointData : PointData, new()
        {
            if (curve == null || curve.CurvePoints.Count == 0)
            {
                return Vector3.Zero;
            }
            if (curve.CurvePoints.Count == 1)
            {
                return curve.CurvePoints[0].PointPosition;
            }

            value01 = Math.Clamp(value01, 0f, 1f);

            int numPoints = curve.CurvePoints.Count;
            for (int i = 0; i < numPoints - 1 + (curve.IsClosed ? 1 : 0); i++)
            {
                var p1 = curve.CurvePoints[i];
                var p2 = curve.CurvePoints[(i + 1) % numPoints];

                float p1Norm = p1.NormalizedPosition01;
                float p2Norm = p2.NormalizedPosition01;

                // Handle loop for closed curves where p2Norm might be 0 for the last segment
                if (p2Norm < p1Norm)
                {
                    p2Norm = 1f;
                }

                if (value01 >= p1Norm && value01 <= p2Norm)
                {
                    float segmentRange = p2Norm - p1Norm;
                    if (segmentRange < 1e-6f)
                    {
                        return p1.PointPosition;
                    }

                    float t = (value01 - p1Norm) / segmentRange;
                    return CircuitMathematics.BezierEvaluateCubic(p1.PointPosition, p1.ForwardControlPointPosition, p2.BackwardControlPointPosition, p2.PointPosition, t);
                }
            }

            // Fallback to the last point if not found (shouldn't happen with clamped value01)
            return curve.CurvePoints[curve.CurvePoints.Count - 1].PointPosition;
        }


        /// <summary>
        /// Interpolates a point on the surface between two circuit points' cross-sections.
        /// </summary>
        /// <param name="p1">The starting circuit point.</param>
        /// <param name="p2">The ending circuit point.</param>
        /// <param name="x">The normalized distance along the cross-section curves (0 to 1).</param>
        /// <param name="y">The normalized distance along the main curve segment between p1 and p2 (0 to 1).</param>
        /// <returns>The interpolated point in world space.</returns>
        public static Vector3 LerpBetweenTwoCrossSections(CircuitPointData p1, CircuitPointData p2, float x, float y)
        {
            // 1. Find the point along each cross-section curve at normalized distance 'x'.
            Vector3 localCrossPoint1 = LerpAlongCurve(p1.CrossSectionCurve, x);
            Vector3 localCrossPoint2 = LerpAlongCurve(p2.CrossSectionCurve, x);

            // 2. Calculate the world-space offset vectors for the start and end points.
            // The cross-section points are in a local 2D space (across/up).
            Vector3 p1Up = Vector3.Normalize(p1.UpDirection);
            Vector3 p1Forward = Vector3.Normalize(p1.ForwardControlPointPosition - p1.PointPosition);
            Vector3 p1Right = Vector3.Cross(p1Forward, p1Up);
            Vector3 offset1 = p1Right * localCrossPoint1.X + p1Up * localCrossPoint1.Y;

            Vector3 p2Up = Vector3.Normalize(p2.UpDirection);
            Vector3 p2Forward = Vector3.Normalize(p2.PointPosition - p2.BackwardControlPointPosition);
            Vector3 p2Right = Vector3.Cross(p2Forward, p2Up);
            Vector3 offset2 = p2Right * localCrossPoint2.X + p2Up * localCrossPoint2.Y;

            // 3. Create a new, temporary Bézier curve by shifting the original segment by these offsets.
            Vector3 newP0 = (Vector3)p1.PointPosition + offset1;
            Vector3 newP1 = (Vector3)p1.ForwardControlPointPosition + offset1;
            Vector3 newP2 = (Vector3)p2.BackwardControlPointPosition + offset2;
            Vector3 newP3 = (Vector3)p2.PointPosition + offset2;

            // 4. Evaluate this new curve at 'y' to get the final point on the surface.
            return CircuitMathematics.BezierEvaluateCubic(newP0, newP1, newP2, newP3, y);
        }
    }
}
