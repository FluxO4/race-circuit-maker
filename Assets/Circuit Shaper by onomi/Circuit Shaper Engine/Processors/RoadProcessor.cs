// RoadProcessor.cs - Updated with validation and debug logging
using OnomiCircuitShaper.Engine.EditRealm;
using OnomiCircuitShaper.Engine.Data;
using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;

namespace OnomiCircuitShaper.Engine.Processors
{
    /// <summary>
    /// A struct to hold generic mesh data, making it engine-agnostic.
    /// This allows the core engine to generate geometry without depending on Unity's Mesh class.
    /// </summary>
    /// <remarks>
    /// [Look here onomi] The Unity layer converts this to UnityEngine.Mesh.
    /// Vertices are in world space, UVs are normalized 0-1 coordinates,
    /// Triangles array uses counter-clockwise winding order for front-facing.
    /// </remarks>
    public struct GenericMeshData
    {
        public Vector3[] Vertices;
        public Vector2[] UVs;
        public int[] Triangles;

        public int MaterialID;
    }

    /// <summary>
    /// A static class containing logic for generating mesh data for roads, bridges, and railings.
    /// All methods are pure functions that take edit-realm objects and return GenericMeshData.
    /// </summary>
    /// <remarks>
    /// [Look here onomi] The road generation algorithm works as follows:
    /// 1. Iterate through pairs of adjacent CircuitPoints in the road's AssociatedPoints
    /// 2. For each segment, create a grid of vertices by calling CurveProcessor.LerpBetweenTwoCrossSections
    /// 3. Use WidthWiseVertexCount for cross-section resolution
    /// 4. Use LengthWiseVertexCountPerUnitWidthWiseVertexCount to calculate length resolution
    /// 5. Generate UVs based on UVTile and UVOffset parameters
    /// 6. Build triangle indices to form quads between vertex rows
    /// 
    /// BURST COMPATIBILITY NOTES:
    /// - Core algorithm uses only value types (Vector3, Vector2, float, int)
    /// - To make this Burst-compatible later, extract point data into NativeArrays first
    /// - Separate data extraction (managed) from mesh generation (pure math)
    /// </remarks>
    public static class RoadProcessor
    {
        /// <summary>
        /// Generates the vertex, UV, and triangle data for a road mesh.
        /// Uses the road's associated points and their cross-sections to create the geometry.
        /// </summary>
        /// <param name="road">The live road object containing all necessary data and references.</param>
        /// <returns>Engine-agnostic mesh data ready for rendering.</returns>
        /// <remarks>
        /// [Look here onomi] This needs to handle the Min/Max properties to generate only
        /// a portion of the full road length if desired.
        /// </remarks>
        public static GenericMeshData BuildRoadMesh(Road road)
        {
            // Note: Same start and end segment is valid (single segment road)
            // Only check that parent curve has points
            if (road.parentCurve == null || road.parentCurve.Points.Count < 2)
            {

                return new GenericMeshData();
            }


            //Extract points from the parent curve using the range

            List<CircuitPoint> pointArray = road.parentCurve.GetPointsFromSegmentRange(road.Data.startSegmentIndex, road.Data.endSegmentIndex);

            CircuitPointData[] pointDataArray = pointArray.Select(p => p.Data).ToArray();

            // Validate cross-sections
            for (int i = 0; i < pointArray.Count; i++)
            {
                var point = pointArray[i];
                if (point?.CrossSection?.Points == null || point.CrossSection.Points.Count < 2)
                {
                    return new GenericMeshData();
                }
            }




            // Calculate mesh dimensions
            int widthSegments = road.Data.WidthWiseVertexCount - 1;
            int lengthSegmentsPerPoint = Math.Max(1, (int)(widthSegments * road.Data.LengthWiseVertexCountPerUnitWidthWiseVertexCount));
            int totalLengthSegments = (pointDataArray.Length - 1) * lengthSegmentsPerPoint;

            int vertexCountWidth = road.Data.WidthWiseVertexCount;
            int vertexCountLength = totalLengthSegments + 1;
            int totalVertices = vertexCountWidth * vertexCountLength;

            // Allocate arrays
            var vertices = new Vector3[totalVertices];
            var uvs = new Vector2[totalVertices];
            var triangles = new int[widthSegments * totalLengthSegments * 6]; // 2 triangles per quad, 3 indices each

            // Generate vertices and UVs
            int vertexIndex = 0;
            for (int lengthIndex = 0; lengthIndex < vertexCountLength; lengthIndex++)
            {
                // Determine which segment we're in and local t value
                float globalT = (float)lengthIndex / totalLengthSegments;
                int segmentIndex = Math.Min((int)(globalT * (pointDataArray.Length - 1)), pointDataArray.Length - 2);
                float localT = (globalT * (pointDataArray.Length - 1)) - segmentIndex;
                localT = Math.Clamp(localT, 0f, 1f);

                var p1 = pointDataArray[segmentIndex];
                var p2 = pointDataArray[segmentIndex + 1];

                // Normalize cross-section curves before interpolation
                CurveProcessor.NormaliseCurvePoints(p1.CrossSectionCurve);
                CurveProcessor.NormaliseCurvePoints(p2.CrossSectionCurve);

                for (int widthIndex = 0; widthIndex < vertexCountWidth; widthIndex++)
                {
                    float widthT = (float)widthIndex / widthSegments;

                    // Generate vertex position using the cross-section interpolation
                    vertices[vertexIndex] = CurveProcessor.LerpBetweenTwoCrossSections(p1, p2, widthT, localT);

                    // Generate UV coordinates
                    float u = widthT * road.Data.UVTile.x + road.Data.UVOffset.x;
                    float v = globalT * road.Data.UVTile.y + road.Data.UVOffset.y;
                    uvs[vertexIndex] = new Vector2(u, v);

                    vertexIndex++;
                }
            }

            // Generate triangle indices
            int triangleIndex = 0;
            for (int lengthIndex = 0; lengthIndex < totalLengthSegments; lengthIndex++)
            {
                for (int widthIndex = 0; widthIndex < widthSegments; widthIndex++)
                {
                    // Calculate the four corners of this quad
                    int bottomLeft = lengthIndex * vertexCountWidth + widthIndex;
                    int bottomRight = bottomLeft + 1;
                    int topLeft = bottomLeft + vertexCountWidth;
                    int topRight = topLeft + 1;

                    // First triangle (clockwise for upward-facing normal)
                    triangles[triangleIndex++] = bottomLeft;
                    triangles[triangleIndex++] = bottomRight;  // Swapped
                    triangles[triangleIndex++] = topLeft;      // Swapped

                    // Second triangle (clockwise for upward-facing normal)
                    triangles[triangleIndex++] = bottomRight;
                    triangles[triangleIndex++] = topRight;     // Swapped
                    triangles[triangleIndex++] = topLeft;      // Swapped
                }
            }

            return new GenericMeshData
            {
                Vertices = vertices,
                UVs = uvs,
                Triangles = triangles,
                MaterialID = road.Data.MaterialIndex
            };
        }



        /// <summary>
        /// Generates a flexible ribbon mesh along the road path.
        /// Start and end positions can be either fixed 2D offsets OR cross-section t-values (0-1).
        /// When useCrossSectionPosition is true, startPos.X and endPos.X are treated as t-values along the cross-section curve.
        /// When false, they are treated as direct offsets from curve center.
        /// </summary>
        /// <param name="pointDataArray">Array of circuit points defining the road path</param>
        /// <param name="startPos">2D position (X=horizontal, Y=vertical offset or cross-section t if useCrossSectionPosition=true)</param>
        /// <param name="endPos">2D position (X=horizontal, Y=vertical offset or cross-section t if useCrossSectionPosition=true)</param>
        /// <param name="alongLengthMinMax">Min/max range along road length (0-1)</param>
        /// <param name="lengthSegmentsPerPoint">Number of segments per point pair</param>
        /// <param name="uvTile">UV tiling for texture mapping</param>
        /// <param name="uvOffset">UV offset for texture mapping</param>
        /// <param name="useCrossSectionPosition">If true, startPos.X and endPos.X are cross-section t-values (0-1), otherwise they're direct offsets</param>
        /// <returns>Generic mesh data for the ribbon</returns>
        private static GenericMeshData BuildRibbon(
            CircuitPointData[] pointDataArray,
            Vector2 startPos,
            Vector2 endPos,
            Vector2 alongLengthMinMax,
            int lengthSegmentsPerPoint,
            Vector2 uvTile,
            Vector2 uvOffset,
            bool useCrossSectionPosition = false)
        {
            if (pointDataArray == null || pointDataArray.Length < 2)
            {
                return new GenericMeshData();
            }

            // Clamp length range
            alongLengthMinMax = new Vector2(
                Math.Clamp(alongLengthMinMax.X, 0f, 1f),
                Math.Clamp(alongLengthMinMax.Y, 0f, 1f)
            );
            if (alongLengthMinMax.X >= alongLengthMinMax.Y)
            {
                alongLengthMinMax = new Vector2(alongLengthMinMax.X, alongLengthMinMax.X + 0.01f);
            }

            int totalLengthSegments = (pointDataArray.Length - 1) * lengthSegmentsPerPoint;
            int startSegmentIndex = (int)(alongLengthMinMax.X * totalLengthSegments);
            int endSegmentIndex = (int)(alongLengthMinMax.Y * totalLengthSegments);
            int activeSegmentCount = endSegmentIndex - startSegmentIndex;

            if (activeSegmentCount <= 0)
            {
                return new GenericMeshData();
            }

            int vertexCountLength = activeSegmentCount + 1;
            int totalVertices = 2 * vertexCountLength;
            var vertices = new Vector3[totalVertices];
            var uvs = new Vector2[totalVertices];
            var triangles = new int[activeSegmentCount * 6];

            // Generate vertices and UVs
            int vertexIndex = 0;
            for (int i = 0; i < vertexCountLength; i++)
            {
                int actualSegmentIndex = startSegmentIndex + i;
                float globalT = (float)actualSegmentIndex / totalLengthSegments;
                int segmentIndex = Math.Min((int)(globalT * (pointDataArray.Length - 1)), pointDataArray.Length - 2);
                float localT = (globalT * (pointDataArray.Length - 1)) - segmentIndex;
                localT = Math.Clamp(localT, 0f, 1f);

                var p1 = pointDataArray[segmentIndex];
                var p2 = pointDataArray[segmentIndex + 1];

                // Normalize cross-section curves
                CurveProcessor.NormaliseCurvePoints(p1.CrossSectionCurve);
                CurveProcessor.NormaliseCurvePoints(p2.CrossSectionCurve);

                Vector3 vertex1, vertex2;

                if (useCrossSectionPosition)
                {
                    // Sample cross-sections at both points and interpolate
                    var crossSection1Start = CurveProcessor.LerpAlongCurve(p1.CrossSectionCurve, startPos.X);
                    var crossSection2Start = CurveProcessor.LerpAlongCurve(p2.CrossSectionCurve, startPos.X);
                    var crossSection1End = CurveProcessor.LerpAlongCurve(p1.CrossSectionCurve, endPos.X);
                    var crossSection2End = CurveProcessor.LerpAlongCurve(p2.CrossSectionCurve, endPos.X);

                    // Interpolate cross-section positions between the two points (use XY components only)
                    Vector2 interpolatedStart = new Vector2(
                        crossSection1Start.X + (crossSection2Start.X - crossSection1Start.X) * localT,
                        crossSection1Start.Y + (crossSection2Start.Y - crossSection1Start.Y) * localT);
                    Vector2 interpolatedEnd = new Vector2(
                        crossSection1End.X + (crossSection2End.X - crossSection1End.X) * localT,
                        crossSection1End.Y + (crossSection2End.Y - crossSection1End.Y) * localT);

                    // Get base position at curve center
                    Vector3 curvePosition = CircuitMathematics.BezierEvaluateCubic(
                        p1.PointPosition, p1.ForwardControlPointPosition,
                        p2.BackwardControlPointPosition, p2.PointPosition, localT);

                    // Calculate local coordinate system
                    Vector3 curveTangent = CircuitMathematics.BezierEvaluateCubicDerivative(
                        p1.PointPosition, p1.ForwardControlPointPosition,
                        p2.BackwardControlPointPosition, p2.PointPosition, localT);

                    if (curveTangent.LengthSquared() < 1e-6f)
                    {
                        curveTangent = Vector3.Normalize((Vector3)p2.PointPosition - (Vector3)p1.PointPosition);
                    }
                    else
                    {
                        curveTangent = Vector3.Normalize(curveTangent);
                    }

                    Vector3 upDir = Vector3.Normalize(Vector3.Lerp((Vector3)p1.UpDirection, (Vector3)p2.UpDirection, localT));
                    upDir = upDir - Vector3.Dot(upDir, curveTangent) * curveTangent;
                    if (upDir.LengthSquared() < 1e-6f)
                    {
                        upDir = Vector3.UnitY - Vector3.Dot(Vector3.UnitY, curveTangent) * curveTangent;
                    }
                    upDir = Vector3.Normalize(upDir);

                    Vector3 rightDir = Vector3.Normalize(Vector3.Cross(curveTangent, upDir));

                    // Apply interpolated cross-section offsets plus vertical offsets
                    vertex1 = curvePosition + rightDir * interpolatedStart.X + upDir * (interpolatedStart.Y + startPos.Y);
                    vertex2 = curvePosition + rightDir * interpolatedEnd.X + upDir * (interpolatedEnd.Y + endPos.Y);
                }
                else
                {
                    // Original behavior: use fixed offsets from curve center
                    Vector3 curvePosition = CircuitMathematics.BezierEvaluateCubic(
                        p1.PointPosition, p1.ForwardControlPointPosition,
                        p2.BackwardControlPointPosition, p2.PointPosition, localT);

                    Vector3 curveTangent = CircuitMathematics.BezierEvaluateCubicDerivative(
                        p1.PointPosition, p1.ForwardControlPointPosition,
                        p2.BackwardControlPointPosition, p2.PointPosition, localT);

                    if (curveTangent.LengthSquared() < 1e-6f)
                    {
                        curveTangent = Vector3.Normalize((Vector3)p2.PointPosition - (Vector3)p1.PointPosition);
                    }
                    else
                    {
                        curveTangent = Vector3.Normalize(curveTangent);
                    }

                    Vector3 upDir = Vector3.Normalize(Vector3.Lerp((Vector3)p1.UpDirection, (Vector3)p2.UpDirection, localT));
                    upDir = upDir - Vector3.Dot(upDir, curveTangent) * curveTangent;
                    if (upDir.LengthSquared() < 1e-6f)
                    {
                        upDir = Vector3.UnitY - Vector3.Dot(Vector3.UnitY, curveTangent) * curveTangent;
                    }
                    upDir = Vector3.Normalize(upDir);

                    Vector3 rightDir = Vector3.Normalize(Vector3.Cross(curveTangent, upDir));

                    vertex1 = curvePosition + rightDir * startPos.X + upDir * startPos.Y;
                    vertex2 = curvePosition + rightDir * endPos.X + upDir * endPos.Y;
                }

                vertices[vertexIndex] = vertex1;
                float vCoord = ((float)i / activeSegmentCount) * uvTile.Y + uvOffset.Y;
                uvs[vertexIndex] = new Vector2(uvOffset.X, vCoord);
                vertexIndex++;

                vertices[vertexIndex] = vertex2;
                uvs[vertexIndex] = new Vector2(uvTile.X + uvOffset.X, vCoord);
                vertexIndex++;
            }

            // Generate triangle indices with consistent winding
            int triangleIndex = 0;
            for (int i = 0; i < activeSegmentCount; i++)
            {
                int bottomLeft = i * 2;
                int bottomRight = bottomLeft + 1;
                int topLeft = bottomLeft + 2;
                int topRight = topLeft + 1;

                // First triangle: counter-clockwise when viewed from outside
                triangles[triangleIndex++] = bottomLeft;
                triangles[triangleIndex++] = bottomRight;
                triangles[triangleIndex++] = topLeft;

                // Second triangle: counter-clockwise when viewed from outside
                triangles[triangleIndex++] = topLeft;
                triangles[triangleIndex++] = bottomRight;
                triangles[triangleIndex++] = topRight;
            }

            return new GenericMeshData
            {
                Vertices = vertices,
                UVs = uvs,
                Triangles = triangles
            };
        }

        /// <summary>
        /// Generates the vertex, UV, and triangle data for a bridge mesh.
        /// The bridge wraps around the road using a template-based or custom shape.
        /// </summary>
        public static GenericMeshData BuildBridgeMesh(Bridge bridge, Road parentRoad)
        {
            UnityEngine.Debug.Log("[RoadProcessor] BuildBridgeMesh called");

            if (bridge == null || parentRoad == null || parentRoad.parentCurve == null)
            {
                UnityEngine.Debug.Log("[RoadProcessor] BuildBridgeMesh: Early return - null check failed");
                return new GenericMeshData();
            }

            if (!bridge.Data.Enabled)
            {
                UnityEngine.Debug.Log("[RoadProcessor] BuildBridgeMesh: Bridge disabled");
                return new GenericMeshData();
            }

            List<CircuitPoint> pointArray = parentRoad.parentCurve.GetPointsFromSegmentRange(
                parentRoad.Data.startSegmentIndex,
                parentRoad.Data.endSegmentIndex);

            UnityEngine.Debug.Log($"[RoadProcessor] BuildBridgeMesh: Point array count = {pointArray.Count}");

            if (pointArray.Count < 2)
            {
                UnityEngine.Debug.Log("[RoadProcessor] BuildBridgeMesh: Not enough points");
                return new GenericMeshData();
            }

            CircuitPointData[] pointDataArray = pointArray.Select(p => p.Data).ToArray();

            // Generate bridge shape points from template
            List<Vector2> bridgeShapePoints = new List<Vector2>();

            if (bridge.Data.UseTemplate)
            {
                var data = bridge.Data;
                // Create I-beam style bridge profile (one half, will be mirrored)
                bridgeShapePoints.Add(new Vector2(0, 0)); // Road surface edge
                bridgeShapePoints.Add(new Vector2(0, data.TemplateCurbHeight)); // Curb top
                bridgeShapePoints.Add(new Vector2(data.TemplateEdgeWidth, data.TemplateCurbHeight)); // Edge width
                bridgeShapePoints.Add(new Vector2(data.TemplateEdgeWidth, data.TemplateCurbHeight - data.TemplateFlangeDepth)); // Before flange
                bridgeShapePoints.Add(new Vector2(data.TemplateEdgeWidth + data.TemplateFlangeWidth, data.TemplateCurbHeight - data.TemplateFlangeDepth)); // Flange outer
                bridgeShapePoints.Add(new Vector2(data.TemplateEdgeWidth + data.TemplateFlangeWidth, data.TemplateCurbHeight - data.TemplateFlangeDepth - data.TemplateFlangeHeight)); // Flange bottom
                bridgeShapePoints.Add(new Vector2(data.TemplateEdgeWidth, data.TemplateCurbHeight - data.TemplateFlangeDepth - data.TemplateFlangeHeight)); // After flange
                bridgeShapePoints.Add(new Vector2(0, data.TemplateCurbHeight - data.TemplateBridgeHeight)); // Bottom point
            }
            else
            {
                // Use custom shape points
                foreach (var point in bridge.Data.BridgeShapePoints)
                {
                    bridgeShapePoints.Add((Vector2)point);
                }
            }

            UnityEngine.Debug.Log($"[RoadProcessor] BuildBridgeMesh: Bridge shape points count = {bridgeShapePoints.Count}, UseTemplate = {bridge.Data.UseTemplate}");

            if (bridgeShapePoints.Count < 2)
            {
                UnityEngine.Debug.Log("[RoadProcessor] BuildBridgeMesh: Not enough shape points");
                return new GenericMeshData();
            }

            // Calculate mesh resolution
            int widthSegments = parentRoad.Data.WidthWiseVertexCount - 1;
            int lengthSegmentsPerPoint = Math.Max(1, (int)(widthSegments * parentRoad.Data.LengthWiseVertexCountPerUnitWidthWiseVertexCount));

            UnityEngine.Debug.Log($"[RoadProcessor] BuildBridgeMesh: widthSegments = {widthSegments}, lengthSegmentsPerPoint = {lengthSegmentsPerPoint}");

            // Build bridge mesh using ribbons for each segment of the bridge profile
            // The bridge profile defines a 2D shape that wraps around both edges of the road
            List<GenericMeshData> ribbons = new List<GenericMeshData>();

            // Get left and right edge positions (assuming cross-section is centered)
            // We need to get the actual edge positions from the first point's cross-section
            CurveProcessor.NormaliseCurvePoints(pointDataArray[0].CrossSectionCurve);
            var leftEdgePos3 = CurveProcessor.LerpAlongCurve(pointDataArray[0].CrossSectionCurve, 0f);
            var rightEdgePos3 = CurveProcessor.LerpAlongCurve(pointDataArray[0].CrossSectionCurve, 1f);

            Vector2 leftEdgePos = new Vector2(leftEdgePos3.X, leftEdgePos3.Y);
            Vector2 rightEdgePos = new Vector2(rightEdgePos3.X, rightEdgePos3.Y);

            
            // Create ribbons for left side of bridge
            for (int i = 0; i < bridgeShapePoints.Count - 1; i++)
            {
                Vector2 p1 = bridgeShapePoints[i] + leftEdgePos;
                Vector2 p2 = bridgeShapePoints[i + 1] + leftEdgePos;

                UnityEngine.Debug.Log($"[RoadProcessor] BuildBridgeMesh: Creating left ribbon {i}, p1 = {p1}, p2 = {p2}");

                // Build ribbon from p1 to p2 on left side
                GenericMeshData leftRibbon = BuildRibbon(
                    pointDataArray,
                    p2,  // Start position
                    p1,  // End position
                    new Vector2(0, 1), // Full length (0 to 1)
                    lengthSegmentsPerPoint,
                    bridge.Data.UVTile,
                    bridge.Data.UVOffset, true);

                UnityEngine.Debug.Log($"[RoadProcessor] BuildBridgeMesh: Left ribbon {i} has {leftRibbon.Vertices?.Length ?? 0} vertices");

                if (leftRibbon.Vertices != null && leftRibbon.Vertices.Length > 0)
                {
                    ribbons.Add(leftRibbon);
                }
            }

            // Create bottom connecting ribbon
            Vector2 bottomLeft = bridgeShapePoints[bridgeShapePoints.Count - 1];

            UnityEngine.Debug.Log($"[RoadProcessor] BuildBridgeMesh: Creating bottom ribbon, bottomLeft = {bottomLeft}");

            GenericMeshData bottomRibbon = BuildRibbon(
                pointDataArray,
                new Vector2(bottomLeft.X, bottomLeft.Y) + rightEdgePos,   
                new Vector2(bottomLeft.X, bottomLeft.Y) + leftEdgePos, 
                new Vector2(0, 1),
                lengthSegmentsPerPoint,
                bridge.Data.UVTile,
                bridge.Data.UVOffset, true);

            UnityEngine.Debug.Log($"[RoadProcessor] BuildBridgeMesh: Bottom ribbon has {bottomRibbon.Vertices?.Length ?? 0} vertices");

            if (bottomRibbon.Vertices != null && bottomRibbon.Vertices.Length > 0)
            {
                ribbons.Add(bottomRibbon);
            }

            // Create ribbons for right side of bridge (mirrored)
            for (int i = bridgeShapePoints.Count - 1; i > 0; i--)
            {
                Vector2 p1 = bridgeShapePoints[i] + rightEdgePos;
                Vector2 p2 = bridgeShapePoints[i - 1] + rightEdgePos;

                UnityEngine.Debug.Log($"[RoadProcessor] BuildBridgeMesh: Creating right ribbon {i}, p1 = {p1}, p2 = {p2}");

                // Build ribbon from p1 to p2 on right side (mirrored)
                GenericMeshData rightRibbon = BuildRibbon(
                    pointDataArray,
                    p2,  // Start position (mirrored)
                    p1,  // End position (mirrored)
                    new Vector2(0, 1),
                    lengthSegmentsPerPoint,
                    bridge.Data.UVTile,
                    bridge.Data.UVOffset, true);

                UnityEngine.Debug.Log($"[RoadProcessor] BuildBridgeMesh: Right ribbon {i} has {rightRibbon.Vertices?.Length ?? 0} vertices");

                if (rightRibbon.Vertices != null && rightRibbon.Vertices.Length > 0)
                {
                    ribbons.Add(rightRibbon);
                }
            }

            UnityEngine.Debug.Log($"[RoadProcessor] BuildBridgeMesh: Total ribbons collected = {ribbons.Count}");

            // Combine all ribbons into one mesh
            var result = CombineMeshData(ribbons.ToArray(), bridge.Data.MaterialIndex);

            UnityEngine.Debug.Log($"[RoadProcessor] BuildBridgeMesh: Final mesh has {result.Vertices?.Length ?? 0} vertices");

            return result;
        }

        /// <summary>
        /// Generates the vertex, UV, and triangle data for a railing mesh.
        /// A railing is a simple vertical ribbon extending upward from a road edge.
        /// </summary>
        public static GenericMeshData BuildRailingMesh(Railing railing, Road parentRoad)
        {
            if (railing == null || parentRoad == null || parentRoad.parentCurve == null)
            {
                return new GenericMeshData();
            }

            List<CircuitPoint> pointArray = parentRoad.parentCurve.GetPointsFromSegmentRange(
                parentRoad.Data.startSegmentIndex,
                parentRoad.Data.endSegmentIndex);

            if (pointArray.Count < 2)
            {
                return new GenericMeshData();
            }

            CircuitPointData[] pointDataArray = pointArray.Select(p => p.Data).ToArray();

            // Calculate mesh resolution
            int widthSegments = parentRoad.Data.WidthWiseVertexCount - 1;
            int lengthSegmentsPerPoint = Math.Max(1, (int)(widthSegments * parentRoad.Data.LengthWiseVertexCountPerUnitWidthWiseVertexCount));

            // Build railing as a vertical ribbon using cross-section positions
            // startPos.X and endPos.X are the cross-section t-value (HorizontalPosition)
            // startPos.Y and endPos.Y are the vertical offsets (0 for base, RailingHeight for top)
            GenericMeshData railingMesh = BuildRibbon(
                pointDataArray,
                new Vector2(railing.Data.HorizontalPosition, 0f),  // Cross-section position, at road surface
                new Vector2(railing.Data.HorizontalPosition, railing.Data.RailingHeight),  // Same position, extended upward
                new Vector2(railing.Data.Min, railing.Data.Max),  // Length range
                lengthSegmentsPerPoint,
                Vector2.One,
                Vector2.Zero,
                useCrossSectionPosition: true);

            railingMesh.MaterialID = railing.Data.MaterialIndex;
            return railingMesh;
        }

        /// <summary>
        /// Combines multiple mesh data objects into a single mesh.
        /// </summary>
        private static GenericMeshData CombineMeshData(GenericMeshData[] meshes, int materialID)
        {
            if (meshes == null || meshes.Length == 0)
            {
                return new GenericMeshData();
            }

            // Calculate total sizes
            int totalVertices = 0;
            int totalTriangles = 0;
            foreach (var mesh in meshes)
            {
                if (mesh.Vertices != null)
                {
                    totalVertices += mesh.Vertices.Length;
                    totalTriangles += mesh.Triangles.Length;
                }
            }

            if (totalVertices == 0)
            {
                return new GenericMeshData();
            }

            // Allocate combined arrays
            var combinedVertices = new Vector3[totalVertices];
            var combinedUVs = new Vector2[totalVertices];
            var combinedTriangles = new int[totalTriangles];

            int vertexOffset = 0;
            int triangleOffset = 0;

            foreach (var mesh in meshes)
            {
                if (mesh.Vertices == null) continue;

                // Copy vertices and UVs
                Array.Copy(mesh.Vertices, 0, combinedVertices, vertexOffset, mesh.Vertices.Length);
                Array.Copy(mesh.UVs, 0, combinedUVs, vertexOffset, mesh.UVs.Length);

                // Copy and offset triangle indices
                for (int i = 0; i < mesh.Triangles.Length; i++)
                {
                    combinedTriangles[triangleOffset + i] = mesh.Triangles[i] + vertexOffset;
                }

                vertexOffset += mesh.Vertices.Length;
                triangleOffset += mesh.Triangles.Length;
            }

            return new GenericMeshData
            {
                Vertices = combinedVertices,
                UVs = combinedUVs,
                Triangles = combinedTriangles,
                MaterialID = materialID
            };
        }
    }
}
