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
        public static GenericMeshData BuildRoadMesh(Road road)
        {
            if (road.parentCurve == null || road.parentCurve.Points.Count < 2)
            {
                return new GenericMeshData();
            }

            List<CircuitPoint> pointArray = road.parentCurve.GetPointsFromSegmentRange(road.Data.startSegmentIndex, road.Data.endSegmentIndex);
            if (pointArray.Count < 2) return new GenericMeshData();

            CircuitPointData[] pointDataArray = pointArray.Select(p => p.Data).ToArray();

            for (int i = 0; i < pointArray.Count; i++)
            {
                var point = pointArray[i];
                if (point?.CrossSection?.Points == null || point.CrossSection.Points.Count < 2)
                {
                    return new GenericMeshData();
                }
            }

            int widthSegments = road.Data.WidthWiseVertexCount - 1;
            int lengthSegmentsPerPoint = Math.Max(1, (int)(widthSegments * road.Data.LengthWiseVertexCountPerUnitWidthWiseVertexCount));
            int totalLengthSegments = (pointDataArray.Length - 1) * lengthSegmentsPerPoint;

            int vertexCountWidth = road.Data.WidthWiseVertexCount;
            int vertexCountLength = totalLengthSegments + 1;
            int totalVertices = vertexCountWidth * vertexCountLength;

            var vertices = new Vector3[totalVertices];
            var uvs = new Vector2[totalVertices];
            var triangles = new int[widthSegments * totalLengthSegments * 6];

            float currentLengthDistance = 0f;
            Vector3 previousCenterPoint = Vector3.Zero;

            int vertexIndex = 0;
            for (int lengthIndex = 0; lengthIndex < vertexCountLength; lengthIndex++)
            {
                float globalT = (float)lengthIndex / totalLengthSegments;
                int segmentIndex = Math.Min((int)(globalT * (pointDataArray.Length - 1)), pointDataArray.Length - 2);
                float localT = (globalT * (pointDataArray.Length - 1)) - segmentIndex;
                localT = Math.Clamp(localT, 0f, 1f);

                var p1 = pointDataArray[segmentIndex];
                var p2 = pointDataArray[segmentIndex + 1];

                CurveProcessor.NormaliseCurvePoints(p1.CrossSectionCurve);
                CurveProcessor.NormaliseCurvePoints(p2.CrossSectionCurve);

                // Calculate distance for UVs
                Vector3 centerPos = CircuitMathematics.BezierEvaluateCubic(
                    p1.PointPosition, p1.ForwardControlPointPosition,
                    p2.BackwardControlPointPosition, p2.PointPosition, localT);

                if (lengthIndex > 0)
                {
                    currentLengthDistance += Vector3.Distance(centerPos, previousCenterPoint);
                }
                previousCenterPoint = centerPos;

                float currentSectionWidth = 1.0f;
                if (road.Data.UseDistanceBasedWidthUV)
                {
                    Vector3 leftEdge = CurveProcessor.LerpBetweenTwoCrossSections(p1, p2, 0f, localT);
                    Vector3 rightEdge = CurveProcessor.LerpBetweenTwoCrossSections(p1, p2, 1f, localT);
                    currentSectionWidth = Vector3.Distance(leftEdge, rightEdge);
                }

                for (int widthIndex = 0; widthIndex < vertexCountWidth; widthIndex++)
                {
                    float widthT = (float)widthIndex / widthSegments;
                    vertices[vertexIndex] = CurveProcessor.LerpBetweenTwoCrossSections(p1, p2, widthT, localT);

                    float u, v;
                    if (road.Data.UseDistanceBasedWidthUV)
                    {
                        u = widthT * currentSectionWidth * road.Data.UVTile.x + road.Data.UVOffset.x;
                    }
                    else
                    {
                        u = widthT * road.Data.UVTile.x + road.Data.UVOffset.x;
                    }

                    if (road.Data.UseDistanceBasedLengthUV)
                    {
                        v = currentLengthDistance * road.Data.UVTile.y + road.Data.UVOffset.y;
                    }
                    else
                    {
                        v = globalT * road.Data.UVTile.y + road.Data.UVOffset.y;
                    }
                    
                    uvs[vertexIndex] = new Vector2(u, v);

                    vertexIndex++;
                }
            }

            int triangleIndex = 0;
            for (int lengthIndex = 0; lengthIndex < totalLengthSegments; lengthIndex++)
            {
                for (int widthIndex = 0; widthIndex < widthSegments; widthIndex++)
                {
                    int bottomLeft = lengthIndex * vertexCountWidth + widthIndex;
                    int bottomRight = bottomLeft + 1;
                    int topLeft = bottomLeft + vertexCountWidth;
                    int topRight = topLeft + 1;

                    triangles[triangleIndex++] = bottomLeft;
                    triangles[triangleIndex++] = bottomRight;
                    triangles[triangleIndex++] = topLeft;

                    triangles[triangleIndex++] = bottomRight;
                    triangles[triangleIndex++] = topRight;
                    triangles[triangleIndex++] = topLeft;
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
        /// Calculates the position, and orthonormal up and right vectors for a point along the main circuit curve.
        /// This logic is shared by all mesh generation functions to ensure consistency.
        /// </summary>
        private static void GetInterpolatedFrame(CircuitPointData p1, CircuitPointData p2, float localT, out Vector3 position, out Vector3 upDir, out Vector3 rightDir)
        {
            position = CircuitMathematics.BezierEvaluateCubic(
                p1.PointPosition, p1.ForwardControlPointPosition,
                p2.BackwardControlPointPosition, p2.PointPosition, localT);

            Vector3 curveTangent = CircuitMathematics.BezierEvaluateCubicDerivative(
                p1.PointPosition, p1.ForwardControlPointPosition,
                p2.BackwardControlPointPosition, p2.PointPosition, localT);

            if (curveTangent.LengthSquared() < 1e-6f)
            {
                curveTangent = (Vector3)p2.PointPosition - (Vector3)p1.PointPosition;
            }
            curveTangent = Vector3.Normalize(curveTangent);

            Vector3 interpolatedUp = Vector3.Lerp((Vector3)p1.UpDirection, (Vector3)p2.UpDirection, localT);
            
            // Gram-Schmidt orthogonalization to get a clean coordinate system
            upDir = interpolatedUp - Vector3.Dot(interpolatedUp, curveTangent) * curveTangent;
            if (upDir.LengthSquared() < 1e-6f)
            {
                // If up is parallel to tangent, find a new perpendicular vector.
                var randomVec = new Vector3(0, 1, 0);
                if (Math.Abs(Vector3.Dot(randomVec, curveTangent)) > 0.99f)
                {
                    randomVec = new Vector3(1, 0, 0);
                }
                upDir = Vector3.Cross(curveTangent, randomVec);
            }
            upDir = Vector3.Normalize(upDir);

            rightDir = Vector3.Normalize(Vector3.Cross(curveTangent, upDir));
        }

        /// <summary>
        /// Generates a flexible ribbon mesh along a specified portion of the road path.
        /// Allows specifying an origin (0-1 along cross-section) for both start and end of the ribbon width,
        /// plus an offset from that origin.
        /// </summary>
        private static GenericMeshData BuildRibbon(
            CircuitPointData[] pointDataArray,
            float startOriginT, Vector2 startOffset,
            float endOriginT, Vector2 endOffset,
            Vector2 alongLengthMinMax,
            int lengthSegmentsPerPoint,
            Vector2 uvTile,
            Vector2 uvOffset,
            bool useDistanceBasedWidthUV = false,
            bool useDistanceBasedLengthUV = false)
        {
            if (pointDataArray == null || pointDataArray.Length < 2)
            {
                return new GenericMeshData();
            }

            alongLengthMinMax = new Vector2(Math.Clamp(alongLengthMinMax.X, 0f, 1f), Math.Clamp(alongLengthMinMax.Y, 0f, 1f));
            if (alongLengthMinMax.X >= alongLengthMinMax.Y)
            {
                return new GenericMeshData();
            }

            int totalLengthSegments = (pointDataArray.Length - 1) * lengthSegmentsPerPoint;
            int startStep = (int)(alongLengthMinMax.X * totalLengthSegments);
            int endStep = (int)(alongLengthMinMax.Y * totalLengthSegments);
            int activeSegmentCount = endStep - startStep;

            if (activeSegmentCount <= 0)
            {
                return new GenericMeshData();
            }

            int vertexCountLength = activeSegmentCount + 1;
            int totalVertices = 2 * vertexCountLength;
            var vertices = new Vector3[totalVertices];
            var uvs = new Vector2[totalVertices];
            var triangles = new int[activeSegmentCount * 6];

            float currentLengthDistance = 0f;
            Vector3 previousCenterPoint = Vector3.Zero;

            int vertexIndex = 0;
            for (int i = 0; i < vertexCountLength; i++)
            {
                int currentStep = startStep + i;
                float globalT = totalLengthSegments > 0 ? (float)currentStep / totalLengthSegments : 0;
                int segmentIndex = Math.Min((int)(globalT * (pointDataArray.Length - 1)), pointDataArray.Length - 2);
                float localT = (globalT * (pointDataArray.Length - 1)) - segmentIndex;
                localT = Math.Clamp(localT, 0f, 1f);

                var p1 = pointDataArray[segmentIndex];
                var p2 = pointDataArray[segmentIndex + 1];

                // Get the coordinate frame for orientation
                GetInterpolatedFrame(p1, p2, localT, out Vector3 centerPos, out Vector3 upDir, out Vector3 rightDir);

                if (i > 0)
                {
                    currentLengthDistance += Vector3.Distance(centerPos, previousCenterPoint);
                }
                previousCenterPoint = centerPos;

                // Calculate the base positions on the road surface using the cross-section interpolation
                Vector3 base1 = CurveProcessor.LerpBetweenTwoCrossSections(p1, p2, startOriginT, localT);
                Vector3 base2 = CurveProcessor.LerpBetweenTwoCrossSections(p1, p2, endOriginT, localT);

                // Apply offsets relative to the interpolated frame
                Vector3 vertex1 = base1 + rightDir * startOffset.X + upDir * startOffset.Y;
                Vector3 vertex2 = base2 + rightDir * endOffset.X + upDir * endOffset.Y;

                vertices[vertexIndex] = vertex1;

                float vCoord;
                if (useDistanceBasedLengthUV)
                {
                    vCoord = currentLengthDistance * uvTile.Y + uvOffset.Y;
                }
                else
                {
                    vCoord = ((float)i / activeSegmentCount) * uvTile.Y + uvOffset.Y;
                }

                float ribbonWidth = 1.0f;
                if (useDistanceBasedWidthUV)
                {
                    ribbonWidth = Vector3.Distance(vertex1, vertex2);
                }

                float u1 = uvOffset.X;
                float u2 = (useDistanceBasedWidthUV ? ribbonWidth : 1.0f) * uvTile.X + uvOffset.X;

                uvs[vertexIndex] = new Vector2(u1, vCoord);
                vertexIndex++;

                vertices[vertexIndex] = vertex2;
                uvs[vertexIndex] = new Vector2(u2, vCoord);
                vertexIndex++;
            }

            int triangleIndex = 0;
            for (int i = 0; i < activeSegmentCount; i++)
            {
                int bottomLeft = i * 2;
                int bottomRight = bottomLeft + 1;
                int topLeft = bottomLeft + 2;
                int topRight = topLeft + 1;

                triangles[triangleIndex++] = bottomLeft;
                triangles[triangleIndex++] = topLeft;
                triangles[triangleIndex++] = bottomRight;

                triangles[triangleIndex++] = bottomRight;
                triangles[triangleIndex++] = topLeft;
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
        /// </summary>
        public static GenericMeshData BuildBridgeMesh(Bridge bridge, Road parentRoad)
        {
            if (bridge == null || parentRoad == null || parentRoad.parentCurve == null || !bridge.Data.Enabled)
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
            List<Vector2> bridgeShapePoints = new List<Vector2>();

            if (bridge.Data.UseTemplate)
            {
                var data = bridge.Data;
                bridgeShapePoints.Add(new Vector2(0, 0));
                bridgeShapePoints.Add(new Vector2(0, data.TemplateCurbHeight));
                bridgeShapePoints.Add(new Vector2(data.TemplateEdgeWidth, data.TemplateCurbHeight));
                bridgeShapePoints.Add(new Vector2(data.TemplateEdgeWidth, data.TemplateCurbHeight - data.TemplateFlangeDepth));
                bridgeShapePoints.Add(new Vector2(data.TemplateEdgeWidth + data.TemplateFlangeWidth, data.TemplateCurbHeight - data.TemplateFlangeDepth));
                bridgeShapePoints.Add(new Vector2(data.TemplateEdgeWidth + data.TemplateFlangeWidth, data.TemplateCurbHeight - data.TemplateFlangeDepth - data.TemplateFlangeHeight));
                bridgeShapePoints.Add(new Vector2(data.TemplateEdgeWidth, data.TemplateCurbHeight - data.TemplateFlangeDepth - data.TemplateFlangeHeight));
                bridgeShapePoints.Add(new Vector2(data.TemplateEdgeWidth, data.TemplateCurbHeight - data.TemplateBridgeHeight));
                bridgeShapePoints.Add(new Vector2(0, data.TemplateCurbHeight - data.TemplateBridgeHeight));
            }
            else
            {
                bridgeShapePoints.AddRange(bridge.Data.BridgeShapePoints.Select(p => (Vector2)p));
            }

            if (bridgeShapePoints.Count < 2)
            {
                return new GenericMeshData();
            }

            int widthSegments = parentRoad.Data.WidthWiseVertexCount - 1;
            int lengthSegmentsPerPoint = Math.Max(1, (int)(widthSegments * parentRoad.Data.LengthWiseVertexCountPerUnitWidthWiseVertexCount));

            var ribbons = new List<GenericMeshData>();
            
            // Left Side: Anchor to origin 0 (Left Edge)
            for (int i = 0; i < bridgeShapePoints.Count - 1; i++)
            {
                // We build from point i+1 to i to ensure correct normal direction (outwards)
                Vector2 p1 = new Vector2(-bridgeShapePoints[i + 1].X, bridgeShapePoints[i + 1].Y);
                Vector2 p2 = new Vector2(-bridgeShapePoints[i].X, bridgeShapePoints[i].Y);
                
                ribbons.Add(BuildRibbon(
                    pointDataArray, 
                    0f, p2, // Start at left edge + offset
                    0f, p1, // End at left edge + offset
                    new Vector2(0, 1), lengthSegmentsPerPoint, bridge.Data.UVTile, bridge.Data.UVOffset, bridge.Data.UseDistanceBasedWidthUV, bridge.Data.UseDistanceBasedLengthUV));
            }

            // Right Side: Anchor to origin 1 (Right Edge)
            for (int i = bridgeShapePoints.Count - 1; i > 0; i--)
            {
                // We build from point i-1 to i to ensure correct normal direction (outwards)
                Vector2 p1 = bridgeShapePoints[i - 1];
                Vector2 p2 = bridgeShapePoints[i];

                ribbons.Add(BuildRibbon(
                    pointDataArray, 
                    1f, p2, // Start at right edge + offset
                    1f, p1, // End at right edge + offset
                    new Vector2(0, 1), lengthSegmentsPerPoint, bridge.Data.UVTile, bridge.Data.UVOffset, bridge.Data.UseDistanceBasedWidthUV, bridge.Data.UseDistanceBasedLengthUV));
            }

            // Bottom Ribbon: Connects the last point of left side to last point of right side
            Vector2 lastPoint = bridgeShapePoints[bridgeShapePoints.Count - 1];
            Vector2 bottomLeft = new Vector2(-lastPoint.X, lastPoint.Y);
            Vector2 bottomRight = new Vector2(lastPoint.X, lastPoint.Y);
            
            ribbons.Add(BuildRibbon(
                pointDataArray, 
                0f, bottomLeft, // Start at left edge + offset
                1f, bottomRight, // End at right edge + offset
                new Vector2(0, 1), lengthSegmentsPerPoint, bridge.Data.UVTile, bridge.Data.UVOffset, bridge.Data.UseDistanceBasedWidthUV, bridge.Data.UseDistanceBasedLengthUV));

            return CombineMeshData(ribbons.ToArray(), bridge.Data.MaterialIndex);
        }


        /// <summary>
        /// Generates the vertex, UV, and triangle data for a railing mesh.
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
            
            int widthSegments = parentRoad.Data.WidthWiseVertexCount - 1;
            int lengthSegmentsPerPoint = Math.Max(1, (int)(widthSegments * parentRoad.Data.LengthWiseVertexCountPerUnitWidthWiseVertexCount));

            // Handle sidedness: generate one or two ribbons based on the Sidedness setting
            switch (railing.Data.Sidedness)
            {
                case RailingSidedness.DoubleSided:
                    // Create two ribbons with opposite winding order
                    GenericMeshData frontFace = BuildRibbon(
                        pointDataArray,
                        railing.Data.HorizontalPosition, Vector2.Zero, // Base: at HorizontalPosition, 0 offset
                        railing.Data.HorizontalPosition, new Vector2(0, railing.Data.RailingHeight), // Top: at HorizontalPosition, height offset
                        new Vector2(railing.Data.Min, railing.Data.Max),
                        lengthSegmentsPerPoint,
                        railing.Data.UVTile,
                        railing.Data.UVOffset,
                        railing.Data.UseDistanceBasedWidthUV,
                        railing.Data.UseDistanceBasedLengthUV);

                    // Back face: swap start and end to reverse winding
                    GenericMeshData backFace = BuildRibbon(
                        pointDataArray,
                        railing.Data.HorizontalPosition, new Vector2(0, railing.Data.RailingHeight), // Top: at HorizontalPosition, height offset
                        railing.Data.HorizontalPosition, Vector2.Zero, // Base: at HorizontalPosition, 0 offset
                        new Vector2(railing.Data.Min, railing.Data.Max),
                        lengthSegmentsPerPoint,
                        railing.Data.UVTile,
                        railing.Data.UVOffset,
                        railing.Data.UseDistanceBasedWidthUV,
                        railing.Data.UseDistanceBasedLengthUV);

                    GenericMeshData combinedMesh = CombineMeshData(new[] { frontFace, backFace }, railing.Data.MaterialIndex);
                    return combinedMesh;

                case RailingSidedness.LeftSided:
                    // Left side facing (counter-clockwise winding from top to bottom)
                    GenericMeshData leftMesh = BuildRibbon(
                        pointDataArray,
                        railing.Data.HorizontalPosition, new Vector2(0, railing.Data.RailingHeight), // Top
                        railing.Data.HorizontalPosition, Vector2.Zero, // Bottom
                        new Vector2(railing.Data.Min, railing.Data.Max),
                        lengthSegmentsPerPoint,
                        railing.Data.UVTile,
                        railing.Data.UVOffset,
                        railing.Data.UseDistanceBasedWidthUV,
                        railing.Data.UseDistanceBasedLengthUV);
                    leftMesh.MaterialID = railing.Data.MaterialIndex;
                    return leftMesh;

                case RailingSidedness.RightSided:
                default:
                    // Right side facing (counter-clockwise winding from bottom to top)
                    GenericMeshData rightMesh = BuildRibbon(
                        pointDataArray,
                        railing.Data.HorizontalPosition, Vector2.Zero, // Bottom
                        railing.Data.HorizontalPosition, new Vector2(0, railing.Data.RailingHeight), // Top
                        new Vector2(railing.Data.Min, railing.Data.Max),
                        lengthSegmentsPerPoint,
                        railing.Data.UVTile,
                        railing.Data.UVOffset,
                        railing.Data.UseDistanceBasedWidthUV,
                        railing.Data.UseDistanceBasedLengthUV);
                    rightMesh.MaterialID = railing.Data.MaterialIndex;
                    return rightMesh;
            }
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

            int totalVertices = 0;
            int totalTriangles = 0;
            foreach (var mesh in meshes)
            {
                if (mesh.Vertices != null && mesh.Vertices.Length > 0)
                {
                    totalVertices += mesh.Vertices.Length;
                    totalTriangles += mesh.Triangles.Length;
                }
            }

            if (totalVertices == 0)
            {
                return new GenericMeshData();
            }

            var combinedVertices = new Vector3[totalVertices];
            var combinedUVs = new Vector2[totalVertices];
            var combinedTriangles = new int[totalTriangles];

            int vertexOffset = 0;
            int triangleOffset = 0;

            foreach (var mesh in meshes)
            {
                if (mesh.Vertices == null || mesh.Vertices.Length == 0) continue;

                Array.Copy(mesh.Vertices, 0, combinedVertices, vertexOffset, mesh.Vertices.Length);
                Array.Copy(mesh.UVs, 0, combinedUVs, vertexOffset, mesh.UVs.Length);

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