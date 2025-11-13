using OnomiCircuitShaper.Engine.Data;

namespace OnomiCircuitShaper.Engine.Data
{
    /// <summary>
    /// A top-level container designed to hold all data required for the circuit shaper.
    /// This can be easily serialized to and from JSON to save/load entire track layouts
    /// and their associated editor settings.
    /// </summary>
    [System.Serializable]
    public class OnomiCircuitShaperData
    {
        /// <summary>
        /// The core data defining the geometry and structure of the race circuit.
        /// </summary>
        public CircuitData circuitData = new CircuitData();

        /// <summary>
        /// The settings related to the editor's behavior and gizmo appearance.
        /// </summary>
        public CircuitAndEditorSettings settingsData = new CircuitAndEditorSettings();
    }
}
