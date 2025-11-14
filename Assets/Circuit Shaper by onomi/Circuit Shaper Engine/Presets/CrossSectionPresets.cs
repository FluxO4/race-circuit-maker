using System.Numerics;

namespace OnomiCircuitShaper.Engine.Presets
{
    /// <summary>
    /// A static class containing a set of predefined cross-section presets.
    /// X is the direction aligned with circuit point's right vector,
    /// Y is the direction aligned with the circuit point's up vector. For endpoints, this is always 0
    /// Z is always 0 in these presets.
    /// </summary>
    public static class CrossSectionPresets
    {




        /// <summary>
        /// A simple flat cross-section preset with two points.
        /// </summary>
        public static readonly Vector3[] FlatPreset = new Vector3[]
        {
            new Vector3(-2f, 0f, 0f),
            new Vector3(2f, 0f, 0f)
        };

        /// <summary>
        /// A triangular cross-section preset dipping downwards.
        /// </summary>
        public static readonly Vector3[] TriangularPreset = new Vector3[]
        {
            new Vector3(-2f, 0f, 0f),
            new Vector3(0f, -1f, 0f),
            new Vector3(2f, 0f, 0f)
        };

        /// <summary>
        /// A trapezoidal cross-section preset with a wider base.  
        /// </summary>
        public static readonly Vector3[] TrapezoidalPreset = new Vector3[]
        {
            new Vector3(-2.5f, 0f, 0f),
            new Vector3(-1.5f, -1f, 0f),
            new Vector3(1.5f, -1f, 0f),
            new Vector3(2.5f, 0f, 0f)
        };

        // An upward trapezoidal shape
        public static readonly Vector3[] InvertedTrapezoidalPreset = new Vector3[]
        {
            new Vector3(-2.5f, 0f, 0f),
            new Vector3(-1.5f, 1f, 0f),
            new Vector3(1.5f, 1f, 0f),
            new Vector3(2.5f, 0f, 0f)
        };
    }
}
