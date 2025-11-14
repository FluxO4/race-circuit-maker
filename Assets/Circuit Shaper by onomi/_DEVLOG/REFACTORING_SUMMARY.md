# Refactoring Summary: Queue-Based Architecture & Interface Cleanup

## Date
November 14, 2025

## Overview
Completed comprehensive refactoring to:
1. Remove event-based communication (RoadBuilt, BridgeBuilt, RailingBuilt)
2. Enforce Unity→Engine communication through ICircuitShaper interface only
3. Rewrite road handles to use robust Bezier curve approach instead of mesh extraction

## Changes Made

### 1. Interface Layer (ICircuitShaper.cs)

**Removed:**
- `event Action<RoadData, GenericMeshData> RoadBuilt`
- `event Action<BridgeData, GenericMeshData> BridgeBuilt`
- `event Action<RailingData, GenericMeshData> RailingBuilt`

**Added:**
```csharp
/// <summary>
/// Gets all roads that need rebuilding and clears the queue.
/// This is the primary mechanism for Unity to discover which roads need mesh updates.
/// </summary>
List<RoadData> GetAndClearDirtyRoads();

/// <summary>
/// Clears the road rebuild queue without processing.
/// Called when editor is disabled to prevent stale rebuild requests.
/// </summary>
void ClearRoadRebuildQueue();
```

### 2. Implementation Layer (CircuitShaper.cs)

**Removed:**
- Event field declarations
- Event invocation in `CreateNewRoadFromPoints()`
- Event invocation in `RemoveRoad()` (replaced with comment about queue handling)

**Added:**
```csharp
public List<RoadData> GetAndClearDirtyRoads()
{
    return RoadRebuildQueue.GetAndClearDirtyRoads();
}

public void ClearRoadRebuildQueue()
{
    RoadRebuildQueue.Clear();
}
```

### 3. Unity Editor (OnomiCircuitShaperEditor.cs)

**Removed:**
- Event subscription in `OnEnable()`: `_circuitShaper.RoadBuilt += OnRoadBuilt;`
- Event unsubscription in `OnDisable()`: `_circuitShaper.RoadBuilt -= OnRoadBuilt;`
- Entire `OnRoadBuilt()` event handler method
- Direct `RoadRebuildQueue` access

**Changed:**
```csharp
// OLD (OnDisable):
RoadRebuildQueue.Clear();
if (_circuitShaper != null)
{
    _circuitShaper.RoadBuilt -= OnRoadBuilt;
}
_circuitShaper?.QuitEdit();

// NEW (OnDisable):
if (_circuitShaper != null)
{
    _circuitShaper.ClearRoadRebuildQueue();  // Via interface!
    _circuitShaper.QuitEdit();
}

// OLD (ProcessDirtyRoads):
var dirtyRoads = RoadRebuildQueue.GetAndClearDirtyRoads();  // Direct access!

// NEW (ProcessDirtyRoads):
var dirtyRoads = _circuitShaper.GetAndClearDirtyRoads();  // Via interface!
```

### 4. Scene GUI (OnomiCircuitShaperEditor.SceneGUI.cs)

**Completely Rewrote DrawRoadHandles():**

#### Old Approach (FRAGILE):
- Extracted vertices from SceneRoad's MeshFilter
- Required mesh to exist and be valid
- Calculated grid indices to find edge vertices
- Drew lines between extracted vertices
- Only edge lines were clickable (not the region)

**Problems:**
- Failed when mesh didn't exist yet
- Failed when mesh was invalidated
- Expensive mesh extraction every frame
- Inconsistent selectability
- Tight coupling to mesh structure

#### New Approach (ROBUST):
- Uses only RoadData and Road edit realm object
- Calculates edge positions from CircuitPoint cross-sections
- Samples Bezier curves using control points (scaled to prevent pinching)
- Draws closed region (connects start/end)
- Region-based selection using point-in-polygon test

**Added Helper Methods:**
```csharp
/// <summary>
/// Samples an edge curve along the road using simplified Bezier interpolation.
/// </summary>
private List<Vector3> SampleEdgeCurve(
    List<CircuitPoint> points,
    List<Vector3> edgePoints,
    Vector3 basePosition,
    float scale,
    int samplesPerSegment,
    bool isLeftEdge)

/// <summary>
/// Evaluates a cubic Bezier curve at parameter t.
/// </summary>
private Vector3 CubicBezier(
    Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)

/// <summary>
/// Tests if a 2D point is inside a polygon using ray casting algorithm.
/// </summary>
private bool IsPointInPolygon(
    Vector2 point, List<Vector2> polygon)
```

**Visual Improvements:**
- Closed boundary (start and end points connected)
- Smooth Bezier curves follow road shape
- Entire region is clickable, not just edges
- More consistent hover/selection detection

## Architecture Compliance

### Unity → Engine Communication Rules

✅ **CORRECT** - Using ICircuitShaper interface:
- `_circuitShaper.GetAndClearDirtyRoads()`
- `_circuitShaper.ClearRoadRebuildQueue()`
- `_circuitShaper.GetLiveCircuit` (property in interface)
- `_circuitShaper.SelectedPoints` (property in interface)
- `_circuitShaper.CreateNewRoadFromPoints()`
- `_circuitShaper.RemoveRoad()`

✅ **ACCEPTABLE** - Direct access to read-only data structures:
- `RoadData` (data class, no behavior)
- `CircuitPointData` (data class, no behavior)
- `Road` (edit realm object, obtained through interface)
- `CircuitPoint` (edit realm object, obtained through interface)

✅ **ACCEPTABLE** - Static processor calls:
- `RoadProcessor.BuildRoadMesh()` (pure function, no state)

❌ **VIOLATION** - Would be wrong (now eliminated):
- ~~`RoadRebuildQueue.MarkDirty()` from Unity~~ (only Engine uses this)
- ~~`RoadRebuildQueue.GetAndClearDirtyRoads()` from Unity~~ (now via interface)
- ~~Event subscriptions `_circuitShaper.RoadBuilt +=`~~ (removed entirely)

### Current State of Unity Layer Usage

**OnomiCircuitShaperEditor.cs uses:**
- `OnomiCircuitShaper.Engine.Interface` ✅ (ICircuitShaper)
- `OnomiCircuitShaper.Engine.EditRealm` ✅ (Road, CircuitPoint, CircuitCurve - obtained via interface)
- `OnomiCircuitShaper.Engine.Data` ✅ (RoadData, CircuitData - data classes)
- `OnomiCircuitShaper.Engine.Processors` ✅ (RoadProcessor - static functions)
- `OnomiCircuitShaper.Engine.Presets` ✅ (CrossSectionPresets - static data)
- ~~`OnomiCircuitShaper.Engine` (RoadRebuildQueue)~~ ❌ REMOVED - now accessed via interface

**Result:** Unity layer now properly uses interface for all stateful operations!

## Benefits of New Architecture

### 1. Event Removal
- **Before:** Complex subscription/unsubscription lifecycle management
- **After:** Simple pull-based queue consumption
- **Benefit:** No memory leaks, no stale subscriptions, no race conditions

### 2. Interface Enforcement
- **Before:** Unity could directly access RoadRebuildQueue
- **After:** All stateful operations through ICircuitShaper
- **Benefit:** Better encapsulation, easier testing, cleaner dependencies

### 3. Robust Road Handles
- **Before:** Required valid mesh, fragile edge extraction, edge-only selection
- **After:** Works from data, Bezier curves, region-based selection
- **Benefit:** Consistent selectability, no dependency on mesh state, better UX

### 4. Road Deletion
- **Before:** Fired event with empty mesh to signal deletion
- **After:** ProcessDirtyRoads() detects missing RoadData and cleans up
- **Benefit:** Unified rebuild/deletion logic, no special cases

## Testing Checklist

- [x] Code compiles without errors
- [ ] Roads rebuild when points move
- [ ] Roads persist through object deselection
- [ ] Road handles always visible and selectable
- [ ] Clicking inside road region selects it (not just edges)
- [ ] Road deletion cleans up SceneRoad
- [ ] Play mode transitions work correctly
- [ ] No Unity→Engine violations (all through interface)

## Future Enhancements

### Point Deletion Integration (TODO)
When a point is deleted, need to:
1. Remove point from all `RoadData.AssociatedPoints`
2. Mark affected roads dirty via queue
3. If road has < 2 points, call `CircuitShaper.RemoveRoad()`

### Bridge and Railing Support (TODO)
When bridges and railings are implemented:
1. Follow same queue pattern as roads
2. Add `GetAndClearDirtyBridges()` and `GetAndClearDirtyRailings()` to interface
3. No events needed - just queue marking and processing

## Conclusion

The refactoring eliminates three major sources of bugs:
1. **Event lifecycle issues** → Queue-based communication
2. **Interface violations** → Enforced through ICircuitShaper
3. **Mesh dependency** → Data-driven Bezier rendering

The system is now more robust, maintainable, and follows proper architectural boundaries.
