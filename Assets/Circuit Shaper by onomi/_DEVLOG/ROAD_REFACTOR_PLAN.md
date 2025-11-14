# Road System Refactoring Plan

## Current Problems
- Duplicate roads appearing
- Roads failing to reconnect after reset
- Overly complex validation logic
- Nested loops and fragile reference tracking
- Unity/Engine boundary violations
- Laggy scene handles

## New Clean Architecture

### 1. **Data Layer: Point Addressing**
- `RoadData.PointAddresses: List<PointAddress>` (curve index + point index)
- No more `CircuitPointData` references
- Survives serialization perfectly

### 2. **Lifecycle: Full Rebuild Per Session**
- OnEnable: Destroy all SceneRoads, rebuild from data
- OnDisable: Destroy all SceneRoads
- No persistence tracking needed
- Dictionary lives in editor only

### 3. **Selection: Engine-Side**
- `ICircuitShaper.SelectedRoad: Road`
- `SelectRoad(Road)` / `DeselectRoad()`
- Mirrors point/curve selection pattern
- Unity just reads and renders

### 4. **Handles: Simple Centerline**
- Draw spline through point centers
- Make thicker on hover (4px → 8px)
- Even thicker on selection (8px → 12px)
- No complex Bezier edge calculations
- Data-driven, no mesh dependency

### 5. **Queue System: Kept & Improved**
- Points mark roads dirty via addresses
- `Circuit.BuildPointRoadAssociations()` at session start
- Fast lookup, no events needed

## Implementation Steps
1. Update RoadData with PointAddress
2. Simplify Road class (remove events)
3. Add road selection to ICircuitShaper
4. Rewrite Circuit.ReconnectRoads() to use addresses
5. Simplify DrawRoadHandles() to centerline
6. Update CreateRoadFromPoints() to use addresses
7. Clean OnEnable/OnDisable to destroy all SceneRoads

## Result
- ~60% less code
- No reference tracking issues
- No serialization problems
- Faster rendering
- Easier to debug
