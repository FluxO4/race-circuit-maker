# Onomi Circuit Shaper Structure

This document outlines the architecture of the Onomi Circuit Shaper, a system for creating and editing race circuits. The system is designed with an engine-agnostic core, allowing it to be integrated into different game engines like Unity, Godot, etc., with a thin presentation layer.

## Core Philosophy

The architecture separates data, editing logic, and final output.

1.  **Data Layer (`OnomiCircuitShaper.Engine.Data`)**: Pure, serializable C# classes that represent the entire state of a circuit. This layer is completely engine-agnostic and can be saved to or loaded from formats like JSON.
2.  **Edit Realm (`OnomiCircuitShaper.Engine.EditRealm`)**: "Live" wrapper classes that hold the data during an editing session. These objects provide methods and events for interactive editing, without being tied to a specific engine's UI or scene objects.
3.  **Processors (`OnomiCircuitShaper.Engine.Processors`)**: Static utility classes that perform complex operations on the data, such as mesh generation, curve mathematics, and other procedural tasks. These are also engine-agnostic.
4.  **Interface (`OnomiCircuitShaper.Engine.Interface`)**: A public-facing API that defines the contract for interacting with the circuit shaper engine. This ensures a stable and consistent way for the presentation layer to communicate with the core engine.
5.  **Unity Layer (`OnomiCircuitShaper.Unity`)**: The Unity-specific implementation. It handles rendering, user input (Inspector UI and Scene View gizmos), and bridges the gap between the Unity environment and the engine-agnostic core.

## Data Hierarchy (`OnomiCircuitShaper.Engine.Data`)

This is the hierarchy of the pure data classes that can be serialized to save and load circuits.

-   `OnomiCircuitShaperData` (Root Object)
    -   `CircuitData circuitData`: Contains all the geometric and structural data for the circuit.
    -   `CircuitAndEditorSettings settingsData`: Holds settings for editor behavior and gizmo appearance.
-   `CircuitData`
    -   `List<CircuitCurveData> CircuitCurves`: The main splines that define the path of the circuit.
    -   `List<RoadData> CircuitRoads`: Definitions for the road meshes to be generated along the curves.
-   `CircuitCurveData` (inherits from `CurveData<CircuitPointData>`)
    -   `List<CircuitPointData> CurvePoints`: The points that make up the main circuit spline.
    -   `bool IsClosed`: Whether the curve forms a closed loop.
-   `CircuitPointData` (inherits from `PointData`)
    -   `CrossSectionCurveData CrossSectionCurve`: Defines the 2D shape of the road at this point.
-   `CrossSectionCurveData` (inherits from `CurveData<CrossSectionPointData>`)
    -   `List<CrossSectionPointData> CurvePoints`: The points that make up the 2D cross-section shape.
-   `CrossSectionPointData` (inherits from `PointData`)
    -   (No extra fields, exists for type safety and future expansion)
-   `PointData` (Base class for all points)
    -   `Vector3 PointPosition`: The anchor point's world position.
    -   `Vector3 ForwardControlPointPosition`: The forward (outgoing) Bézier control handle.
    -   `Vector3 BackwardControlPointPosition`: The backward (incoming) Bézier control handle.
    -   `Vector3 UpDirection`: The point's orientation vector (for banking/rotation).
    -   `bool? IndependentControlPointsOverride`: Per-point override for control point behavior.
    -   `float AutoSetTension`: Controls the automatic control point distance (default 0.33).
    -   `float NormalizedPosition01`: Pre-calculated normalized distance along the curve (0 to 1).
-   `RoadData`
    -   `List<PointData> AssociatedPoints`: Sequence of points defining the road path.
    -   `int WidthWiseVertexCount`: Cross-section tessellation resolution.
    -   `float LengthWiseVertexCountPerUnitWidthWiseVertexCount`: Length tessellation ratio.
    -   `Vector2 UVTile`: UV tiling factor.
    -   `Vector2 UVOffset`: UV offset.
    -   `float Min`: Start position along the road (0-1 normalized).
    -   `float Max`: End position along the road (0-1 normalized).
    -   `List<RailingData> Railings`: Railings attached to this road.
    -   `BridgeData Bridge`: Optional bridge structure.
-   `RailingData`
    -   `float RailingHeight`: Vertical height of the railing.
    -   `float Min`: Start position (0-1 along road).
    -   `float Max`: End position (0-1 along road).
    -   `float HorizontalPosition`: Position across road width (0-1, where 0 is left edge, 1 is right edge).
-   `BridgeData`
    -   `bool UseTemplate`: If true, use I-beam template; if false, use custom shape.
    -   `List<Vector2> BridgeShapePoints`: Custom 2D profile points (if not using template).
    -   `Vector2 UVTile`: UV tiling for bridge texture.
    -   `float TemplateEdgeWidth`: Width of template edge beams.
    -   `float TemplateBridgeHeight`: Height of template I-beam.
    -   `float TemplateFlangeWidth`: Width of template flanges.
    -   `float TemplateFlangeHeight`: Height of template flanges.
    -   `float TemplateFlangeDepth`: Depth of template flanges.
    -   `float TemplateCurbHeight`: Height of template curb.

## Edit Realm Hierarchy (`OnomiCircuitShaper.Engine.EditRealm`)

These are the "live" objects used during an editing session. They wrap data objects and provide interactive functionality.

-   `Circuit` (Top-level edit session manager)
    -   **Properties:**
        -   `CircuitData Data`: The raw circuit data being edited.
        -   `CircuitAndEditorSettings Settings`: Current editor settings.
        -   `List<CircuitCurve> Curves`: Live list of all circuit curves.
        -   `List<Road> Roads`: Live list of all roads.
    -   **Methods:**
        -   `void BeginEditFromData(CircuitData, CircuitAndEditorSettings)`: Initializes edit session, creates live wrappers from data.
        -   `void EndEdit()`: Clears all live objects, keeps data intact.
        -   `CircuitCurve AddCurve(CircuitCurveData)`: Adds a new curve to the circuit.
        -   `void AddRoad(RoadData)`: Adds a new road to the circuit.
    -   **Notes:** [Look here onomi] Missing FindPoint helper methods to resolve PointData to live Point wrappers. Needed for establishing road-to-point references.

-   `CircuitCurve` (Live editable main circuit spline)
    -   **Properties:**
        -   `CircuitCurveData Data`: Underlying curve data.
        -   `CircuitAndEditorSettings Settings`: Reference to global settings.
        -   `List<CircuitPoint> Points`: Live list of points in this curve.
        -   `bool IsClosed`: Gets/sets whether curve forms a closed loop.
    -   **Events:**
        -   `event Action CurveStateChanged`: Fired when curve structure changes.
    -   **Methods:**
        -   `CircuitPoint AddPointAtIndex(Vector3, int)`: Inserts point at specific index.
        -   `CircuitPoint AddPointOnCurve(Vector3)`: Adds point at closest position on curve.
        -   `CircuitPoint AddPointOnCurve(Vector3, Vector3)`: Adds point via camera ray cast.
        -   `void UpdateNeighborReferences()`: Updates NextPoint/PreviousPoint links for all points.
        -   `void OnCurveStateChanged()`: Raises CurveStateChanged event.
    -   **Notes:** [Look here onomi] Missing constructor that takes (data, settings, parentCircuit). Missing RemovePoint, SplitCurve methods.

-   `CircuitPoint` (Live editable point on main circuit)
    -   **Inherits from:** `Point<CircuitPointData>`
    -   **Properties:**
        -   `CircuitCurve CircuitCurve`: Parent curve reference.
        -   `CrossSectionCurve CrossSection`: Live cross-section for this point.
        -   `Vector3 GetLeftEndPointPosition`: World position of leftmost cross-section point.
        -   `Vector3 GetRightEndPointPosition`: World position of rightmost cross-section point.
    -   **Methods:**
        -   `void SetCrossSectionCurve(CrossSectionCurve)`: Assigns a new cross-section.
        -   `void Rotate(float)`: Rotates up vector around forward axis (banking).
        -   (Inherits MovePoint, MoveForwardControlPoint, MoveBackwardControlPoint, AutoSetControlPoints from Point<T>)
    -   **Notes:** [Look here onomi] Constructor takes (CircuitCurve, CircuitPointData, Settings). Overrides base movement methods to trigger neighbor updates when AutoSetControlPoints is enabled.

-   `CrossSectionCurve` (Live editable 2D road profile)
    -   **Properties:**
        -   `CrossSectionCurveData Data`: Underlying data.
        -   `CircuitAndEditorSettings Settings`: Reference to settings.
        -   `List<CrossSectionPoint> Points`: Live points in this profile.
        -   `CircuitPoint parentCircuitPoint`: The parent circuit point.
    -   **Events:**
        -   `event Action CurveStateChanged`: Fired when cross-section changes.
    -   **Methods:**
        -   `void ChangeCrossSectionPointCount(int)`: Resamples to new point count, preserving shape.
        -   `void SetPointsFromLocalPositions(List<Vector3>)`: Rebuilds from list of 2D positions.
        -   `void SetPreset(Vector3[])`: Applies a predefined shape.
        -   `void HandleCrossSectionPointChanged()`: Recalculates all control points for smoothness.
        -   `void NormaliseCrossSectionPoints()`: Updates normalized positions along curve.
        -   `void UpdateNeighborReferences()`: Updates point neighbor links.
    -   **Notes:** [Look here onomi] Constructor takes (data, settings, parentCircuitPoint). IsClosed is always false for cross-sections.

-   `CrossSectionPoint` (Live editable point on 2D profile)
    -   **Inherits from:** `Point<CrossSectionPointData>`
    -   **Properties:**
        -   `CircuitPoint ParentCircuitPoint`: Parent circuit point reference.
        -   `CrossSectionCurve ParentCrossSectionCurve`: Parent curve reference.
    -   **Methods:**
        -   `void MoveCrossSectionPoint(Vector3)`: Moves point (handles world-to-local transform).
        -   `Vector3 GetWorldPosition()`: Calculates world position from local 2D coords.
        -   `Vector3 GetWorldForwardControlPointPosition()`: World position of forward control.
        -   `Vector3 GetWorldBackwardControlPointPosition()`: World position of backward control.
    -   **Notes:** [Look here onomi] Position stored as 2D (x=across, y=up, z=0) relative to parent. World position calculated by transforming through parent's orientation.

-   `Point<TData>` (Abstract base for all points)
    -   **Properties:**
        -   `TData Data`: The underlying PointData.
        -   `CircuitAndEditorSettings Settings`: Settings reference.
        -   `int PointIndex`: Index within parent curve.
        -   `Point<TData> NextPoint`: Next point in sequence.
        -   `Point<TData> PreviousPoint`: Previous point in sequence.
        -   `Vector3 GetRightVector`: Calculated right direction (cross product of forward and up).
        -   `Vector3 GetForwardVector`: Calculated forward direction (tangent).
        -   `Vector3 GetUpVector`: Normalized up direction.
        -   `Vector3 PointPosition`: Data passthrough for anchor position.
        -   `Vector3 ForwardControlPointPosition`: Data passthrough.
        -   `Vector3 BackwardControlPointPosition`: Data passthrough.
        -   `Vector3 RotatorPointPosition`: Position for rotation gizmo handle.
    -   **Events:**
        -   `event Action<Point<TData>> PointStateChanged`: Fired when point is modified.
    -   **Methods:**
        -   `virtual void MovePoint(Vector3)`: Moves anchor and both control points.
        -   `virtual void MoveForwardControlPoint(Vector3)`: Moves forward control.
        -   `virtual void MoveBackwardControlPoint(Vector3)`: Moves backward control.
        -   `abstract void AutoSetControlPoints()`: Recalculates control points for smooth curve.
        -   `protected virtual void OnPointStateChanged()`: Raises PointStateChanged event.

-   `Road` (Live editable road mesh)
    -   **Properties:**
        -   `RoadData Data`: Underlying road data.
        -   `CircuitAndEditorSettings Settings`: Settings reference.
        -   `List<Point<PointData>> AssociatedPoints`: Live point references.
        -   `List<Railing> Railings`: Live railings.
        -   `Bridge Bridge`: Live bridge (if any).
    -   **Events:**
        -   `event Action<Road> RoadRebuilt`: Fired when mesh needs regeneration.
    -   **Methods:**
        -   `void BuildRoadFromPoints(List<CircuitPoint>)`: Initializes road from point list.
    -   **Notes:** [Look here onomi] Constructor takes (RoadData, Settings, Circuit). Should populate Railings/Bridge wrappers and subscribe to point change events.

-   `Railing` (Live editable railing)
    -   **Properties:**
        -   `RailingData Data`: Underlying railing data.

-   `Bridge` (Live editable bridge)
    -   **Properties:**
        -   `BridgeData Data`: Underlying bridge data.

## Interface Layer (`OnomiCircuitShaper.Engine.Interface`)

Public-facing API for presentation layers (Unity, Godot, etc.).

-   `ICircuitShaper` (Main interface contract)
    -   **Events:**
        -   `event Action<RoadData, GenericMeshData> RoadBuilt`: Fired when road mesh is generated.
        -   `event Action<BridgeData, GenericMeshData> BridgeBuilt`: Fired when bridge mesh is generated.
        -   `event Action<RailingData, GenericMeshData> RailingBuilt`: Fired when railing mesh is generated.
    -   **Properties:**
        -   `CircuitData GetData()`: Returns current circuit data.
        -   `Circuit GetLiveCircuit`: Returns live edit session object.
        -   `IReadOnlyList<CircuitPoint> SelectedPoints`: Current point selection.
        -   `CircuitCurve SelectedCurve`: Currently selected curve.
        -   `SinglePointSelectionMode GetSinglePointSelectionMode()`: Current editing mode.
    -   **Methods:**
        -   `void SetData(CircuitData)`: Replaces current circuit data.
        -   `void BeginEdit()`: Starts editing session.
        -   `void QuitEdit()`: Ends editing session.
        -   `CircuitData LoadFromJson(string)`: Deserializes circuit from JSON.
        -   `string SaveToJson()`: Serializes circuit to JSON.
        -   `CircuitPoint AddPointAsNewCurve(Vector3)`: Creates new curve with one point.
        -   `CircuitPoint AddPointAsNewCurve(Vector3, Vector3)`: Creates via ray cast.
        -   `CircuitPoint AddPointToCurve(CircuitCurve, Vector3)`: Adds point to existing curve.
        -   `CircuitPoint AddPointToCurve(CircuitCurve, Vector3, Vector3)`: Adds via ray cast.
        -   (And many more methods for selection, manipulation, cross-section editing, etc.)

-   `CircuitShaper` (Concrete implementation)
    -   **Implements:** `ICircuitShaper`
    -   **Fields:**
        -   `CircuitData _currentData`
        -   `CircuitAndEditorSettings _settings`
        -   `Circuit _liveCircuit`
        -   `List<CircuitPoint> _selectedPoints`
        -   `CircuitCurve _selectedCurve`
        -   `SinglePointSelectionMode _singlePointSelectionMode`
    -   **Notes:** [Look here onomi] Orchestrates all operations, delegates to edit-realm objects, subscribes to RoadRebuilt events and forwards them after calling RoadProcessor.

-   `SinglePointSelectionMode` (Enum)
    -   `AnchorPoint`: Edit the main anchor position.
    -   `ForwardControlPoint`: Edit the forward Bézier handle.
    -   `BackwardControlPoint`: Edit the backward Bézier handle.

## Processor Layer (`OnomiCircuitShaper.Engine.Processors`)

Static utility classes for calculations and mesh generation.

-   `CircuitMathematics` (Pure math functions)
    -   **Methods:**
        -   `static Vector3 BezierEvaluateQuadratic(Vector3, Vector3, Vector3, float)`: Evaluates quadratic Bézier.
        -   `static Vector3 BezierEvaluateCubic(Vector3, Vector3, Vector3, Vector3, float)`: Evaluates cubic Bézier.
        -   `static float EstimateCurveLength(Vector3, Vector3, Vector3, Vector3, int)`: Estimates arc length via subdivision.
        -   `static float GetAverageCurveAltitude<TPoint>(CurveData<TPoint>)`: Calculates average Y position of curve.
        -   `static float GetAverageCircuitAltitude(CircuitData)`: Calculates average Y position of entire circuit.

-   `CurveProcessor` (Curve operations)
    -   **Methods:**
        -   `static void NormaliseCurvePoints<TPointData>(CurveData<TPointData>, int)`: Pre-calculates normalized positions along curve.
        -   `static Vector3 LerpAlongCurve<TPointData>(CurveData<TPointData>, float)`: Fast lookup of position at normalized distance.
        -   `static Vector3 LerpBetweenTwoCrossSections(CircuitPointData, CircuitPointData, float, float)`: Interpolates vertex between two cross-sections (key method for road mesh generation).
    -   **Notes:** [Look here onomi] LerpBetweenTwoCrossSections takes x (along cross-section 0-1) and y (between points 0-1) and returns world-space vertex position.

-   `CircuitPointProcessor` (Point-specific operations)
    -   **Methods:**
        -   `static void TransformToAlignEndPoints(CircuitPoint)`: Aligns cross-section endpoints (not currently used).
        -   `static void ProjectAndPerpendiculariseCrossSection(CircuitPoint)`: Projects cross-section to plane (not currently used).
    -   **Notes:** [Look here onomi] These were experimental and aren't called anywhere.

-   `RoadProcessor` (Mesh generation)
    -   **Methods:**
        -   `static GenericMeshData BuildRoadMesh(Road)`: Generates road geometry.
        -   `static GenericMeshData BuildBridgeMesh(Bridge)`: Generates bridge geometry.
        -   `static GenericMeshData BuildRailingMesh(Railing)`: Generates railing geometry.
    -   **Notes:** [Look here onomi] Returns engine-agnostic GenericMeshData (vertices, UVs, triangles) that Unity layer converts to UnityEngine.Mesh.

-   `GenericMeshData` (Struct)
    -   `Vector3[] Vertices`: Mesh vertex positions.
    -   `Vector2[] UVs`: Texture coordinates.
    -   `int[] Triangles`: Triangle indices (counter-clockwise winding).

## Preset Layer (`OnomiCircuitShaper.Engine.Presets`)

Predefined cross-section shapes.

-   `CrossSectionPresets` (Static class)
    -   `static Vector3[] FlatPreset`: Simple 2-point flat road (4 units wide).
    -   `static Vector3[] TriangularPreset`: 3-point V-shaped channel.
    -   `static Vector3[] TrapezoidalPreset`: 4-point channel with flat bottom.
    -   `static Vector3[] InvertedTrapezoidalPreset`: 4-point raised platform with sloped sides.

## Unity Layer (`OnomiCircuitShaper.Unity`)

Unity-specific presentation and interaction code.

-   `OnomiCircuitShaper` (MonoBehaviour - Scene component)
    -   **Purpose:** Main scene object that holds circuit data and manages SceneRoads.
    -   **Properties:**
        -   `OnomiCircuitShaperData CircuitData`: Serialized data asset.
        -   `Dictionary<RoadData, SceneRoad>`: Maps data to visual GameObjects.
    -   **Methods:**
        -   Initializes ICircuitShaper engine instance.
        -   Subscribes to RoadBuilt/BridgeBuilt/RailingBuilt events.
        -   Creates/updates SceneRoad GameObjects when events fire.
    -   **Notes:** [Look here onomi] Acts as bridge between Unity scene and engine-agnostic core. Handles serialization to Unity scene files.

-   `OnomiCircuitShaperEditor` (Editor - Custom inspector/scene view)
    -   **Purpose:** Provides interactive editing UI in Unity Editor.
    -   **Functionality:**
        -   Draws gizmos for all points (anchor, control points, rotation handles).
        -   Handles mouse input (selection, dragging, adding points).
        -   Displays Inspector GUI for properties (cross-section presets, road parameters, etc.).
        -   Translates user actions into ICircuitShaper method calls.
    -   **Key Methods:**
        -   `OnSceneGUI()`: Renders interactive handles in scene view.
        -   `OnInspectorGUI()`: Renders property editors in inspector.
        -   Various helper methods for different editing modes.
    -   **Notes:** [Look here onomi] Uses Unity's Handles API for 3D gizmos. Supports undo/redo via Undo.RecordObject. Implements ray casting for adding points via mouse clicks.

-   `SceneRoad` (MonoBehaviour - Visual road object)
    -   **Purpose:** GameObject component representing a single road mesh.
    -   **Properties:**
        -   `RoadData AssociatedRoadData`: Reference to source data.
        -   Components: `MeshFilter`, `MeshRenderer`, `MeshCollider`.
    -   **Methods:**
        -   `void UpdateMesh(GenericMeshData)`: Converts engine mesh data to Unity Mesh, assigns to filter/collider.
        -   `void UpdateMaterial(Material)`: Assigns material to renderer.
    -   **Notes:** [Look here onomi] Created/managed by OnomiCircuitShaper. Mesh is regenerated whenever RoadBuilt event fires.

-   `NumericsConverter` (Utility class)
    -   **Purpose:** Converts between System.Numerics.Vector3 (engine) and UnityEngine.Vector3 (Unity).
    -   **Methods:**
        -   `static UnityEngine.Vector3 ToUnity(System.Numerics.Vector3)`
        -   `static System.Numerics.Vector3 FromUnity(UnityEngine.Vector3)`
        -   (Similar for Vector2, Quaternion)
    -   **Notes:** [Look here onomi] Necessary because engine uses System.Numerics for portability while Unity uses its own vector types.

-   `NumericsPropertyDrawers` (Editor - Custom property drawers)
    -   **Purpose:** Allows System.Numerics vectors to display/edit correctly in Unity Inspector.
    -   **Draws:** Custom GUI for Vector3, Vector2, Quaternion from System.Numerics namespace.
    -   **Notes:** [Look here onomi] Without these, System.Numerics types would show as unexpandable fields in Inspector
    -   `SerializableVector3 PointPosition`: The anchor point's position.
    -   `SerializableVector3 ForwardControlPointPosition`: The forward Bézier handle.
    -   `SerializableVector3 BackwardControlPointPosition`: The backward Bézier handle.
    -   `SerializableVector3 UpDirection`: The orientation of the point.
    -   `float NormalizedPosition01`: The point's normalized distance along its curve.
-   `RoadData`
    -   `List<PointData> AssociatedPoints`: A sequence of points (from one or more `CircuitCurveData`) that this road is built upon.
    -   `int WidthWiseVertexCount`: The resolution of the road mesh across its width.
    -   `int LengthWiseVertexCountPerUnitWidthWiseVertexCount`: The resolution of the road mesh along its length.
    -   `SerializableVector2 UVTile`, `UVOffset`: UV mapping parameters.
    -   `List<RailingData> Railings`: Railings attached to this road.
    -   `BridgeData Bridge`: Bridge structure attached to this road.
-   `RailingData`
    -   `float RailingHeight`, `Min`, `Max`, `HorizontalPosition`: Parameters defining a railing's size and position along a road.
-   `BridgeData`
    -   Parameters for generating a bridge mesh.

