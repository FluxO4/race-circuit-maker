using OnomiCircuitShaper.Engine.EditRealm;

namespace OnomiCircuitShaper.Engine.Processors
{
    /// <summary>
    /// A static class containing logic for processing and modifying CircuitPoint objects.
    /// NOTE: THESE ENDED UP NOT BEING USED, SO NO NEED TO IMPLEMENT THEM YET.
    /// </summary>
    public static class CircuitPointProcessor
    {
        /// <summary>
        /// Adjusts the positions of all points in a cross-section so that the midpoint
        /// between its end points aligns with the main anchor point's position.
        /// </summary>
        public static void TransformToAlignEndPoints(CircuitPoint circuitPoint)
        {
            // To be implemented.
        }

        /// <summary>
        /// Flattens the cross-section onto the plane defined by the anchor point's orientation
        /// and rotates it to be perpendicular to the track's forward direction.
        /// </summary>
        public static void ProjectAndPerpendiculariseCrossSection(CircuitPoint circuitPoint)
        {
            // To be implemented.
        }
    }
}
