using OnomiCircuitShaper.Engine.Data;
using System.Collections.Generic;
using UnityEngine;

namespace OnomiCircuitShaper.Unity
{
    /// <summary>
    /// The main MonoBehaviour that acts as the bridge between the Unity scene and the
    /// engine-agnostic Circuit Shaper core. It holds the data and settings and manages
    /// the scene objects that represent the track.
    /// </summary>
    public class OnomiCircuitShaper : MonoBehaviour
    {

        public bool freeze = false;


        /// <summary>
        /// The data
        /// </summary>
        public OnomiCircuitShaperData Data = new OnomiCircuitShaperData();




        // A list of Road materials
        public List<Material> RoadMaterials = new List<Material>();


        // A list of Railing materials
        public List<Material> RailingMaterials = new List<Material>();

        // A list of Bridge materials
        public List<Material> BridgeMaterials = new List<Material>();

        // Physics materials for roads, railings, and bridges
        public List<PhysicMaterial> RoadPhysicsMaterials = new List<PhysicMaterial>();
        public List<PhysicMaterial> RailingPhysicsMaterials = new List<PhysicMaterial>();
        public List<PhysicMaterial> BridgePhysicsMaterials = new List<PhysicMaterial>();

        // Waypoint generation settings
        public WaypointSettings WaypointSettings = new WaypointSettings();

        // Prefab to use for waypoint instantiation (should be a 1m cube or similar)
        public GameObject WaypointPrefab;

        // List of waypoint curve objects in the scene
        [HideInInspector]
        public List<SceneWaypointCurve> WaypointCurves = new List<SceneWaypointCurve>();
    }
}
