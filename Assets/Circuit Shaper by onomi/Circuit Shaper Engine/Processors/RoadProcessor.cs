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

            List<CircuitPoint> pointArray =  road.parentCurve.GetPointsFromSegmentRange(road.Data.startSegmentIndex, road.Data.endSegmentIndex);

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
        /// Generates a bridge ribbon mesh along the road path with vertical/horizontal offsets.
        /// This creates a ribbon that follows the road edge but with offsets defined by the bridge profile.
        /// </summary>
        /// <param name="pointDataArray">Array of circuit points defining the road path</param>
        /// <param name="crossSectionPos">Position on cross-section (0=left edge, 1=right edge)</param>
        /// <param name="offsetStart">2D offset from road surface (X=horizontal out, Y=vertical)</param>
        /// <param name="offsetEnd">2D offset for end of ribbon segment</param>
        /// <param name="lengthSegmentsPerPoint">Number of segments per point pair</param>
        /// <param name="uvTile">UV tiling for texture mapping</param>
        /// <param name="uvOffset">UV offset for texture mapping</param>
        /// <returns>Generic mesh data for the bridge ribbon</returns>
        private static GenericMeshData BuildBridgeRibbon(
            CircuitPointData[] pointDataArray,
            float crossSectionPos,
            Vector2 offsetStart,
            Vector2 offsetEnd,
            int lengthSegmentsPerPoint,
            Vector2 uvTile,
            Vector2 uvOffset)
        {
            if (pointDataArray == null || pointDataArray.Length < 2)
            {
                return new GenericMeshData();
            }

            int totalLengthSegments = (pointDataArray.Length - 1) * lengthSegmentsPerPoint;
            int vertexCountLength = totalLengthSegments + 1;
            int totalVertices = 2 * vertexCountLength;
            
            var vertices = new Vector3[totalVertices];
            var uvs = new Vector2[totalVertices];
            var triangles = new int[totalLengthSegments * 6];

            // Generate vertices and UVs
            int vertexIndex = 0;
            for (int i = 0; i <= totalLengthSegments; i++)
            {
                float globalT = (float)i / totalLengthSegments;
                int segmentIndex = Math.Min((int)(globalT * (pointDataArray.Length - 1)), pointDataArray.Length - 2);
                float localT = (globalT * (pointDataArray.Length - 1)) - segmentIndex;
                localT = Math.Clamp(localT, 0f, 1f);

                var p1 = pointDataArray[segmentIndex];
                var p2 = pointDataArray[segmentIndex + 1];

                // Normalize cross-section curves
                CurveProcessor.NormaliseCurvePoints(p1.CrossSectionCurve);
                CurveProcessor.NormaliseCurvePoints(p2.CrossSectionCurve);

                // Get base position on road edge
                Vector3 basePos = CurveProcessor.LerpBetweenTwoCrossSections(p1, p2, crossSectionPos, localT);

                // Calculate local coordinate system at this point
                // Get tangent
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

                // Get up direction
                Vector3 upDir = Vector3.Normalize(Vector3.Lerp((Vector3)p1.UpDirection, (Vector3)p2.UpDirection, localT));
                upDir = upDir - Vector3.Dot(upDir, curveTangent) * curveTangent;
                if (upDir.LengthSquared() < 1e-6f)
                {
                    upDir = Vector3.UnitY - Vector3.Dot(Vector3.UnitY, curveTangent) * curveTangent;
                }
                upDir = Vector3.Normalize(upDir);

                // Calculate right direction from tangent and up
                Vector3 rightDir = Vector3.Normalize(Vector3.Cross(curveTangent, upDir));

                // Apply start offset (horizontal = right direction, vertical = up direction)
                Vector3 vertex1 = basePos + rightDir * offsetStart.X + upDir * offsetStart.Y;
                vertices[vertexIndex] = vertex1;
                float vCoord = ((float)i / totalLengthSegments) * uvTile.Y + uvOffset.Y;
                uvs[vertexIndex] = new Vector2(uvOffset.X, vCoord);
                vertexIndex++;

                // Apply end offset
                Vector3 vertex2 = basePos + rightDir * offsetEnd.X + upDir * offsetEnd.Y;
                vertices[vertexIndex] = vertex2;
                uvs[vertexIndex] = new Vector2(uvTile.X + uvOffset.X, vCoord);
                vertexIndex++;
            }

            // Generate triangle indices
            int triangleIndex = 0;
            for (int i = 0; i < totalLengthSegments; i++)
            {
                int bottomLeft = i * 2;
                int bottomRight = bottomLeft + 1;
                int topLeft = bottomLeft + 2;
                int topRight = topLeft + 1;

                // First triangle
                triangles[triangleIndex++] = bottomLeft;
                triangles[triangleIndex++] = topLeft;
                triangles[triangleIndex++] = bottomRight;

                // Second triangle
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
        /// Generates a ribbon mesh along the road path between two cross-section coordinates.
        /// A ribbon is a mesh strip that follows the road's path, positioned at specific cross-section coordinates.
        /// </summary>
        /// <param name="pointDataArray">Array of circuit points defining the road path</param>
        /// <param name="crossSectionStart">Normalized start position on cross-section (0-1)</param>
        /// <param name="crossSectionEnd">Normalized end position on cross-section (0-1)</param>
        /// <param name="lengthMin">Normalized start position along road length (0-1)</param>
        /// <param name="lengthMax">Normalized end position along road length (0-1)</param>
        /// <param name="lengthSegmentsPerPoint">Number of segments per point pair</param>
        /// <param name="uvTile">UV tiling for texture mapping</param>
        /// <param name="uvOffset">UV offset for texture mapping</param>
        /// <returns>Generic mesh data for the ribbon</returns>
        private static GenericMeshData BuildRibbon(
            CircuitPointData[] pointDataArray,
            float crossSectionStart,
            float crossSectionEnd,
            float lengthMin,
            float lengthMax,
            int lengthSegmentsPerPoint,
            Vector2 uvTile,
            Vector2 uvOffset)
        {
            if (pointDataArray == null || pointDataArray.Length < 2)
            {
                return new GenericMeshData();
            }

            // Clamp length range
            lengthMin = Math.Clamp(lengthMin, 0f, 1f);
            lengthMax = Math.Clamp(lengthMax, 0f, 1f);
            if (lengthMin >= lengthMax) lengthMax = lengthMin + 0.01f;

            // Calculate total segments and find start/end indices
            int totalLengthSegments = (pointDataArray.Length - 1) * lengthSegmentsPerPoint;
            int startSegmentIndex = (int)(lengthMin * totalLengthSegments);
            int endSegmentIndex = (int)(lengthMax * totalLengthSegments);
            int activeSegmentCount = endSegmentIndex - startSegmentIndex;

            if (activeSegmentCount <= 0)
            {
                return new GenericMeshData();
            }

            // Allocate arrays - ribbon has 2 vertices wide
            int vertexCountLength = activeSegmentCount + 1;
            int totalVertices = 2 * vertexCountLength;
            var vertices = new Vector3[totalVertices];
            var uvs = new Vector2[totalVertices];
            var triangles = new int[activeSegmentCount * 6]; // 2 triangles per segment

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

                // Generate two vertices (start and end of ribbon width)
                vertices[vertexIndex] = CurveProcessor.LerpBetweenTwoCrossSections(p1, p2, crossSectionStart, localT);
                float vCoord = ((float)i / activeSegmentCount) * uvTile.Y + uvOffset.Y;
                uvs[vertexIndex] = new Vector2(uvOffset.X, vCoord);
                vertexIndex++;

                vertices[vertexIndex] = CurveProcessor.LerpBetweenTwoCrossSections(p1, p2, crossSectionEnd, localT);
                uvs[vertexIndex] = new Vector2(uvTile.X + uvOffset.X, vCoord);
                vertexIndex++;
            }

            // Generate triangle indices
            int triangleIndex = 0;
            for (int i = 0; i < activeSegmentCount; i++)
            {
                int bottomLeft = i * 2;
                int bottomRight = bottomLeft + 1;
                int topLeft = bottomLeft + 2;
                int topRight = topLeft + 1;

                // First triangle
                triangles[triangleIndex++] = bottomLeft;
                triangles[triangleIndex++] = topLeft;
                triangles[triangleIndex++] = bottomRight;

                // Second triangle
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
        /// The bridge wraps around the road using a template-based or custom shape.
        /// </summary>
        public static GenericMeshData BuildBridgeMesh(Bridge bridge, Road parentRoad)
        {
            if (bridge == null || parentRoad == null || parentRoad.parentCurve == null)
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

            // Generate bridge shape points from template
            List<Vector2> bridgeShapePoints = new List<Vector2>();

            if (bridge.Data.UseTemplate)
            {
                var data = bridge.Data;
                // Create I-beam style bridge profile (one half, will be mirrored)
                bridgeShapePoints.Add(new Vector2(0, 0)); // Road surface edge
                bridgeShapePoints.Add(new Vector2(0, data.TemplateCurbHeight)); // Curb top
                bridgeShapePoints.Add(new Vector2(-data.TemplateEdgeWidth, data.TemplateCurbHeight)); // Edge width
                bridgeShapePoints.Add(new Vector2(-data.TemplateEdgeWidth, data.TemplateCurbHeight - data.TemplateFlangeDepth)); // Before flange
                bridgeShapePoints.Add(new Vector2(-data.TemplateEdgeWidth - data.TemplateFlangeWidth, data.TemplateCurbHeight - data.TemplateFlangeDepth)); // Flange outer
                bridgeShapePoints.Add(new Vector2(-data.TemplateEdgeWidth - data.TemplateFlangeWidth, data.TemplateCurbHeight - data.TemplateFlangeDepth - data.TemplateFlangeHeight)); // Flange bottom
                bridgeShapePoints.Add(new Vector2(-data.TemplateEdgeWidth, data.TemplateCurbHeight - data.TemplateFlangeDepth - data.TemplateFlangeHeight)); // After flange
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

            if (bridgeShapePoints.Count < 2)
            {
                return new GenericMeshData();
            }

            // Calculate mesh resolution
            int widthSegments = parentRoad.Data.WidthWiseVertexCount - 1;
            int lengthSegmentsPerPoint = Math.Max(1, (int)(widthSegments * parentRoad.Data.LengthWiseVertexCountPerUnitWidthWiseVertexCount));

            // Build bridge mesh using ribbons for each segment of the bridge profile
            // The bridge profile defines a 2D shape that wraps around both edges of the road
            List<GenericMeshData> ribbons = new List<GenericMeshData>();

            // Create ribbons for right side of bridge (from edge going down/out)
            for (int i = 0; i < bridgeShapePoints.Count - 1; i++)
            {
                Vector2 p1 = bridgeShapePoints[i];
                Vector2 p2 = bridgeShapePoints[i + 1];

                // Build a ribbon along the left edge (cross-section position 0)
                GenericMeshData leftRibbon = BuildBridgeRibbon(
                    pointDataArray,
                    0f, // Left edge of road
                    p1, p2,
                    lengthSegmentsPerPoint,
                    bridge.Data.UVTile,
                    Vector2.Zero);

                if (leftRibbon.Vertices != null && leftRibbon.Vertices.Length > 0)
                {
                    ribbons.Add(leftRibbon);
                }
            }

            // Bottom of bridge ribbon (connecting left and right sides)
            Vector2 bottomLeft = bridgeShapePoints[bridgeShapePoints.Count - 1];
            Vector2 bottomRight = new Vector2(-bottomLeft.X, bottomLeft.Y);

            // Create ribbons for right side of bridge (mirrored and reversed)
            for (int i = bridgeShapePoints.Count - 2; i >= 0; i--)
            {
                Vector2 p1 = bridgeShapePoints[i];
                Vector2 p2 = bridgeShapePoints[i - 1];

                // Build a ribbon along the right edge (cross-section position 1), mirroring X offset
                GenericMeshData rightRibbon = BuildBridgeRibbon(
                    pointDataArray,
                    1f, // Right edge of road
                    new Vector2(-p1.X, p1.Y), // Mirror horizontally
                    new Vector2(-p2.X, p2.Y),
                    lengthSegmentsPerPoint,
                    bridge.Data.UVTile,
                    Vector2.Zero);

                if (rightRibbon.Vertices != null && rightRibbon.Vertices.Length > 0)
                {
                    ribbons.Add(rightRibbon);
                }
            }

            // Combine all ribbons into one mesh
            return CombineMeshData(ribbons.ToArray(), bridge.Data.MaterialIndex);
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

            // Railing position on cross-section
            float crossSectionPos = railing.Data.HorizontalPosition;

            // Build the base ribbon at road surface
            GenericMeshData baseMesh = BuildRibbon(
                pointDataArray,
                crossSectionPos,
                crossSectionPos, // Same position creates a line
                railing.Data.Min,
                railing.Data.Max,
                lengthSegmentsPerPoint,
                Vector2.One,
                Vector2.Zero);

            if (baseMesh.Vertices == null || baseMesh.Vertices.Length == 0)
            {
                return new GenericMeshData();
            }

            // Duplicate vertices and offset upward to create railing height
            int vertexCount = baseMesh.Vertices.Length / 2; // Half are bottom, half will be top
            var vertices = new Vector3[baseMesh.Vertices.Length];
            var uvs = new Vector2[baseMesh.Vertices.Length];
            var triangles = new int[(vertexCount - 1) * 6];

            // Copy base vertices (bottom of railing)
            for (int i = 0; i < baseMesh.Vertices.Length; i += 2)
            {
                int baseIdx = i / 2;
                vertices[baseIdx] = baseMesh.Vertices[i];
                uvs[baseIdx] = new Vector2(0, (float)baseIdx / (vertexCount - 1));
            }

            // Create top vertices by offsetting upward
            for (int i = 0; i < vertexCount; i++)
            {
                int segmentIdx = i;
                float globalT = (float)i / (vertexCount - 1);
                int pointIdx = Math.Min((int)(globalT * (pointDataArray.Length - 1)), pointDataArray.Length - 2);
                
                // Get the up direction from the circuit point
                var pointData = pointDataArray[pointIdx];
                Vector3 upDir = Vector3.Normalize((Vector3)pointData.UpDirection);

                vertices[vertexCount + i] = vertices[i] + upDir * railing.Data.RailingHeight;
                uvs[vertexCount + i] = new Vector2(1, (float)i / (vertexCount - 1));
            }

            // Generate triangles
            int triIdx = 0;
            for (int i = 0; i < vertexCount - 1; i++)
            {
                int bl = i;
                int br = vertexCount + i;
                int tl = i + 1;
                int tr = vertexCount + i + 1;

                // First triangle
                triangles[triIdx++] = bl;
                triangles[triIdx++] = tl;
                triangles[triIdx++] = br;

                // Second triangle
                triangles[triIdx++] = br;
                triangles[triIdx++] = tl;
                triangles[triIdx++] = tr;
            }

            return new GenericMeshData
            {
                Vertices = vertices,
                UVs = uvs,
                Triangles = triangles,
                MaterialID = railing.Data.MaterialIndex
            };
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
