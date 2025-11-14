```markdown
# Road Selection and Editing System Design

## Problem Statement
Currently, roads are created as GameObjects in the hierarchy, which causes issues:
- Selecting the SceneRoad GameObject exits the Circuit Shaper editor context
- Edit realm objects get destroyed when focus shifts away
- No unified interface for road properties (materials, bridges, railings, etc.)
- Difficult to maintain data synchronization

## Proposed Solution: In-Editor Road Selection

### Visual Representation
Instead of relying on GameObject selection, implement a visual selection system within the Scene View:

**Option 1: Edge Lines (Recommended)**
- Draw two colored lines along the left and right edges of the road
- When mouse hovers over the road area between these lines, highlight both edges
- Click to select the road (similar to how points are selected)
- Selected roads show with different colored edges (e.g., yellow instead of gray)

**Option 2: Center Spline**
- Draw a single line down the center of the road
- Clickable like a curve
- Less visually intrusive but harder to see on complex tracks

**Option 3: Wireframe Overlay**
- Draw a simplified wireframe of the road mesh
- Only visible when OnomiCircuitShaper is selected
- Most accurate but potentially cluttered

### Selection Implementation

```csharp
// In OnomiCircuitShaperEditor.cs

private Road _selectedRoad = null;

private void DrawRoadHandles(OnomiCircuitShaper target)
{
    if (_circuitShaper?.GetLiveCircuit?.Roads == null) return;
    
    foreach (var road in _circuitShaper.GetLiveCircuit.Roads)
    {
        // Skip roads with no points
        if (road.AssociatedPoints == null || road.AssociatedPoints.Count < 2) continue;
        
        // Determine edge colors
        Color edgeColor = (road == _selectedRoad) ? Color.yellow : new Color(0.5f, 0.5f, 0.5f, 0.5f);
        
        // Draw left and right edge lines
        DrawRoadEdges(road, edgeColor, target);
        
        // Handle selection
        if (IsMouseOverRoad(road, Event.current.mousePosition))
        {
            // Highlight on hover
            DrawRoadEdges(road, Color.white, target);
            
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
            {
                _selectedRoad = road;
                _circuitShaper.ClearSelection(); // Clear point selection
                Event.current.Use();
            }
        }
    }
}

private void DrawRoadEdges(Road road, Color color, OnomiCircuitShaper target)
{
    Handles.color = color;
    
    // Sample points along the road and calculate edge positions
    List<Vector3> leftEdge = new List<Vector3>();
    List<Vector3> rightEdge = new List<Vector3>();
    
    int sampleCount = road.AssociatedPoints.Count * 10; // 10 samples per segment
    
    for (int i = 0; i < sampleCount; i++)
    {
        float t = (float)i / (sampleCount - 1);
        
        // Calculate position along road
        // Use the same logic as mesh generation but just get edge vertices
        // (This is a simplified version - full implementation would interpolate properly)
        
        int segmentIndex = (int)(t * (road.AssociatedPoints.Count - 1));
        segmentIndex = Mathf.Clamp(segmentIndex, 0, road.AssociatedPoints.Count - 2);
        float localT = (t * (road.AssociatedPoints.Count - 1)) - segmentIndex;
        
        var p1 = road.AssociatedPoints[segmentIndex].Data;
        var p2 = road.AssociatedPoints[segmentIndex + 1].Data;
        
        // Get left edge (widthT = 0) and right edge (widthT = 1)
        Vector3 leftPos = CurveProcessor.LerpBetweenTwoCrossSections(p1, p2, 0f, localT);
        Vector3 rightPos = CurveProcessor.LerpBetweenTwoCrossSections(p1, p2, 1f, localT);
        
        leftEdge.Add(leftPos.ToGlobalSpace(target.transform.position, target.Data.settingsData.ScaleMultiplier));
        rightEdge.Add(rightPos.ToGlobalSpace(target.transform.position, target.Data.settingsData.ScaleMultiplier));
    }
    
    // Draw the edge polylines
    Handles.DrawPolyLine(leftEdge.ToArray());
    Handles.DrawPolyLine(rightEdge.ToArray());
}

private bool IsMouseOverRoad(Road road, Vector2 mousePosition)
{
    // Cast a ray and check if it's within the road bounds
    // Implementation would check distance to road surface
    // This is a simplified check - full version would be more sophisticated
    return false; // Placeholder
}
```

### Inspector UI for Selected Road

```csharp
private void DrawSelectedRoadInspector()
{
    if (_selectedRoad == null) return;
    
    EditorGUILayout.Space();
    EditorGUILayout.LabelField("Selected Road", EditorStyles.boldLabel);
    
    // Basic Properties
    EditorGUILayout.LabelField($"Points: {_selectedRoad.AssociatedPoints.Count}");
    
    // Mesh Resolution
    EditorGUI.BeginChangeCheck();
    int widthVerts = EditorGUILayout.IntSlider("Width Vertex Count", _selectedRoad.Data.WidthWiseVertexCount, 2, 50);
    float lengthMult = EditorGUILayout.Slider("Length Multiplier", _selectedRoad.Data.LengthWiseVertexCountPerUnitWidthWiseVertexCount, 0.1f, 5f);
    if (EditorGUI.EndChangeCheck())
    {
        _selectedRoad.Data.WidthWiseVertexCount = widthVerts;
        _selectedRoad.Data.LengthWiseVertexCountPerUnitWidthWiseVertexCount = lengthMult;
        _selectedRoad.MarkDirty();
    }
    
    // UV Settings
    EditorGUILayout.LabelField("UV Mapping", EditorStyles.boldLabel);
    EditorGUI.BeginChangeCheck();
    Vector2 uvTile = EditorGUILayout.Vector2Field("UV Tile", new Vector2(_selectedRoad.Data.UVTile.x, _selectedRoad.Data.UVTile.y));
    Vector2 uvOffset = EditorGUILayout.Vector2Field("UV Offset", new Vector2(_selectedRoad.Data.UVOffset.x, _selectedRoad.Data.UVOffset.y));
    if (EditorGUI.EndChangeCheck())
    {
        _selectedRoad.Data.UVTile = new System.Numerics.Vector2(uvTile.x, uvTile.y);
        _selectedRoad.Data.UVOffset = new System.Numerics.Vector2(uvOffset.x, uvOffset.y);
        _selectedRoad.MarkDirty();
    }
    
    // Material Assignment
    EditorGUILayout.Space();
    if (_target.SceneRoads.TryGetValue(_selectedRoad.Data, out SceneRoad sceneRoad))
    {
        var renderer = sceneRoad.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            EditorGUI.BeginChangeCheck();
            Material newMat = (Material)EditorGUILayout.ObjectField("Material", renderer.sharedMaterial, typeof(Material), false);
            if (EditorGUI.EndChangeCheck())
            {
                renderer.sharedMaterial = newMat;
            }
        }
    }
    
    // Railings Section
    EditorGUILayout.Space();
    EditorGUILayout.LabelField("Railings", EditorStyles.boldLabel);
    if (GUILayout.Button("Add Railing"))
    {
        // Add railing logic
    }
    
    foreach (var railing in _selectedRoad.Railings)
    {
        DrawRailingInspector(railing);
    }
    
    // Bridge Section
    EditorGUILayout.Space();
    EditorGUILayout.LabelField("Bridge", EditorStyles.boldLabel);
    bool hasBridge = _selectedRoad.Bridge != null;
    bool newHasBridge = EditorGUILayout.Toggle("Has Bridge", hasBridge);
    if (newHasBridge != hasBridge)
    {
        if (newHasBridge)
        {
            // Create bridge
            _selectedRoad.Data.Bridge = new BridgeData();
            _selectedRoad.Bridge = new Bridge(_selectedRoad.Data.Bridge);
        }
        else
        {
            // Remove bridge
            _selectedRoad.Data.Bridge = null;
            _selectedRoad.Bridge = null;
        }
        _selectedRoad.MarkDirty();
    }
    
    if (_selectedRoad.Bridge != null)
    {
        DrawBridgeInspector(_selectedRoad.Bridge);
    }
    
    // Delete Road Button
    EditorGUILayout.Space();
    GUI.backgroundColor = Color.red;
    if (GUILayout.Button("Delete Road"))
    {
        if (EditorUtility.DisplayDialog("Delete Road", "Are you sure you want to delete this road?", "Delete", "Cancel"))
        {
            _circuitShaper.RemoveRoad(_selectedRoad);
            _selectedRoad = null;
        }
    }
    GUI.backgroundColor = Color.white;
}

private void DrawRailingInspector(Railing railing)
{
    // Railing properties UI
}

private void DrawBridgeInspector(Bridge bridge)
{
    // Bridge properties UI
}
```

### Integration Points

1. **In OnInspectorGUI()**:
```csharp
// After existing selection handling
if (_selectedRoad != null)
{
    DrawSelectedRoadInspector();
}
```

2. **In OnSceneGUI()**:
```csharp
// After drawing point handles
if (!_isEditingCrossSection)
{
    DrawRoadHandles(_target);
}
```

3. **Clear road selection when selecting points**:
```csharp
public void SelectPoint(CircuitPoint point)
{
    _selectedRoad = null; // Clear road selection
    // ... existing code
}
```

## Benefits of This Approach

1. **Unified Interface**: Everything happens within the Circuit Shaper editor
2. **No Hierarchy Conflicts**: GameObjects remain hidden/internal
3. **Persistent Selection**: Road stays selected even as you edit other parts
4. **Direct Property Access**: Immediate access to all road, railing, and bridge properties
5. **Visual Feedback**: Clear indication of which road is selected
6. **Consistent UX**: Similar interaction pattern to point/curve selection

## Implementation Priority

1. **Phase 1** (Immediate): Fix mesh generation bugs with debug logging
2. **Phase 2**: Implement basic road edge visualization
3. **Phase 3**: Add road selection capability
4. **Phase 4**: Build out inspector UI for road properties
5. **Phase 5**: Add railing and bridge editing interfaces
