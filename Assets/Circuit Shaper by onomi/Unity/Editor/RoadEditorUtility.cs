using OnomiCircuitShaper.Engine.Data;
using OnomiCircuitShaper.Engine.EditRealm;
using OnomiCircuitShaper.Engine.Interface;
using OnomiCircuitShaper.Engine.Processors;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace OnomiCircuitShaper.Unity.Editor
{
    /// <summary>
    /// Shared utility class containing common editor UI drawing functions for roads, bridges, and railings.
    /// Used by both OnomiCircuitShaperEditor and individual SceneRoad/SceneBridge/SceneRailing editors.
    /// </summary>
    public static class RoadEditorUtility
    {
        /// <summary>
        /// Draws the road settings inspector (UV, mesh resolution, material, physics, segment range).
        /// </summary>
        /// <returns>True if any changes were made.</returns>
        public static bool DrawRoadSettings(Road road, OnomiCircuitShaper shaper, ICircuitShaper circuitShaper, bool showSegmentRange = true)
        {
            if (road == null || road.Data == null) return false;

            bool changed = false;
            var roadData = road.Data;

            // UV Settings
            EditorGUILayout.LabelField("UV Settings", EditorStyles.boldLabel);
            var uvTile = (System.Numerics.Vector2)roadData.UVTile;
            var uvOffset = (System.Numerics.Vector2)roadData.UVOffset;

            UnityEngine.Vector2 tileUV = new UnityEngine.Vector2(uvTile.X, uvTile.Y);
            UnityEngine.Vector2 offsetUV = new UnityEngine.Vector2(uvOffset.X, uvOffset.Y);

            EditorGUI.BeginChangeCheck();
            tileUV = EditorGUILayout.Vector2Field("Tile", tileUV);
            offsetUV = EditorGUILayout.Vector2Field("Offset", offsetUV);
            if (EditorGUI.EndChangeCheck())
            {
                roadData.UVTile = (SerializableVector2)(new System.Numerics.Vector2(tileUV.x, tileUV.y));
                roadData.UVOffset = (SerializableVector2)(new System.Numerics.Vector2(offsetUV.x, offsetUV.y));
                RoadRebuildQueue.MarkDirty(road);
                changed = true;
            }

            EditorGUI.BeginChangeCheck();
            bool useDistanceBasedWidthUV = EditorGUILayout.Toggle("Distance-Based Width UV", roadData.UseDistanceBasedWidthUV);
            bool useDistanceBasedLengthUV = EditorGUILayout.Toggle("Distance-Based Length UV", roadData.UseDistanceBasedLengthUV);
            if (EditorGUI.EndChangeCheck())
            {
                roadData.UseDistanceBasedWidthUV = useDistanceBasedWidthUV;
                roadData.UseDistanceBasedLengthUV = useDistanceBasedLengthUV;
                RoadRebuildQueue.MarkDirty(road);
                changed = true;
            }

            // Mesh Resolution
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Mesh Resolution", EditorStyles.boldLabel);
            
            EditorGUI.BeginChangeCheck();
            int widthWiseVertexCount = EditorGUILayout.IntSlider("Width Vertices", roadData.WidthWiseVertexCount, 2, 50);
            float lengthMult = EditorGUILayout.Slider("Length Density", roadData.LengthWiseVertexCountPerUnitWidthWiseVertexCount, 0.1f, 10f);
            if (EditorGUI.EndChangeCheck())
            {
                roadData.WidthWiseVertexCount = widthWiseVertexCount;
                roadData.LengthWiseVertexCountPerUnitWidthWiseVertexCount = lengthMult;
                RoadRebuildQueue.MarkDirty(road);
                changed = true;
            }

            // Material Selection
            EditorGUILayout.Space();
            bool hasMaterials = shaper != null && shaper.RoadMaterials != null && shaper.RoadMaterials.Count > 0;
            EditorGUI.BeginDisabledGroup(!hasMaterials);
            EditorGUILayout.LabelField("Material", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            int maxIndex = hasMaterials ? Mathf.Max(0, shaper.RoadMaterials.Count - 1) : 0;
            int materialIndex = EditorGUILayout.IntSlider("Material Index", roadData.MaterialIndex, 0, maxIndex);
            if (EditorGUI.EndChangeCheck())
            {
                roadData.MaterialIndex = materialIndex;
                RoadRebuildQueue.MarkDirty(road);
                changed = true;
            }
            EditorGUI.EndDisabledGroup();

            if (!hasMaterials)
            {
                EditorGUILayout.HelpBox("No road materials assigned on target.", MessageType.Info);
            }

            // Layer and Tag
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Unity Scene Settings", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            string roadLayer = EditorGUILayout.TextField("Layer", roadData.Layer ?? "");
            string roadTag = EditorGUILayout.TextField("Tag", roadData.Tag ?? "");
            if (EditorGUI.EndChangeCheck())
            {
                roadData.Layer = roadLayer;
                roadData.Tag = roadTag;
                RoadRebuildQueue.MarkDirty(road);
                changed = true;
            }

            // Collider and Physics Material
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Physics Settings", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            bool enableRoadCollider = EditorGUILayout.Toggle("Enable Collider", roadData.EnableCollider);

            bool hasRoadPhysicsMaterials = shaper != null && shaper.RoadPhysicsMaterials != null && shaper.RoadPhysicsMaterials.Count > 0;
            EditorGUI.BeginDisabledGroup(!hasRoadPhysicsMaterials || !enableRoadCollider);
            int maxRoadPhysMatIndex = hasRoadPhysicsMaterials ? Mathf.Max(0, shaper.RoadPhysicsMaterials.Count - 1) : 0;
            int roadPhysicsMaterialIndex = EditorGUILayout.IntSlider("Physics Material Index", roadData.PhysicsMaterialIndex, 0, maxRoadPhysMatIndex);
            EditorGUI.EndDisabledGroup();

            if (!hasRoadPhysicsMaterials)
            {
                EditorGUILayout.HelpBox("No road physics materials assigned.", MessageType.Info);
            }

            if (EditorGUI.EndChangeCheck())
            {
                roadData.EnableCollider = enableRoadCollider;
                roadData.PhysicsMaterialIndex = roadPhysicsMaterialIndex;
                RoadRebuildQueue.MarkDirty(road);
                changed = true;
            }

            // Segment Range (only for main editor, not individual road editors)
            if (showSegmentRange && circuitShaper != null)
            {
                changed |= DrawSegmentRangeControls(road, circuitShaper);
            }

            return changed;
        }

        /// <summary>
        /// Draws the segment range controls (start/end segment indices).
        /// </summary>
        public static bool DrawSegmentRangeControls(Road road, ICircuitShaper circuitShaper)
        {
            if (road == null || road.parentCurve == null) return false;

            bool changed = false;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Road Point Index Range", EditorStyles.boldLabel);

            int pointCount = road.parentCurve.Points?.Count ?? 0;
            int maxAllowed = Mathf.Max(0, pointCount - 1);

            EditorGUILayout.BeginHorizontal();
            
            // Min Index control
            EditorGUILayout.BeginHorizontal(GUILayout.Width(220));
            GUI.enabled = (pointCount > 0);
            GUILayout.Label("Start Seg", GUILayout.Width(80));
            if (GUILayout.Button("-", GUILayout.Width(24)))
            {
                int v = road.Data.startSegmentIndex - 1;
                if (pointCount > 0 && v < 0) v = maxAllowed;
                if (!circuitShaper.TrySetRoadStartSegment(road, v))
                {
                    UnityEngine.Debug.LogWarning("Cannot decrease start segment: would overlap with another road");
                }
                changed = true;
            }
            EditorGUILayout.LabelField(road.Data.startSegmentIndex.ToString(), GUILayout.Width(30), GUILayout.ExpandWidth(false));
            if (GUILayout.Button("+", GUILayout.Width(24)))
            {
                int v = road.Data.startSegmentIndex + 1;
                if (pointCount > 0 && v > maxAllowed) v = 0;
                if (!circuitShaper.TrySetRoadStartSegment(road, v))
                {
                    UnityEngine.Debug.LogWarning("Cannot increase start segment: would overlap with another road");
                }
                changed = true;
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            // Max Index control
            EditorGUILayout.BeginHorizontal(GUILayout.Width(220));
            GUI.enabled = (pointCount > 0);
            GUILayout.Label("End Seg", GUILayout.Width(80));
            if (GUILayout.Button("-", GUILayout.Width(24)))
            {
                int v = road.Data.endSegmentIndex - 1;
                if (pointCount > 0 && v < 0) v = maxAllowed;
                if (!circuitShaper.TrySetRoadEndSegment(road, v))
                {
                    UnityEngine.Debug.LogWarning("Cannot decrease end segment: would overlap with another road");
                }
                changed = true;
            }
            EditorGUILayout.LabelField(road.Data.endSegmentIndex.ToString(), GUILayout.Width(30), GUILayout.ExpandWidth(false));
            if (GUILayout.Button("+", GUILayout.Width(24)))
            {
                int v = road.Data.endSegmentIndex + 1;
                if (pointCount > 0 && v > maxAllowed) v = 0;
                if (!circuitShaper.TrySetRoadEndSegment(road, v))
                {
                    UnityEngine.Debug.LogWarning("Cannot increase end segment: would overlap with another road");
                }
                changed = true;
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndHorizontal();

            if (pointCount == 0)
            {
                EditorGUILayout.HelpBox("Parent curve has no points.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox($"Segment indices wrap between 0 and {maxAllowed}.", MessageType.None);
            }

            return changed;
        }

        /// <summary>
        /// Draws the bridge inspector UI.
        /// </summary>
        /// <returns>True if any changes were made.</returns>
        public static bool DrawBridgeInspector(Road road, OnomiCircuitShaper shaper, ICircuitShaper circuitShaper)
        {
            if (road == null) return false;

            bool changed = false;
            EditorGUILayout.LabelField("Bridge", EditorStyles.boldLabel);

            bool hasBridge = road.Bridge != null && road.Bridge.Data.Enabled;

            EditorGUI.BeginChangeCheck();
            bool enableBridge = EditorGUILayout.Toggle("Enable Bridge", hasBridge);
            if (EditorGUI.EndChangeCheck())
            {
                circuitShaper?.SetRoadBridgeEnabled(road, enableBridge);
                changed = true;
            }

            if (hasBridge && road.Bridge != null)
            {
                changed |= DrawBridgeSettings(road.Bridge, shaper);
            }

            return changed;
        }

        /// <summary>
        /// Draws bridge settings (material, UV, template, physics).
        /// </summary>
        public static bool DrawBridgeSettings(Bridge bridge, OnomiCircuitShaper shaper)
        {
            if (bridge == null || bridge.Data == null) return false;

            bool changed = false;
            var bridgeData = bridge.Data;

            EditorGUI.indentLevel++;

            // Material Index
            bool hasBridgeMaterials = shaper != null && shaper.BridgeMaterials != null && shaper.BridgeMaterials.Count > 0;
            EditorGUI.BeginDisabledGroup(!hasBridgeMaterials);

            EditorGUI.BeginChangeCheck();
            int maxBridgeMatIndex = hasBridgeMaterials ? Mathf.Max(0, shaper.BridgeMaterials.Count - 1) : 0;
            int bridgeMaterialIndex = EditorGUILayout.IntSlider("Material Index", bridgeData.MaterialIndex, 0, maxBridgeMatIndex);
            if (EditorGUI.EndChangeCheck())
            {
                bridgeData.MaterialIndex = bridgeMaterialIndex;
                RoadRebuildQueue.MarkDirty(bridge.ParentRoad);
                changed = true;
            }

            EditorGUI.EndDisabledGroup();
            if (!hasBridgeMaterials)
            {
                EditorGUILayout.HelpBox("No bridge materials assigned.", MessageType.Info);
            }

            // UV Settings
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("UV Settings", EditorStyles.boldLabel);

            var bUvTile = (System.Numerics.Vector2)bridgeData.UVTile;
            var bUvOffset = (System.Numerics.Vector2)bridgeData.UVOffset;
            UnityEngine.Vector2 bridgeTile = new UnityEngine.Vector2(bUvTile.X, bUvTile.Y);
            UnityEngine.Vector2 bridgeOffset = new UnityEngine.Vector2(bUvOffset.X, bUvOffset.Y);

            EditorGUI.BeginChangeCheck();
            bridgeTile = EditorGUILayout.Vector2Field("Tile", bridgeTile);
            bridgeOffset = EditorGUILayout.Vector2Field("Offset", bridgeOffset);
            if (EditorGUI.EndChangeCheck())
            {
                bridgeData.UVTile = (SerializableVector2)(new System.Numerics.Vector2(bridgeTile.x, bridgeTile.y));
                bridgeData.UVOffset = (SerializableVector2)(new System.Numerics.Vector2(bridgeOffset.x, bridgeOffset.y));
                RoadRebuildQueue.MarkDirty(bridge.ParentRoad);
                changed = true;
            }

            EditorGUI.BeginChangeCheck();
            bool bridgeUseDistanceBasedWidthUV = EditorGUILayout.Toggle("Distance-Based Width UV", bridgeData.UseDistanceBasedWidthUV);
            bool bridgeUseDistanceBasedLengthUV = EditorGUILayout.Toggle("Distance-Based Length UV", bridgeData.UseDistanceBasedLengthUV);
            if (EditorGUI.EndChangeCheck())
            {
                bridgeData.UseDistanceBasedWidthUV = bridgeUseDistanceBasedWidthUV;
                bridgeData.UseDistanceBasedLengthUV = bridgeUseDistanceBasedLengthUV;
                RoadRebuildQueue.MarkDirty(bridge.ParentRoad);
                changed = true;
            }

            // Layer and Tag
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Unity Scene Settings", EditorStyles.label);
            EditorGUI.BeginChangeCheck();
            string bridgeLayer = EditorGUILayout.TextField("Layer", bridgeData.Layer ?? "");
            string bridgeTag = EditorGUILayout.TextField("Tag", bridgeData.Tag ?? "");
            if (EditorGUI.EndChangeCheck())
            {
                bridgeData.Layer = bridgeLayer;
                bridgeData.Tag = bridgeTag;
                RoadRebuildQueue.MarkDirty(bridge.ParentRoad);
                changed = true;
            }

            // Collider and Physics Material
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Physics Settings", EditorStyles.label);
            EditorGUI.BeginChangeCheck();
            bool enableBridgeCollider = EditorGUILayout.Toggle("Enable Collider", bridgeData.EnableCollider);

            bool hasBridgePhysicsMaterials = shaper != null && shaper.BridgePhysicsMaterials != null && shaper.BridgePhysicsMaterials.Count > 0;
            EditorGUI.BeginDisabledGroup(!hasBridgePhysicsMaterials || !enableBridgeCollider);
            int maxBridgePhysMatIndex = hasBridgePhysicsMaterials ? Mathf.Max(0, shaper.BridgePhysicsMaterials.Count - 1) : 0;
            int bridgePhysicsMaterialIndex = EditorGUILayout.IntSlider("Physics Material Index", bridgeData.PhysicsMaterialIndex, 0, maxBridgePhysMatIndex);
            EditorGUI.EndDisabledGroup();

            if (!hasBridgePhysicsMaterials)
            {
                EditorGUILayout.HelpBox("No bridge physics materials assigned.", MessageType.Info);
            }

            if (EditorGUI.EndChangeCheck())
            {
                bridgeData.EnableCollider = enableBridgeCollider;
                bridgeData.PhysicsMaterialIndex = bridgePhysicsMaterialIndex;
                RoadRebuildQueue.MarkDirty(bridge.ParentRoad);
                changed = true;
            }

            // Template settings
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Template Settings", EditorStyles.miniBoldLabel);

            EditorGUI.BeginChangeCheck();
            float edgeWidth = EditorGUILayout.FloatField("Edge Width", bridgeData.TemplateEdgeWidth);
            float bridgeHeight = EditorGUILayout.FloatField("Bridge Height", bridgeData.TemplateBridgeHeight);
            float flangeWidth = EditorGUILayout.FloatField("Flange Width", bridgeData.TemplateFlangeWidth);
            float flangeHeight = EditorGUILayout.FloatField("Flange Height", bridgeData.TemplateFlangeHeight);
            float flangeDepth = EditorGUILayout.FloatField("Flange Depth", bridgeData.TemplateFlangeDepth);
            float curbHeight = EditorGUILayout.FloatField("Curb Height", bridgeData.TemplateCurbHeight);

            if (EditorGUI.EndChangeCheck())
            {
                bridgeData.TemplateEdgeWidth = edgeWidth;
                bridgeData.TemplateBridgeHeight = bridgeHeight;
                bridgeData.TemplateFlangeWidth = flangeWidth;
                bridgeData.TemplateFlangeHeight = flangeHeight;
                bridgeData.TemplateFlangeDepth = flangeDepth;
                bridgeData.TemplateCurbHeight = curbHeight;
                RoadRebuildQueue.MarkDirty(bridge.ParentRoad);
                changed = true;
            }

            EditorGUI.indentLevel--;

            return changed;
        }

        /// <summary>
        /// Draws the railings inspector UI for a road.
        /// </summary>
        /// <returns>True if any changes were made.</returns>
        public static bool DrawRailingsInspector(Road road, OnomiCircuitShaper shaper, ICircuitShaper circuitShaper)
        {
            if (road == null) return false;

            bool changed = false;
            EditorGUILayout.LabelField("Railings", EditorStyles.boldLabel);

            if (road.Railings == null)
            {
                road.Data.Railings = new List<RailingData>();
            }

            int railingCount = road.Railings.Count;
            EditorGUILayout.LabelField($"Count: {railingCount}");

            // Add/Remove buttons
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Railing", GUILayout.Height(25)))
            {
                circuitShaper?.AddRailingToRoad(road);
                changed = true;
            }

            EditorGUI.BeginDisabledGroup(railingCount == 0);
            if (GUILayout.Button("Remove Last", GUILayout.Height(25)))
            {
                if (railingCount > 0)
                {
                    circuitShaper?.RemoveRailingFromRoad(road, railingCount - 1);
                    changed = true;
                }
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            // Draw each railing
            for (int i = 0; i < road.Railings.Count; i++)
            {
                EditorGUILayout.Space();
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                EditorGUILayout.LabelField($"Railing {i}", EditorStyles.boldLabel);

                if (DrawRailingSettings(road.Railings[i], road, shaper, i))
                {
                    changed = true;
                }

                // Delete button
                GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
                if (GUILayout.Button($"Delete Railing {i}", GUILayout.Height(20)))
                {
                    circuitShaper?.RemoveRailingFromRoad(road, i);
                    changed = true;
                    EditorGUILayout.EndVertical();
                    break; // Exit loop since we modified the list
                }
                GUI.backgroundColor = Color.white;

                EditorGUILayout.EndVertical();
            }

            return changed;
        }

        /// <summary>
        /// Draws settings for a single railing.
        /// </summary>
        public static bool DrawRailingSettings(Railing railing, Road parentRoad, OnomiCircuitShaper shaper, int railingIndex = -1)
        {
            if (railing == null || railing.Data == null) return false;

            bool changed = false;
            var railingData = railing.Data;

            EditorGUI.indentLevel++;

            EditorGUI.BeginChangeCheck();

            // Visibility
            bool isVisible = EditorGUILayout.Toggle("Visible", railingData.IsVisible);

            // Material
            bool hasRailingMaterials = shaper != null && shaper.RailingMaterials != null && shaper.RailingMaterials.Count > 0;
            EditorGUI.BeginDisabledGroup(!hasRailingMaterials);
            int maxRailingMatIndex = hasRailingMaterials ? Mathf.Max(0, shaper.RailingMaterials.Count - 1) : 0;
            int railingMaterialIndex = EditorGUILayout.IntSlider("Material Index", railingData.MaterialIndex, 0, maxRailingMatIndex);
            EditorGUI.EndDisabledGroup();

            if (!hasRailingMaterials)
            {
                EditorGUILayout.HelpBox("No railing materials assigned.", MessageType.Info);
            }

            // Properties
            float railingHeight = EditorGUILayout.FloatField("Height", railingData.RailingHeight);
            float min = EditorGUILayout.Slider("Min (Length)", railingData.Min, 0f, 1f);
            float max = EditorGUILayout.Slider("Max (Length)", railingData.Max, 0f, 1f);
            float horizontalPos = EditorGUILayout.Slider("Horizontal Position", railingData.HorizontalPosition, 0f, 1f);

            // UV Settings
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("UV Settings", EditorStyles.label);
            var rUvTile = (System.Numerics.Vector2)railingData.UVTile;
            var rUvOffset = (System.Numerics.Vector2)railingData.UVOffset;
            UnityEngine.Vector2 railingTile = new UnityEngine.Vector2(rUvTile.X, rUvTile.Y);
            UnityEngine.Vector2 railingOffset = new UnityEngine.Vector2(rUvOffset.X, rUvOffset.Y);

            railingTile = EditorGUILayout.Vector2Field("Tile", railingTile);
            railingOffset = EditorGUILayout.Vector2Field("Offset", railingOffset);

            bool railingUseDistanceBasedWidthUV = EditorGUILayout.Toggle("Distance-Based Width UV", railingData.UseDistanceBasedWidthUV);
            bool railingUseDistanceBasedLengthUV = EditorGUILayout.Toggle("Distance-Based Length UV", railingData.UseDistanceBasedLengthUV);

            // Sidedness
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Collision Settings", EditorStyles.label);
            RailingSidedness sidedness = (RailingSidedness)EditorGUILayout.EnumPopup("Sidedness", railingData.Sidedness);

            // Layer and Tag
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Unity Scene Settings", EditorStyles.label);
            string railingLayer = EditorGUILayout.TextField("Layer", railingData.Layer ?? "");
            string railingTag = EditorGUILayout.TextField("Tag", railingData.Tag ?? "");

            // Collider and Physics Material
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Rendering & Physics Settings", EditorStyles.label);
            bool enableMeshRenderer = EditorGUILayout.Toggle("Enable Mesh Renderer", railingData.EnableMeshRenderer);
            bool enableRailingCollider = EditorGUILayout.Toggle("Enable Collider", railingData.EnableCollider);

            bool hasRailingPhysicsMaterials = shaper != null && shaper.RailingPhysicsMaterials != null && shaper.RailingPhysicsMaterials.Count > 0;
            EditorGUI.BeginDisabledGroup(!hasRailingPhysicsMaterials || !enableRailingCollider);
            int maxRailingPhysMatIndex = hasRailingPhysicsMaterials ? Mathf.Max(0, shaper.RailingPhysicsMaterials.Count - 1) : 0;
            int railingPhysicsMaterialIndex = EditorGUILayout.IntSlider("Physics Material Index", railingData.PhysicsMaterialIndex, 0, maxRailingPhysMatIndex);
            EditorGUI.EndDisabledGroup();

            if (!hasRailingPhysicsMaterials)
            {
                EditorGUILayout.HelpBox("No railing physics materials assigned.", MessageType.Info);
            }

            if (EditorGUI.EndChangeCheck())
            {
                railingData.IsVisible = isVisible;
                railingData.MaterialIndex = railingMaterialIndex;
                railingData.RailingHeight = railingHeight;
                railingData.Min = Mathf.Min(min, max - 0.01f);
                railingData.Max = Mathf.Max(max, min + 0.01f);
                railingData.HorizontalPosition = horizontalPos;
                railingData.UVTile = (SerializableVector2)(new System.Numerics.Vector2(railingTile.x, railingTile.y));
                railingData.UVOffset = (SerializableVector2)(new System.Numerics.Vector2(railingOffset.x, railingOffset.y));
                railingData.UseDistanceBasedWidthUV = railingUseDistanceBasedWidthUV;
                railingData.UseDistanceBasedLengthUV = railingUseDistanceBasedLengthUV;
                railingData.Sidedness = sidedness;
                railingData.Layer = railingLayer;
                railingData.Tag = railingTag;
                railingData.EnableMeshRenderer = enableMeshRenderer;
                railingData.EnableCollider = enableRailingCollider;
                railingData.PhysicsMaterialIndex = railingPhysicsMaterialIndex;
                
                if (parentRoad != null)
                {
                    RoadRebuildQueue.MarkDirty(parentRoad);
                }
                changed = true;
            }

            EditorGUI.indentLevel--;

            return changed;
        }

        /// <summary>
        /// Rebuilds a single road's mesh and updates its SceneRoad.
        /// </summary>
        public static void RebuildRoad(Road road, SceneRoad sceneRoad, OnomiCircuitShaper shaper)
        {
            if (road == null || sceneRoad == null) return;

            // Generate road mesh
            var meshData = RoadProcessor.BuildRoadMesh(road);

            if (meshData.Vertices == null || meshData.Vertices.Length == 0) return;

            // Convert to Unity types
            UnityEngine.Vector3[] vertices = new UnityEngine.Vector3[meshData.Vertices.Length];
            for (int i = 0; i < meshData.Vertices.Length; i++)
            {
                var v = meshData.Vertices[i];
                vertices[i] = new UnityEngine.Vector3(v.X, v.Y, v.Z);
            }

            UnityEngine.Vector2[] uvs = new UnityEngine.Vector2[meshData.UVs.Length];
            for (int i = 0; i < meshData.UVs.Length; i++)
            {
                var uv = meshData.UVs[i];
                uvs[i] = new UnityEngine.Vector2(uv.X, uv.Y);
            }

            sceneRoad.UpdateMesh(vertices, uvs, meshData.Triangles, meshData.MaterialID);

            // Handle bridge
            GenericMeshData bridgeMesh = new GenericMeshData();
            if (road.Bridge != null && road.Bridge.Data.Enabled)
            {
                bridgeMesh = RoadProcessor.BuildBridgeMesh(road.Bridge, road);
            }
            int bridgeMatID = (road.Bridge != null) ? road.Bridge.Data.MaterialIndex : 0;
            sceneRoad.UpdateBridge(bridgeMesh, road.Bridge, bridgeMatID);

            // Handle railings
            var railingUpdates = new List<(GenericMeshData, Railing)>();
            if (road.Railings != null)
            {
                foreach (var railing in road.Railings)
                {
                    var railingMesh = RoadProcessor.BuildRailingMesh(railing, road);
                    railingUpdates.Add((railingMesh, railing));
                }
            }
            sceneRoad.UpdateRailings(railingUpdates);
        }
    }
}
