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


        Circuit GetLiveCircuit { get; }

        /// <summary>
        /// Sets the active circuit data, replacing any existing data.
        /// </summary>
        void SetData(CircuitData circuitData);

        /// <summary>

        /// Starts an editing session, creating the live edit realm objects.
        /// </summary>
        void BeginEdit();

        /// <summary>
        /// Ends the current editing session.
        /// </summary>
        void QuitEdit();



        // Selection state (read-only from callers)
        IReadOnlyList<CircuitPoint> SelectedPoints { get; }
        CircuitCurve SelectedCurve { get; }

        /// <summary>
        /// Adds a new point, creating a new curve in the process.
        /// </summary>
        void AddPointAsNewCurve(Vector3 position);

        // A version of this taking camera position and direction
        void AddPointAsNewCurve(Vector3 cameraPosition, Vector3 cameraDirection);

        /// <summary>
        /// Adds a new point to an existing curve.
        /// </summary>
        void AddPointToCurve(CircuitCurve curve, Vector3 position);

        // A version of this taking camera position and direction
        void AddPointToCurve(CircuitCurve curve, Vector3 cameraPosition, Vector3 cameraDirection);

        /// <summary>
        /// Removes a point from the circuit.
        /// </summary>
        void RemoveCircuitPoint(CircuitPoint circuitPoint);

        /// <summary>
        /// Moves a circuit anchor point to a new position.
        /// </summary>
        void MoveCircuitPoint(CircuitPoint circuitPointToMove, Vector3 newPosition);

        //Move the circuit point's forward control point
        void MoveCircuitPointForwardControl(CircuitPoint circuitPointToMove, Vector3 newPosition);
        //Move the circuit point's backward control point
        void MoveCircuitPointBackwardControl(CircuitPoint circuitPointToMove, Vector3 newPosition);

        /// <summary>
        /// Moves a cross-section point to a new position.
        /// </summary>
        void MoveCrossSectionPoint(CrossSectionPoint crossSectionPointToMove, Vector3 newPosition);

        /// <summary>
        /// Creates a new road from a sequence of points.
        /// </summary>
        void CreateNewRoadFromPoints(CircuitPoint[] pointData);
        /// <summary>
        /// Removes a road from the circuit.
        /// </summary>
        void RemoveRoad(Road road);


        // Selection manipulation APIs
        /// <summary>
        /// Selects the supplied point and deselects others.
        /// </summary>
        void SelectPoint(CircuitPoint point);

        /// <summary>
        /// Deselects the supplied point if it is selected.
        /// </summary>
        void DeselectPoint(CircuitPoint point);

        /// <summary>
        /// Adds the supplied point to the current selection (does not clear existing selection).
        /// </summary>
        void AddPointToSelection(CircuitPoint point);

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
