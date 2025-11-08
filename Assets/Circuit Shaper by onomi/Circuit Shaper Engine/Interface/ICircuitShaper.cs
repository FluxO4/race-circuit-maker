using OnomiCircuitShaper.Engine.Data;
using OnomiCircuitShaper.Engine.Processors;
using System;
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

        /// <summary>
        /// Adds a new point, creating a new curve in the process.
        /// </summary>
        void AddPointAsNewCurve(Vector3 position);

        /// <summary>
        /// Adds a new point to an existing curve.
        /// </summary>
        void AddPointToCurve(CurveData curveData, Vector3 position);

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
        void CreateNewRoadFromPoints(PointData[] pointData);

        /// <summary>
        /// Removes a road from the circuit.
        /// </summary>
        void RemoveRoad(RoadData roadData);
    }
}
