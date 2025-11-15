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
            if (road.Data.maxPointIndex == road.Data.minPointIndex)
            {
                UnityEngine.Debug.Log("Invalid point index range for road: " + road + " Range: " + road.Data.minPointIndex + " to " + road.Data.maxPointIndex);
                return new GenericMeshData();
            }


            //Extract points from the parent curve using the range
            UnityEngine.Debug.Log(road + " " + road.parentCurve + " Range: " + road.Data.minPointIndex + " to " + road.Data.maxPointIndex);
            List<CircuitPoint> pointArray = road.parentCurve.GetPointsInRange(road.Data.minPointIndex, road.Data.maxPointIndex);

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

            UnityEngine.Debug.Log("Building road mesh for road with " + pointDataArray.Length + " points.");

            
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
        /// Generates the vertex, UV, and triangle data for a bridge mesh.
        /// </summary>
        public static GenericMeshData BuildBridgeMesh(Bridge bridge)
        {
            // To be implemented.
            return new GenericMeshData();
        }

        /// <summary>
        /// Generates the vertex, UV, and triangle data for a railing mesh.
        /// </summary>
        public static GenericMeshData BuildRailingMesh(Railing railing)
        {
            // To be implemented.
            return new GenericMeshData();
        }
    }
}
