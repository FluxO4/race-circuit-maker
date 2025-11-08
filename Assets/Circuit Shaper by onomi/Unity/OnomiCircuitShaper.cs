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
        /// <summary>
        /// The editor and circuit settings asset.
        /// </summary>
        public CircuitAndEditorSettings CircuitSettings;

        /// <summary>
        /// The raw data for the circuit being edited or displayed.
        /// </summary>
        public CircuitData CircuitData;

        /// <summary>
        /// A dictionary that maps the raw RoadData to the live Unity GameObjects
        /// that are rendering the road meshes in the scene.
        /// </summary>
        public Dictionary<RoadData, SceneRoad> SceneRoads = new Dictionary<RoadData, SceneRoad>();
    }
}
