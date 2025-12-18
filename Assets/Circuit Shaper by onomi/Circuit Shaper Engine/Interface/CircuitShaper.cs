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
        private CircuitData _currentData;
        private CircuitAndEditorSettings _settings;
        private Circuit _liveCircuit;
        // Selection state
        private readonly List<CircuitPoint> _selectedPoints = new List<CircuitPoint>();
        private CircuitCurve _selectedCurve;
        private Road _selectedRoad;

        private SinglePointSelectionMode _singlePointSelectionMode = SinglePointSelectionMode.AnchorPoint;

        public CircuitShaper(CircuitData data, CircuitAndEditorSettings settings)
        {
            _currentData = data;
            _settings = settings;
        }

        // Expose selection state as read-only
        public IReadOnlyList<CircuitPoint> SelectedPoints => _selectedPoints.AsReadOnly();
        public CircuitCurve SelectedCurve => _selectedCurve;
        public Road SelectedRoad => _selectedRoad;

        public SinglePointSelectionMode GetSinglePointSelectionMode() => _singlePointSelectionMode;



        public Circuit GetLiveCircuit => _liveCircuit;

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

        public List<Road> GetAndClearDirtyRoads()
        {
            return RoadRebuildQueue.GetAndClearDirtyRoads();
        }

        public void ClearRoadRebuildQueue()
        {
            RoadRebuildQueue.Clear();
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

        public CircuitPoint AddPointAsNewCurve(Vector3 position)
        {
            // Create a new curve containing a single circuit point at the given position,
            // add it to the current data, and select the newly created point.

            //Create new curve and add it to the data.
            CircuitCurve curve = _liveCircuit.AddCurve();
            // Use AddPointAtIndex for empty curve since AddPointOnCurve requires existing points
            CircuitPoint newPoint = curve.AddPointAtIndex(position, 0);

            return newPoint;
        }

        public CircuitPoint AddPointAsNewCurve(Vector3 rayStart, Vector3 rayDirection)
        {
            // Create a new curve containing a single circuit point.
            // If other curves exist, find the closest point on any curve to the ray.
            // Otherwise, intersect the ray with the XZ plane at y=0.

            Vector3 pointPosition = rayStart;

            // Check if there are any existing curves
            if (_liveCircuit.Curves.Count > 0)
            {
                // Find the closest point on any existing curve segment to the ray
                float closestDistanceSqr = float.MaxValue;
                Vector3 closestPoint = rayStart;

                foreach (var existingCurve in _liveCircuit.Curves)
                {
                    if (existingCurve.Points.Count < 2) continue;

                    for (int i = 0; i < existingCurve.Points.Count - 1 + (existingCurve.IsClosed ? 1 : 0); i++)
                    {
                        var p1 = existingCurve.Points[i];
                        var p2 = existingCurve.Points[(i + 1) % existingCurve.Points.Count];

                        // Find closest points between ray and this segment
                        Vector3 segmentStart = p1.PointPosition;
                        Vector3 segmentEnd = p2.PointPosition;

                        // Simple closest point calculation (project ray onto segment)
                        Vector3 segmentDir = segmentEnd - segmentStart;
                        float segmentLength = segmentDir.Length();
                        if (segmentLength < 1e-6f) continue;
                        segmentDir = Vector3.Normalize(segmentDir);

                        // Find closest point on segment to ray origin
                        Vector3 toRayStart = rayStart - segmentStart;
                        float t = Vector3.Dot(toRayStart, segmentDir);
                        t = Math.Clamp(t, 0f, segmentLength);
                        Vector3 pointOnSegment = segmentStart + segmentDir * t;

                        // Check distance from ray to this point
                        Vector3 toPoint = pointOnSegment - rayStart;
                        float rayParam = Vector3.Dot(toPoint, rayDirection);
                        Vector3 pointOnRay = rayStart + rayDirection * Math.Max(0, rayParam);
                        
                        float distSqr = Vector3.DistanceSquared(pointOnRay, pointOnSegment);
                        if (distSqr < closestDistanceSqr)
                        {
                            closestDistanceSqr = distSqr;
                            closestPoint = pointOnSegment;
                        }
                    }
                }

                pointPosition = closestPoint;
            }
            else
            {
                // No existing curves - intersect ray with XZ plane (y=0)
                if (Math.Abs(rayDirection.Y) > 1e-6f)
                {
                    float t = -rayStart.Y / rayDirection.Y;
                    if (t > 0)
                    {
                        pointPosition = rayStart + rayDirection * t;
                    }
                }
            }

            //Create new curve and add it to the data.
            CircuitCurve curve = _liveCircuit.AddCurve();
            CircuitPoint newPoint = curve.AddPointAtIndex(pointPosition, 0);

            return newPoint;
        }

        public CircuitPoint AddPointToCurve(CircuitCurve curve, Vector3 position) => curve?.AddPointOnCurve(position);

        public CircuitPoint AddPointToCurve(CircuitCurve curve, Vector3 rayStart, Vector3 rayDirection) => curve?.AddPointOnCurve(rayStart, rayDirection);

        public CircuitPoint AddPointToSelectedCurve(Vector3 position) => AddPointToCurve(_selectedCurve, position);
    
        public CircuitPoint AddPointToSelectedCurve(Vector3 rayStart, Vector3 rayDirection) => AddPointToCurve(_selectedCurve, rayStart, rayDirection);

        public void RemoveCircuitPoint(CircuitPoint circuitPoint) => circuitPoint.CircuitCurve.RemovePoint(circuitPoint);
        
        public void DeleteSelectedPoints()
        {
            // Create a copy of the selected points list to avoid modification during iteration.
            var pointsToDelete = new List<CircuitPoint>(_selectedPoints);

            foreach (var point in pointsToDelete)
            {
                RemoveCircuitPoint(point);
            }

            // Clear selection after deletion.
            ClearSelection();
        }

        public void MoveCircuitPoint(CircuitPoint circuitPointToMove, Vector3 newPosition) => circuitPointToMove.MoveCircuitPoint(newPosition);

        public void MoveCircuitPointForwardControl(CircuitPoint circuitPointToMove, Vector3 newPosition) => circuitPointToMove.MoveForwardControlPoint(newPosition);
        
        public void MoveCircuitPointBackwardControl(CircuitPoint circuitPointToMove, Vector3 newPosition) => circuitPointToMove.MoveBackwardControlPoint(newPosition);

        public void RotateCircuitPoint(CircuitPoint circuitPoint, Vector3 eulerAngles) => circuitPoint.RotateCircuitPoint(eulerAngles);


        public void MoveCrossSectionPoint(CrossSectionPoint crossSectionPointToMove, Vector3 newPosition) => crossSectionPointToMove.MoveCrossSectionPoint(newPosition);

        public void SetCrossSectionPointCount(CrossSectionCurve crossSectionCurve, int newPointCount) => crossSectionCurve.ChangeCrossSectionPointCount(newPointCount);
        
        public void SetCrossSectionPointAutoSetTension(CrossSectionPoint crossSectionPoint, float newTension) => crossSectionPoint.SetAutoSetTension(newTension);

        public void SetCrossSectionPreset(CircuitPoint circuitPoint, Vector3[] preset)
        {
            if (circuitPoint == null) return;

            if (circuitPoint.CrossSection == null)
            {
                CrossSectionCurveData newCurveData = new CrossSectionCurveData();
                newCurveData.CurvePoints = new List<CrossSectionPointData>();
                circuitPoint.SetCrossSectionCurve(new CrossSectionCurve(newCurveData, _settings, circuitPoint));
            }

            circuitPoint.CrossSection.SetPreset(preset);
        }


        public void DeleteSelectedCurve() => _liveCircuit.DeleteCurve(_selectedCurve);
        
        public void SetSelectedCurveIsClosed(bool isClosed)
        {
            if (_selectedCurve == null) return;

            _selectedCurve.IsClosed = isClosed;
        }


        public void CreateNewRoadFromPoints(CircuitPoint[] points)
        {
            //Compute the point index range in the selected curve.
            if (points == null || points.Length < 2) return;

            int startIndex = _selectedCurve.Points.IndexOf(points[0]);
            int endIndex = _selectedCurve.Points.IndexOf(points[^1]);

            _selectedCurve.AddRoadFromRange(startIndex, endIndex);
        }

        public void CreateRoadFromSelectedPoints()
        {
            if (_selectedPoints.Count < 2) return;
            CreateNewRoadFromPoints(_selectedPoints.ToArray());
        }

        public void RemoveRoad(Road road) => road.parentCurve.RemoveRoad(road);

        // Selection manipulation implementations
        public void SelectPoint(CircuitPoint point)
        {
            if (point == null)
                return;

            _selectedPoints.Clear();
            _singlePointSelectionMode = SinglePointSelectionMode.AnchorPoint;
            _selectedPoints.Add(point);

            //Also set the selected curve if this point belongs to one.
            _selectedCurve = point.CircuitCurve;
            
            // Note: Road selection is managed in Unity editor layer
        }

        public void DeselectPoint(CircuitPoint point)
        {
            if (point == null)
                return;

            // We can only deselect from the beginning or end of the selection.
            if (_selectedPoints.Count == 0)
                return;
            if (_selectedPoints[0] != point && _selectedPoints[^1] != point)
                return;

            _selectedPoints.Remove(point);
        }

        public void AddPointToSelection(CircuitPoint point)
        {
            if (point == null)
                return;

            if (_selectedPoints.Contains(point))
                return;

            //We should use SelectPoint to select the first point.
            if (_selectedPoints.Count == 0)
            {
                SelectPoint(point);
                return;
            }

            //Return if trying to add a point from a different curve than the selected one.
            if (_selectedCurve != null && _selectedCurve != point.CircuitCurve)
            {
                return;
            }

            //We can only select points connected to any already selected border point.
            if (_selectedPoints[^1].NextPoint == point ||
                _selectedPoints[0].PreviousPoint == point)
            {
                bool connectedAtBeginning = _selectedPoints[0].PreviousPoint == point;
                if (connectedAtBeginning)
                {
                    _selectedPoints.Insert(0, point);

                }
                else
                {

                    _selectedPoints.Add(point);
                }
                _singlePointSelectionMode = SinglePointSelectionMode.AnchorPoint;
            }
        }

        public void ClearSelection()
        {
            _selectedPoints.Clear();
            _selectedCurve = null;
            _singlePointSelectionMode = SinglePointSelectionMode.AnchorPoint;
        }

        public void SelectRoad(Road road)
        {
            _selectedRoad = road;
        }

        public void DeselectRoad()
        {
            _selectedRoad = null;
        }

        public void SetSinglePointSelectionMode(SinglePointSelectionMode mode)
        {
            _singlePointSelectionMode = mode;
        }

        public bool TrySetRoadStartSegment(Road road, int newStartSegment)
        {
            return road?.parentCurve?.TrySetRoadStartSegment(road, newStartSegment) ?? false;
        }

        public bool TrySetRoadEndSegment(Road road, int newEndSegment)
        {
            return road?.parentCurve?.TrySetRoadEndSegment(road, newEndSegment) ?? false;
        }

        public void SetRoadBridgeEnabled(Road road, bool enabled)
        {
            if (road == null) return;

            road.EnableBridge(enabled);
            road.parentCurve?.OnCurveStateChanged();
            RoadRebuildQueue.MarkDirty(road);
        }

        public void AddRailingToRoad(Road road)
        {
            if (road == null) return;

            if (road.Data.Railings == null)
            {
                road.Data.Railings = new List<RailingData>();
            }

            road.Data.Railings.Add(new RailingData());
            road.RebuildRailingsWrappers();
            road.parentCurve?.OnCurveStateChanged();
            RoadRebuildQueue.MarkDirty(road);
        }

        public void RemoveRailingFromRoad(Road road, int index)
        {
            if (road == null || road.Data.Railings == null) return;
            if (index < 0 || index >= road.Data.Railings.Count) return;

            road.Data.Railings.RemoveAt(index);
            road.RebuildRailingsWrappers();
            road.parentCurve?.OnCurveStateChanged();
            RoadRebuildQueue.MarkDirty(road);
        }
    }
}
