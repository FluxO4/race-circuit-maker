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
        /// Gets the current, underlying circuit data.
        /// </summary>
        CircuitData GetData();

        /// <summary>
        /// Gets all roads that need rebuilding and clears the queue.
        /// This is the primary mechanism for Unity to discover which roads need mesh updates.
        /// </summary>
        List<Road> GetAndClearDirtyRoads();

        /// <summary>
        /// Clears the road rebuild queue without processing.
        /// Called when editor is disabled to prevent stale rebuild requests.
        /// </summary>
        void ClearRoadRebuildQueue();


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

        SinglePointSelectionMode GetSinglePointSelectionMode();

        CircuitCurve SelectedCurve { get; }

        Road SelectedRoad { get; }

        /// <summary>
        /// Adds a new point, creating a new curve in the process.
        /// </summary>
        CircuitPoint AddPointAsNewCurve(Vector3 position);

        // A version of this taking camera position and direction
        CircuitPoint AddPointAsNewCurve(Vector3 cameraPosition, Vector3 cameraDirection);

        /// <summary>
        /// Adds a new point to an existing curve.
        /// </summary>
        CircuitPoint AddPointToCurve(CircuitCurve curve, Vector3 position);

        // A version of this taking camera position and direction
        CircuitPoint AddPointToCurve(CircuitCurve curve, Vector3 cameraPosition, Vector3 cameraDirection);

        /// <summary>
        /// Removes a point from the circuit.
        /// </summary>
        void RemoveCircuitPoint(CircuitPoint circuitPoint);

        //Remove the selected points
        void DeleteSelectedPoints();

        /// <summary>
        /// Moves a circuit anchor point to a new position.
        /// </summary>
        void MoveCircuitPoint(CircuitPoint circuitPointToMove, Vector3 newPosition);

        //Move the circuit point's forward control point
        void MoveCircuitPointForwardControl(CircuitPoint circuitPointToMove, Vector3 newPosition);
        //Move the circuit point's backward control point
        void MoveCircuitPointBackwardControl(CircuitPoint circuitPointToMove, Vector3 newPosition);
        

        /// <summary>
        /// Rotates a circuit point by the given euler angles.
        /// </summary>
        void RotateCircuitPoint(CircuitPoint circuitPoint, Vector3 eulerAngles);

        /// <summary>
        /// Moves a cross-section point to a new position.
        /// </summary>
        void MoveCrossSectionPoint(CrossSectionPoint crossSectionPointToMove, Vector3 newPosition);


        //A function that sets the number of cross section points in a cross section curve
        void SetCrossSectionPointCount(CrossSectionCurve crossSectionCurve, int newPointCount);


        //A function that sets the cross section of a circuit point to a preset
        void SetCrossSectionPreset(CircuitPoint circuitPoint, Vector3[] preset);


        void SetCrossSectionPointAutoSetTension(CrossSectionPoint crossSectionPoint, float newTension);

        /// <summary>
        /// Creates a new road from a sequence of points.
        /// </summary>
        void CreateNewRoadFromPoints(CircuitPoint[] pointData);

        /// <summary>
        /// Creates a new road from the currently selected points.
        /// </summary>
        void CreateRoadFromSelectedPoints();

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
        /// Selects the supplied road and deselects others.
        /// </summary>
        void SelectRoad(Road road);

        /// <summary>
        /// Deselects the currently selected road if any.
        /// </summary>
        void DeselectRoad();


        //Set the single point selection mode
        void SetSinglePointSelectionMode(SinglePointSelectionMode mode);

        /// <summary>
        /// Adds a point to the currently selected curve (if any).
        /// </summary>
        CircuitPoint AddPointToSelectedCurve(Vector3 position);

        // A version of this taking camera position and direction
        CircuitPoint AddPointToSelectedCurve(Vector3 cameraPosition, Vector3 cameraDirection);

        //A function to set the IsClosed property of the selected curve
        void SetSelectedCurveIsClosed(bool isClosed);

        //A function to delete the currently selected curve
        void DeleteSelectedCurve();

        /// <summary>
        /// Attempts to change the start segment index of a road. Returns false if it would cause overlap with another road.
        /// </summary>
        bool TrySetRoadStartSegment(Road road, int newStartSegment);

        /// <summary>
        /// Attempts to change the end segment index of a road. Returns false if it would cause overlap with another road.
        /// </summary>
        bool TrySetRoadEndSegment(Road road, int newEndSegment);

    }

    public enum SinglePointSelectionMode
    {
        AnchorPoint,
        ForwardControlPoint,
        BackwardControlPoint
    }
}
