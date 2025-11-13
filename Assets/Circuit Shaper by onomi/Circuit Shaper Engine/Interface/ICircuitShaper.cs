using OnomiCircuitShaper.Engine.Data;
using OnomiCircuitShaper.Engine.EditRealm;
using OnomiCircuitShaper.Engine.Processors;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace OnomiCircuitShaper.Engine.Interface
{
    /// <summary>
    /// Defines the public-facing contract for the Circuit Shaper engine. This interface
    /// provides a stable set of methods and events for an external application (like a Unity
    /// editor script) to interact with the circuit data and editing session.
    /// </summary>
    public interface ICircuitShaper
    {
        /// <summary>
        /// Fired when a road mesh has been generated or updated.
        /// Provides the raw data and the generated mesh data.
        /// </summary>
        event Action<RoadData, GenericMeshData> RoadBuilt;

        /// <summary>
        /// Fired when a bridge mesh has been generated or updated.
        /// </summary>
        event Action<BridgeData, GenericMeshData> BridgeBuilt;

        /// <summary>
        /// Fired when a railing mesh has been generated or updated.
        /// </summary>
        event Action<RailingData, GenericMeshData> RailingBuilt;

        /// <summary>
        /// Gets the current, underlying circuit data.
        /// </summary>
        CircuitData GetData();

        /// <summary>
        /// Sets the active circuit data, replacing any existing data.
        /// </summary>
        void SetData(CircuitData circuitData);

        /// <summary>
        /// Loads circuit data from a JSON string.
        /// </summary>
        CircuitData LoadFromJson(string json);

        /// <summary>
        /// Saves the current circuit data to a JSON string.
        /// </summary>
        string SaveToJson();

        /// <summary>
        /// Starts an editing session, creating the live "Edit Realm" objects.
        /// </summary>
        void BeginEdit();

        /// <summary>
        /// Ends the current editing session.
        /// </summary>
        void QuitEdit();


        // Edit realm object accessors
        Circuit GetLiveCircuit { get; }


        // Selection state (read-only from callers)
        IReadOnlyList<PointData> SelectedPoints { get; }
        CircuitCurveData SelectedCurve { get; }

        /// <summary>
        /// Adds a new point, creating a new curve in the process.
        /// </summary>
        void AddPointAsNewCurve(Vector3 position);

        // A version of this taking camera position and direction
        void AddPointAsNewCurve(Vector3 cameraPosition, Vector3 cameraDirection);

        /// <summary>
        /// Adds a new point to an existing curve.
        /// </summary>
        void AddPointToCurve(CircuitCurveData curveData, Vector3 position);

        // A version of this taking camera position and direction
        void AddPointToCurve(CircuitCurveData curveData, Vector3 cameraPosition, Vector3 cameraDirection);

        /// <summary>
        /// Removes a point from the circuit.
        /// </summary>
        void RemoveCircuitPoint(CircuitPointData circuitPointData);

        /// <summary>
        /// Moves a circuit anchor point to a new position.
        /// </summary>
        void MoveCircuitPoint(CircuitPointData circuitPointToMove, Vector3 newPosition);

        /// <summary>
        /// Moves a cross-section point to a new position.
        /// </summary>
        void MoveCrossSectionPoint(CrossSectionPointData crossSectionPointToMove, Vector3 newPosition);

        /// <summary>
        /// Creates a new road from a sequence of points.
        /// </summary>
        void CreateNewRoadFromPoints(CircuitPointData[] pointData);

        /// <summary>
        /// Removes a road from the circuit.
        /// </summary>
        void RemoveRoad(RoadData roadData);

        /// <summary>
        /// Calculates the average altitude (Y) of all points in the provided curve.
        /// </summary>
        float GetAverageCurveAltitude(CircuitCurveData curveData);

        /// <summary>
        /// Calculates the average altitude (Y) of all points across all curves in the circuit.
        /// </summary>
        float GetAverageCircuitAltitude(CircuitData circuitData);

        // Selection manipulation APIs
        /// <summary>
        /// Selects the supplied point and deselects others.
        /// </summary>
        void SelectPoint(CircuitPointData pointData);

        /// <summary>
        /// Deselects the supplied point if it is selected.
        /// </summary>
        void DeselectPoint(CircuitPointData pointData);

        /// <summary>
        /// Adds the supplied point to the current selection (does not clear existing selection).
        /// </summary>
        void AddPointToSelection(CircuitPointData pointData);

        /// <summary>
        /// Clears the current point selection and the selected curve.
        /// </summary>
        void ClearSelection();

        /// <summary>
        /// Adds a point to the currently selected curve (if any).
        /// </summary>
        void AddPointToSelectedCurve(Vector3 position);

        // A version of this taking camera position and direction
        void AddPointToSelectedCurve(Vector3 cameraPosition, Vector3 cameraDirection);


    }
}
