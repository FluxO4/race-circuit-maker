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

                /// <summary>
        /// If true, the forward and backward control points of a point can be moved
        /// independently. If false, moving one will mirror the movement in the other
        /// to maintain a smooth curve. This is a global default.
        /// </summary>
        public bool IndependentControlPoints = false;

        /// <summary>
        /// The distance from the anchor point at which the "roll" or "twist" rotator
        /// handle should be drawn in the editor.
        /// </summary>
        public float RotatorPointDistance = 2.0f;


        // free move gizmo settings
        /// <summary>
        /// The size of the free move gizmo handles in the editor.
        /// </summary>
        public float FreeMoveGizmoHandleSizeFactor = 1.0f;

        /// <summary>
        /// Whether the handles should be free move or selectable.
        /// </summary>
        public bool FreeMoveMode = true;

        /// <summary>
        /// If true, road GameObjects will be hidden in the Unity hierarchy.
        /// </summary>
        public bool HideRoadsInHierarchy = false;
    }
}
