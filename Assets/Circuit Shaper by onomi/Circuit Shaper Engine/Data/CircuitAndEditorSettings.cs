namespace OnomiCircuitShaper.Engine.Data
{
    /// <summary>
    /// A container for global settings related to the circuit and the editor's behavior.
    /// This class is intended to be part of the Integration Layer, likely managed by a ScriptableObject,
    /// to store user preferences and editor state.
    /// </summary>
    [System.Serializable]
    public class CircuitAndEditorSettings
    {
        /// <summary>
        /// A global scale multiplier that affects various editor gizmos and handles.
        /// </summary>
        public float ScaleMultiplier = 1.0f;

        /// <summary>
        /// If true, control points should be automatically adjusted when an anchor point is moved.
        /// This provides a smoother, more intuitive editing experience for beginners.
        /// </summary>
        public bool AutoSetControlPoints = true;
    }
}
