using OnomiCircuitShaper.Engine.Data;

namespace OnomiCircuitShaper.Engine.Data
{
    /// <summary>
    /// A top-level container designed to hold all data required for the circuit shaper.
    /// This class is the root object for serialization. An instance of this class can be
    /// easily serialized to and from JSON (or another format) to save or load an entire
    /// track layout and its associated editor settings.
    /// </summary>
    [System.Serializable]
    public class OnomiCircuitShaperData
    {
        /// <summary>
        /// The core data defining the geometry and structure of the race circuit.
        /// See <see cref="CircuitData"/> for more details.
        /// </summary>
        public CircuitData circuitData = new CircuitData();

        /// <summary>
        /// The settings related to the editor's behavior and gizmo appearance.
        /// See <see cref="CircuitAndEditorSettings"/> for more details.
        /// </summary>
        public CircuitAndEditorSettings settingsData = new CircuitAndEditorSettings();
    }
}
