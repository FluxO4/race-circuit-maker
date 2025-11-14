# Queue-Based Road Rebuild Architecture

## Overview

This document describes the robust queue-based road rebuild system that replaced the fragile event-based approach.

## Problem Statement

The original event-based system had multiple failure modes:
1. **Event Subscriptions Lost**: Roads lost subscriptions when edit realm was recreated
2. **Random Selection Failures**: Road selection became invalid after realm recreation
3. **Play Mode Issues**: Roads disappeared and duplicated during play mode transitions
4. **Point Deletion Crashes**: Roads didn't handle missing points gracefully
5. **Race Conditions**: Multiple simultaneous events caused inconsistent states
6. **Maintenance Complexity**: Subscription management spread across multiple layers

## Solution: Queue-Based Architecture

### Core Design

Instead of events, we use a **static communication channel** via `RoadRebuildQueue`:

```
CircuitPoint Movement → Queue.MarkDirty(road) → Editor pulls from queue → Rebuild mesh
```

### Key Components

#### 1. RoadRebuildQueue (Engine Layer)
**File**: `Assets/Circuit Shaper by onomi/Circuit Shaper Engine/RoadRebuildQueue.cs`

Thread-safe static class that acts as a communication channel:

```csharp
public static class RoadRebuildQueue
{
    private static HashSet<RoadData> _dirtyRoads = new HashSet<RoadData>();
    private static readonly object _lock = new object();
    
    public static void MarkDirty(RoadData roadData);
    public static List<RoadData> GetAndClearDirtyRoads();
    public static void Clear();
    public static bool HasDirtyRoads();
    public static int DirtyRoadCount { get; }
}
```

**Design Rationale**:
- HashSet prevents duplicates (multiple points moving doesn't duplicate work)
- Lock-based thread safety for editor multithreading
- Atomic GetAndClearDirtyRoads() prevents race conditions
- Static lifetime survives edit realm recreation

#### 2. CircuitPoint Association Tracking (Edit Objects Layer)
**File**: `Assets/Circuit Shaper by onomi/Circuit Shaper Engine/Edit Objects/CircuitPoint.cs`

Each point tracks which roads use it:

```csharp
public List<RoadData> AssociatedRoads { get; private set; }

public void AddRoadAssociation(RoadData roadData);
public void RemoveRoadAssociation(RoadData roadData);
private void MarkAssociatedRoadsDirty();
```

All movement methods call `MarkAssociatedRoadsDirty()`:
- `MoveCircuitPoint()`
- `MoveForwardControlPoint()`
- `MoveBackwardControlPoint()`
- `RotateCircuitPoint()`

#### 3. Circuit Reconnection Logic (Edit Objects Layer)
**File**: `Assets/Circuit Shaper by onomi/Circuit Shaper Engine/Edit Objects/Circuit.cs`

Rebuilds associations when edit realm is recreated:

```csharp
public void ReconnectRoadsToPoints()
{
    // Clear all existing associations
    foreach (var point in AllPoints)
        point.AssociatedRoads.Clear();
    
    // Rebuild bidirectional associations
    foreach (var road in Roads)
        foreach (var pointData in road.Data.AssociatedPoints)
            livePoint.AddRoadAssociation(road.Data);
    
    // Mark all roads dirty for rebuild
    RoadRebuildQueue.MarkDirty(road.Data);
}
```

Called by `CircuitShaper.BeginEdit()` after edit realm creation.

#### 4. Editor Queue Processing (Unity Layer)
**File**: `Assets/Circuit Shaper by onomi/Unity/Editor/OnomiCircuitShaperEditor.cs`

Editor pulls from queue and rebuilds meshes:

```csharp
private void ProcessDirtyRoads()
{
    // Throttled to 10 checks/second
    if (currentTime - _lastRoadUpdateTime < MinRoadUpdateInterval) return;
    
    var dirtyRoads = RoadRebuildQueue.GetAndClearDirtyRoads();
    
    foreach (var roadData in dirtyRoads)
    {
        // 1. Validate road still exists in CircuitData
        // 2. Validate all points have valid cross-sections
        // 3. Find live Road object
        // 4. Generate mesh via RoadProcessor.BuildRoadMesh()
        // 5. Update SceneRoad via UpdateRoadMesh()
    }
}
```

Called from:
- `OnInspectorGUI()`: Updates during inspector changes
- `OnSceneGUI()`: Updates during scene view manipulation

#### 5. SceneRoad Play Mode Protection (Unity Layer)
**File**: `Assets/Circuit Shaper by onomi/Unity/SceneRoad.cs`

Prevents destruction during play mode:

```csharp
private void Awake()
{
    this.gameObject.hideFlags = HideFlags.DontSaveInEditor;
    // ... mesh component setup
}
```

## Data Flow

### Road Creation
```
User selects points → CircuitShaper.CreateNewRoadFromPoints()
  → Create RoadData + Road
  → Add to CircuitData.CircuitRoads
  → point.AddRoadAssociation(roadData) for each point
  → RoadRebuildQueue.MarkDirty(roadData)
  → Editor.ProcessDirtyRoads() picks it up
  → RoadProcessor.BuildRoadMesh()
  → SceneRoad mesh updated
```

### Point Movement
```
User drags point → CircuitPoint.MoveCircuitPoint()
  → point.MarkAssociatedRoadsDirty()
  → RoadRebuildQueue.MarkDirty() for each associated road
  → Editor.ProcessDirtyRoads() picks them up
  → Roads rebuilt with new point positions
```

### Edit Realm Reconnection
```
User clicks off object → OnDisable() called
  → RoadRebuildQueue.Clear()
  → SceneRoads persist (HideFlags)
  
User clicks on object → OnEnable() called
  → CircuitShaper.BeginEdit()
  → Circuit recreated from CircuitData
  → Circuit.ReconnectRoadsToPoints()
  → Associations rebuilt
  → All roads marked dirty
  → Editor.ProcessDirtyRoads() rebuilds all
```

### Play Mode Transition
```
Enter Play Mode:
  → SceneRoads have HideFlags.DontSaveInEditor
  → Roads remain in scene but inactive
  → Queue cleared
  
Exit Play Mode:
  → SceneRoads reactivate
  → Edit realm recreated
  → Circuit.ReconnectRoadsToPoints() restores everything
```

## Advantages Over Event System

### 1. No Subscription Management
- **Before**: Complex subscription/unsubscription in Road.BuildRoadFromPoints()
- **After**: Simple queue marking, no cleanup needed

### 2. Survives Edit Realm Recreation
- **Before**: Events lost when edit realm destroyed
- **After**: Queue is static, persists across realm lifetimes

### 3. Predictable Timing
- **Before**: Events fire immediately, potentially multiple times per frame
- **After**: Throttled batch processing (10 times/second), controlled performance

### 4. Thread Safe
- **Before**: Race conditions from simultaneous events
- **After**: Lock-protected HashSet, atomic consumption

### 5. Duplicate Prevention
- **Before**: Moving point A and B of same road triggers two rebuilds
- **After**: HashSet automatically deduplicates

### 6. Validation Centralized
- **Before**: Validation scattered across event handlers
- **After**: Single validation point in ProcessDirtyRoads()

### 7. Play Mode Resilience
- **Before**: Roads destroyed/duplicated during transitions
- **After**: Clean separation with HideFlags + queue clear

## Performance Characteristics

- **Throttling**: 0.1s interval = max 10 updates/second
- **Batch Processing**: All dirty roads processed in single pass
- **Duplicate Prevention**: HashSet ensures each road rebuilt once
- **Memory**: HashSet<RoadData> grows with unique dirty roads (cleared each frame)
- **Thread Safety**: Lock contention minimal (quick add/clear operations)

## Future Enhancements

### Point Deletion Handling (TODO)
When a point is deleted:
1. Loop through `point.AssociatedRoads`
2. Remove pointData from `roadData.AssociatedPoints`
3. If road has < 2 points remaining, call `CircuitShaper.RemoveRoad()`
4. Otherwise, mark road dirty for rebuild
5. Clear `point.AssociatedRoads`

### Event System Cleanup (TODO)
Remove old event-based code:
- `Road.IsDirty`, `Road.MarkDirty()`, `Road.ClearDirty()`
- Event subscription code in `Road.BuildRoadFromPoints()`
- `RoadRebuilt` event from `Road.cs`
- `CheckAndRebuildDirtyRoads()` method (replaced by ProcessDirtyRoads)

## Testing Checklist

- [ ] Create road from 2 points
- [ ] Move point, verify road updates
- [ ] Click off object and back on, verify road persists
- [ ] Enter/exit play mode, verify road survives
- [ ] Move multiple points of same road, verify single rebuild
- [ ] Delete point that's part of road, verify graceful handling
- [ ] Create road with 10+ points, verify performance
- [ ] Select road after clicking off/on, verify selection valid

## Migration Notes

This architecture is **partially complete**:

✅ **Completed**:
- RoadRebuildQueue class
- CircuitPoint association tracking
- Circuit reconnection logic
- Editor queue processing
- SceneRoad play mode protection
- CircuitShaper.CreateNewRoadFromPoints() integration

⚠️ **Pending**:
- Point deletion handling
- Old event code cleanup

The system is **functional and testable** in its current state. The pending items are for robustness and cleanup.

## Conclusion

The queue-based architecture transforms a fragile event system into a robust, predictable communication channel. By decoupling road updates from point events and centralizing validation, we eliminate entire classes of bugs while improving performance and maintainability.
