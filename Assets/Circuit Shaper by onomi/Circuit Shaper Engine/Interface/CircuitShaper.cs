using OnomiCircuitShaper.Engine.Data;
using OnomiCircuitShaper.Engine.EditRealm;
using OnomiCircuitShaper.Engine.Processors;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace OnomiCircuitShaper.Engine.Interface
{
    /// <summary>
    /// The concrete implementation of the ICircuitShaper interface. This class orchestrates
    /// the entire editing process, managing the live Circuit object and calling the static
    /// Processor classes to act on the data.
    /// </summary>
    public class CircuitShaper : ICircuitShaper
    {
        public event Action<RoadData, GenericMeshData> RoadBuilt;
        public event Action<BridgeData, GenericMeshData> BridgeBuilt;
        public event Action<RailingData, GenericMeshData> RailingBuilt;

        private CircuitData _currentData;
        private CircuitAndEditorSettings _settings;
        private Circuit _liveCircuit;
        // Selection state
        private readonly List<PointData> _selectedPoints = new List<PointData>();
        private CircuitCurveData _selectedCurve;

        public CircuitShaper(CircuitData data, CircuitAndEditorSettings settings)
        {
            _currentData = data;
            _settings = settings;
        }

        // Expose selection state as read-only
        public IReadOnlyList<PointData> SelectedPoints => _selectedPoints.AsReadOnly();
        public CircuitCurveData SelectedCurve => _selectedCurve;

        public void BeginEdit()
        {
            if (_liveCircuit != null)
                throw new InvalidOperationException("An edit session is already in progress.");


            //Create a new live circuit object and populate it from the current data.
            _liveCircuit = new Circuit();
            _liveCircuit.BeginEditFromData(_currentData, _settings);
        }

        public void QuitEdit()
        {
            _liveCircuit?.EndEdit();
            _liveCircuit = null;
        }

        public CircuitData GetData() => _currentData;

        public void SetData(CircuitData circuitData)
        {
            _currentData = circuitData;
        }

        public CircuitData LoadFromJson(string json)
        {
            // To be implemented using a JSON library.
            return null;
        }

        public string SaveToJson()
        {
            // To be implemented using a JSON library.
            return string.Empty;
        }

        public void AddPointAsNewCurve(Vector3 position)
        {
            // Create a new curve containing a single circuit point at the given position,
            // add it to the current data, and select the newly created point.
            var curve = new CircuitCurveData();

            var newPoint = new CircuitPointData()
            {
                PointPosition = position,
                ForwardControlPointPosition = position + new Vector3(1, 0, 0),
                BackwardControlPointPosition = position + new Vector3(-1, 0, 0),
                UpDirection = System.Numerics.Vector3.UnitY
            };

            curve.CurvePoints = new List<PointData>() { newPoint };

            if (_currentData.CircuitCurves == null)
                _currentData.CircuitCurves = new List<CircuitCurveData>();

            _currentData.CircuitCurves.Add(curve);

            // If there is a live edit circuit, keep its data in sync as best-effort so selection
            // lookup can find the new point immediately.
            if (_liveCircuit != null && _liveCircuit.Data != null)
            {
                if (_liveCircuit.Data.CircuitCurves == null)
                    _liveCircuit.Data.CircuitCurves = new List<CircuitCurveData>();

                _liveCircuit.Data.CircuitCurves.Add(curve);
            }

            // Select the newly created point (this will also set the selected curve).
            SelectPoint(newPoint);
        }
        
        public void AddPointToCurve(CircuitCurveData curveData, Vector3 position)
        {
            if (curveData == null)
                return;

            // Create a simple new circuit point and append it to the curve's points list.
            // We only perform a minimal, safe modification of the data structure here so
            // higher layers (edit realm or UI) can pick up the change. Full consistency
            // (indexes, neighbour wiring, autoset control points) is the responsibility
            // of the EditRealm/Circuit layer when BeginEdit/Build is invoked.
            var newPoint = new CircuitPointData()
            {
                PointPosition = position,
                ForwardControlPointPosition = position,
                BackwardControlPointPosition = position,
                UpDirection = new System.Numerics.Vector3(0, 1, 0)
            };

            if (curveData.CurvePoints == null)
                curveData.CurvePoints = new List<PointData>();

            curveData.CurvePoints.Add(newPoint);
        }

        public void RemoveCircuitPoint(CircuitPointData circuitPointData)
        {
            // To be implemented.
        }

        public void MoveCircuitPoint(CircuitPointData circuitPointToMove, Vector3 newPosition)
        {
            // To be implemented.
        }

        public void MoveCrossSectionPoint(CrossSectionPointData crossSectionPointToMove, Vector3 newPosition)
        {
            // To be implemented.
        }

        public void CreateNewRoadFromPoints(CircuitPointData[] pointData)
        {
            // To be implemented.
        }

        public void RemoveRoad(RoadData roadData)
        {
            // To be implemented.
        }

        public float GetAverageCurveAltitude(CurveData curveData)
        {
            return CircuitMathematics.GetAverageCurveAltitude(curveData);
        }

        public float GetAverageCircuitAltitude(CircuitData circuitData)
        {
            return CircuitMathematics.GetAverageCircuitAltitude(circuitData);
        }

        // Selection manipulation implementations
        public void SelectPoint(CircuitPointData pointData)
        {
            if (pointData == null)
                return;

            _selectedPoints.Clear();
            _selectedPoints.Add(pointData);

            //Also set the selected curve if this point belongs to one.
            if (_liveCircuit != null && _liveCircuit.Data != null && _liveCircuit.Data.CircuitCurves != null)
            {
                _selectedCurve = _liveCircuit.Data.CircuitCurves.Find(curve =>
                    curve.CurvePoints != null && curve.CurvePoints.Contains(pointData));
            }
            else
            {
                _selectedCurve = null;
            }
        }

        public void DeselectPoint(CircuitPointData pointData)
        {
            if (pointData == null)
                return;

            _selectedPoints.Remove(pointData);
        }

        public void AddPointToSelection(CircuitPointData pointData)
        {
            if (pointData == null)
                return;

            if (_selectedPoints.Contains(pointData))
                return;

            //We should use SelectPoint to select the first point.
            if (_selectedPoints.Count == 0)
            {
                SelectPoint(pointData);
                return;
            }
            
            //Return if trying to add a point from a different curve than the selected one.
            if(_selectedCurve != null && !_selectedCurve.CurvePoints.Contains(pointData))
            {
                return;
            }
            
            
            //We can only select points connected to any already selected point
            if(_liveCircuit != null && _selectedPoints.Count > 0)
            {
                bool isConnected = false;
                foreach(var selectedPoint in _selectedPoints)
                {
                    if(_liveCircuit.Curves[SelectedCurve].Points[selectedPoint].NextPoint.Data == pointData ||
                       _liveCircuit.Curves[SelectedCurve].Points[selectedPoint].PreviousPoint.Data == pointData)
                    {
                        isConnected = true;
                        break;
                    }
                }

                if(!isConnected)
                    return;
            }
                
            _selectedPoints.Add(pointData);
        }

        public void ClearSelection()
        {
            _selectedPoints.Clear();
            _selectedCurve = null;
        }

        public void AddPointToSelectedCurve(Vector3 position)
        {
            if (_selectedCurve == null)
                return;

            AddPointToCurve(_selectedCurve, position);
        }
    }
}
