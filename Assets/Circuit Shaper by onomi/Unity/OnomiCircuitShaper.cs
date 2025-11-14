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
        /// The data
        /// </summary>
        public OnomiCircuitShaperData Data = new OnomiCircuitShaperData();
    }
}
