using OnomiCircuitShaper.Engine.Data;
using OnomiCircuitShaper.Engine.EditRealm;
using OnomiCircuitShaper.Engine.Processors;
using System;
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

        public CircuitShaper(CircuitData data, CircuitAndEditorSettings settings)
        {
            _currentData = data;
            _settings = settings;
        }

        public void BeginEdit()
        {
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
            // To be implemented.
        }

        public void AddPointToCurve(CurveData curveData, Vector3 position)
        {
            // To be implemented.
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

        public void CreateNewRoadFromPoints(PointData[] pointData)
        {
            // To be implemented.
        }

        public void RemoveRoad(RoadData roadData)
        {
            // To be implemented.
        }
    }
}
