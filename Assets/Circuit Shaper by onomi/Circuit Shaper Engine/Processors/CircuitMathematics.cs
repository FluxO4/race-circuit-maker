using System.Numerics;

namespace OnomiCircuitShaper.Engine.Processors
{
    /// <summary>
    /// A static class containing pure, engine-agnostic mathematical functions
    /// for Bézier curve calculations.
    /// </summary>
    public static class CircuitMathematics
    {
        /// <summary>
        /// Evaluates a point on a quadratic Bézier curve defined by three points.
        /// </summary>
        public static Vector3 BezierEvaluateQuadratic(Vector3 a, Vector3 b, Vector3 c, float t)
        {
            Vector3 p0 = Vector3.Lerp(a, b, t);
            Vector3 p1 = Vector3.Lerp(b, c, t);
            return Vector3.Lerp(p0, p1, t);
        }

        /// <summary>
        /// Evaluates a point on a cubic Bézier curve defined by four points.
        /// </summary>
        public static Vector3 BezierEvaluateCubic(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float t)
        {
            Vector3 p0 = BezierEvaluateQuadratic(a, b, c, t);
            Vector3 p1 = BezierEvaluateQuadratic(b, c, d, t);
            return Vector3.Lerp(p0, p1, t);
        }

        /// <summary>
        /// Estimates the total arc-length of a cubic Bézier curve by subdividing it into straight line segments.
        /// </summary>
        public static float EstimateCurveLength(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, int subdivisions = 10)
        {
            float length = 0.0f;
            Vector3 previousPoint = p0;

            for (int i = 1; i <= subdivisions; i++)
            {
                float t = (float)i / subdivisions;
                Vector3 currentPoint = BezierEvaluateCubic(p0, p1, p2, p3, t);
                length += Vector3.Distance(previousPoint, currentPoint);
                previousPoint = currentPoint;
            }

            return length;
        }
    }
}
