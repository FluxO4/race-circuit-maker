using OnomiCircuitShaper.Engine.Data;
using System;
using System.Numerics;
using System.Collections.Generic;

namespace OnomiCircuitShaper.Engine.Processors
{
    /// <summary>
    /// Represents a single waypoint with its transform data.
    /// </summary>
    public struct WaypointData
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Scale; // X = width, Y = height, Z = depth
    }

    /// <summary>
    /// Processes circuit curves to generate waypoint approximations.
    /// Waypoints use adaptive sampling: more points on curves, fewer on straights.
    /// </summary>
    public static class WaypointProcessor
    {
        /// <summary>
        /// Generates waypoints for a circuit curve with adaptive sampling based on curvature.
        /// Returns an array of waypoint transform data.
        /// </summary>
        /// <param name="points">Array of circuit points defining the curve</param>
        /// <param name="settings">Waypoint generation settings</param>
        /// <param name="isClosed">Whether the curve forms a closed loop</param>
        /// <param name="roadWidth">Base width of the road at each point (from cross-section)</param>
        public static WaypointData[] GenerateWaypoints(
            CircuitPointData[] points,
            WaypointSettings settings,
            bool isClosed,
            Func<int, float> getRoadWidthAtPointIndex)
        {
            if (points == null || points.Length < 2)
            {
                return new WaypointData[0];
            }

            var waypoints = new List<WaypointData>();
            int segmentCount = isClosed ? points.Length : points.Length - 1;

            for (int segmentIndex = 0; segmentIndex < segmentCount; segmentIndex++)
            {
                var p1 = points[segmentIndex];
                var p2 = points[(segmentIndex + 1) % points.Length];
                
                // Sample this bezier segment adaptively
                var segmentWaypoints = SampleBezierSegmentAdaptive(
                    p1, p2, settings, segmentIndex, getRoadWidthAtPointIndex);
                
                // Don't duplicate the start point if continuing from previous segment
                int startIdx = (segmentIndex == 0) ? 0 : 1;
                for (int i = startIdx; i < segmentWaypoints.Count; i++)
                {
                    waypoints.Add(segmentWaypoints[i]);
                }
            }

            return waypoints.ToArray();
        }

        /// <summary>
        /// Samples a single bezier segment with adaptive point distribution based on curvature.
        /// </summary>
        private static List<WaypointData> SampleBezierSegmentAdaptive(
            CircuitPointData p1,
            CircuitPointData p2,
            WaypointSettings settings,
            int segmentIndex,
            Func<int, float> getRoadWidthAtPointIndex)
        {
            var waypoints = new List<WaypointData>();
            
            // Estimate segment length
            float segmentLength = CircuitMathematics.EstimateCurveLength(
                p1.PointPosition, p1.ForwardControlPointPosition,
                p2.BackwardControlPointPosition, p2.PointPosition, 20);

            // Calculate how many samples we need based on quality and length
            // Quality 1-100 maps to 0.1-10 samples per unit length
            float samplesPerUnit = 0.1f + (settings.ApproximationQuality / 100f) * 9.9f;
            int initialSamples = Math.Max(2, (int)(segmentLength * samplesPerUnit));
            
            // Create initial uniform samples
            var samples = new List<CurveSample>();
            for (int i = 0; i <= initialSamples; i++)
            {
                float t = (float)i / initialSamples;
                samples.Add(EvaluateCurveSample(p1, p2, t));
            }

            // Refine samples based on curvature - add more points where curvature is high
            samples = RefineSamplesByCurvature(p1, p2, samples, settings);

            // Filter by minimum/maximum spacing
            samples = FilterBySpacing(samples, settings);

            // Convert samples to waypoint data with transforms
            float roadWidth = getRoadWidthAtPointIndex(segmentIndex);
            for (int i = 0; i < samples.Count; i++)
            {
                waypoints.Add(CreateWaypointFromSample(samples[i], roadWidth, settings));
            }

            return waypoints;
        }

        /// <summary>
        /// Evaluates a curve at parameter t and returns position, tangent, normal, and curvature.
        /// </summary>
        private static CurveSample EvaluateCurveSample(CircuitPointData p1, CircuitPointData p2, float t)
        {
            var sample = new CurveSample();
            
            sample.T = t;
            sample.Position = CircuitMathematics.BezierEvaluateCubic(
                p1.PointPosition, p1.ForwardControlPointPosition,
                p2.BackwardControlPointPosition, p2.PointPosition, t);
            
            sample.Tangent = CircuitMathematics.BezierEvaluateCubicDerivative(
                p1.PointPosition, p1.ForwardControlPointPosition,
                p2.BackwardControlPointPosition, p2.PointPosition, t);
            
            if (sample.Tangent.LengthSquared() < 1e-6f)
            {
                sample.Tangent = Vector3.Normalize(p2.PointPosition - p1.PointPosition);
            }
            else
            {
                sample.Tangent = Vector3.Normalize(sample.Tangent);
            }

            // Calculate up direction (interpolate between points) and ensure orthogonality
            Vector3 interpolatedUp = Vector3.Lerp(p1.UpDirection, p2.UpDirection, t);
            sample.Up = interpolatedUp - Vector3.Dot(interpolatedUp, sample.Tangent) * sample.Tangent;
            
            if (sample.Up.LengthSquared() < 1e-6f)
            {
                // Fallback if up is parallel to tangent
                sample.Up = new Vector3(0, 1, 0);
                sample.Up = sample.Up - Vector3.Dot(sample.Up, sample.Tangent) * sample.Tangent;
                if (sample.Up.LengthSquared() < 1e-6f)
                {
                    sample.Up = new Vector3(1, 0, 0);
                }
            }
            sample.Up = Vector3.Normalize(sample.Up);
            
            // Calculate orthonormal frame: in right-handed system
            // Right = Up × Forward (tangent)
            sample.Right = Vector3.Normalize(Vector3.Cross(sample.Up, sample.Tangent));
            
            // Recalculate up to ensure perfect orthonormality: Up = Forward × Right
            sample.Up = Vector3.Normalize(Vector3.Cross(sample.Tangent, sample.Right));
            
            // Calculate curvature
            sample.Curvature = CircuitMathematics.CalculateCurvature(
                p1.PointPosition, p1.ForwardControlPointPosition,
                p2.BackwardControlPointPosition, p2.PointPosition, t);
            
            return sample;
        }

        /// <summary>
        /// Refines sample list by subdividing regions with high curvature.
        /// </summary>
        private static List<CurveSample> RefineSamplesByCurvature(
            CircuitPointData p1,
            CircuitPointData p2,
            List<CurveSample> samples,
            WaypointSettings settings)
        {
            var refined = new List<CurveSample>();
            
            for (int i = 0; i < samples.Count - 1; i++)
            {
                refined.Add(samples[i]);
                
                // Check if we need to add a point between samples[i] and samples[i+1]
                float maxCurvature = Math.Max(samples[i].Curvature, samples[i + 1].Curvature);
                
                // If curvature is high, subdivide this interval
                if (maxCurvature > settings.CurvatureThreshold)
                {
                    float midT = (samples[i].T + samples[i + 1].T) / 2f;
                    var midSample = EvaluateCurveSample(p1, p2, midT);
                    refined.Add(midSample);
                }
            }
            
            // Add the last sample
            refined.Add(samples[samples.Count - 1]);
            
            return refined;
        }

        /// <summary>
        /// Filters samples to respect minimum and maximum spacing constraints.
        /// </summary>
        private static List<CurveSample> FilterBySpacing(List<CurveSample> samples, WaypointSettings settings)
        {
            if (samples.Count <= 2)
            {
                return samples;
            }

            var filtered = new List<CurveSample>();
            filtered.Add(samples[0]); // Always keep first point
            
            Vector3 lastAcceptedPos = samples[0].Position;
            
            for (int i = 1; i < samples.Count - 1; i++)
            {
                float distance = Vector3.Distance(lastAcceptedPos, samples[i].Position);
                
                // Only add if we're beyond minimum spacing
                if (distance >= settings.MinWaypointSpacing)
                {
                    filtered.Add(samples[i]);
                    lastAcceptedPos = samples[i].Position;
                }
            }
            
            // Always keep last point
            filtered.Add(samples[samples.Count - 1]);
            
            // Now ensure we don't have gaps larger than max spacing
            var finalFiltered = new List<CurveSample>();
            for (int i = 0; i < filtered.Count - 1; i++)
            {
                finalFiltered.Add(filtered[i]);
                
                float distance = Vector3.Distance(filtered[i].Position, filtered[i + 1].Position);
                if (distance > settings.MaxWaypointSpacing)
                {
                    // Need to add intermediate points
                    int subdivisions = (int)Math.Ceiling(distance / settings.MaxWaypointSpacing);
                    for (int j = 1; j < subdivisions; j++)
                    {
                        float t = (float)j / subdivisions;
                        float interpolatedT = filtered[i].T + t * (filtered[i + 1].T - filtered[i].T);
                        
                        // We need the original p1/p2 here - for now, lerp the samples
                        // This is approximate but should be fine for spacing enforcement
                        Vector3 lerpedTangent = Vector3.Normalize(Vector3.Lerp(filtered[i].Tangent, filtered[i + 1].Tangent, t));
                        Vector3 lerpedUp = Vector3.Normalize(Vector3.Lerp(filtered[i].Up, filtered[i + 1].Up, t));
                        
                        // Recalculate right and up to ensure orthonormal frame
                        // Right = Up × Forward (tangent)
                        Vector3 lerpedRight = Vector3.Normalize(Vector3.Cross(lerpedUp, lerpedTangent));
                        // Up = Forward × Right
                        lerpedUp = Vector3.Normalize(Vector3.Cross(lerpedTangent, lerpedRight));
                        
                        var interpolated = new CurveSample
                        {
                            T = interpolatedT,
                            Position = Vector3.Lerp(filtered[i].Position, filtered[i + 1].Position, t),
                            Tangent = lerpedTangent,
                            Up = lerpedUp,
                            Right = lerpedRight,
                            Curvature = (filtered[i].Curvature + filtered[i + 1].Curvature) / 2f
                        };
                        finalFiltered.Add(interpolated);
                    }
                }
            }
            finalFiltered.Add(filtered[filtered.Count - 1]);
            
            return finalFiltered;
        }

        /// <summary>
        /// Converts a curve sample to a waypoint with full transform data.
        /// </summary>
        private static WaypointData CreateWaypointFromSample(
            CurveSample sample,
            float roadWidth,
            WaypointSettings settings)
        {
            var waypoint = new WaypointData();
            
            waypoint.Position = sample.Position;
            
            // Create rotation: Forward = Tangent, Up = Up, Right = Right
            // Convert to quaternion
            waypoint.Rotation = CreateRotationFromAxes(sample.Tangent, sample.Up, sample.Right);
            
            // Calculate scale
            // Width = road width + buffer
            // Height = settings height
            // Depth = settings depth
            waypoint.Scale = new Vector3(
                roadWidth + settings.WidthBuffer,
                settings.Height,
                settings.Depth
            );
            
            return waypoint;
        }

        /// <summary>
        /// Creates a quaternion rotation from forward, up, and right vectors.
        /// Uses rotation matrix to quaternion conversion where:
        /// - Forward (tangent) = Z axis of the waypoint
        /// - Up = Y axis of the waypoint
        /// - Right = X axis of the waypoint
        /// </summary>
        private static Quaternion CreateRotationFromAxes(Vector3 forward, Vector3 up, Vector3 right)
        {
            // Build rotation matrix with proper axis alignment:
            // Column 0 (X-axis) = right
            // Column 1 (Y-axis) = up  
            // Column 2 (Z-axis) = forward
            
            float m00 = right.X;    float m01 = up.X;    float m02 = forward.X;
            float m10 = right.Y;    float m11 = up.Y;    float m12 = forward.Y;
            float m20 = right.Z;    float m21 = up.Z;    float m22 = forward.Z;
            
            // Convert rotation matrix to quaternion
            float trace = m00 + m11 + m22;
            
            if (trace > 0f)
            {
                float s = (float)Math.Sqrt(trace + 1.0f) * 2f; // s = 4 * qw
                return new Quaternion(
                    (m21 - m12) / s,
                    (m02 - m20) / s,
                    (m10 - m01) / s,
                    0.25f * s
                );
            }
            else if (m00 > m11 && m00 > m22)
            {
                float s = (float)Math.Sqrt(1.0f + m00 - m11 - m22) * 2f; // s = 4 * qx
                return new Quaternion(
                    0.25f * s,
                    (m01 + m10) / s,
                    (m02 + m20) / s,
                    (m21 - m12) / s
                );
            }
            else if (m11 > m22)
            {
                float s = (float)Math.Sqrt(1.0f + m11 - m00 - m22) * 2f; // s = 4 * qy
                return new Quaternion(
                    (m01 + m10) / s,
                    0.25f * s,
                    (m12 + m21) / s,
                    (m02 - m20) / s
                );
            }
            else
            {
                float s = (float)Math.Sqrt(1.0f + m22 - m00 - m11) * 2f; // s = 4 * qz
                return new Quaternion(
                    (m02 + m20) / s,
                    (m12 + m21) / s,
                    0.25f * s,
                    (m10 - m01) / s
                );
            }
        }

        /// <summary>
        /// Helper struct to store curve evaluation results.
        /// </summary>
        private struct CurveSample
        {
            public float T;
            public Vector3 Position;
            public Vector3 Tangent;
            public Vector3 Up;
            public Vector3 Right;
            public float Curvature;
        }
    }
}
