using OnomiCircuitShaper.Engine.Data;

namespace OnomiCircuitShaper.Engine.EditRealm
{
    /// <summary>
    /// Represents a live, editable bridge object within an active editing session.
    /// It wraps the <see cref="BridgeData"/> and provides an interactive handle
    /// to the bridge's properties as part of a <see cref="Road"/> object.
    /// This class exists only during edit mode and manages bridge geometry generation.
    /// </summary>
    /// <remarks>
    /// [Look here onomi] Bridges can be generated using templates (I-beam style) or custom shapes.
    /// The UseTemplate property in BridgeData determines which generation method is used.
    /// When using custom shapes, BridgeShapePoints defines the 2D profile that gets extruded along the road path.
    /// </remarks>
    public class Bridge
    {
        /// <summary>
        /// The raw, serializable data for this bridge. All changes made to the
        /// live <see cref="Bridge"/> object should be reflected in this data.
        /// Contains geometry parameters like template settings, UV mapping, and custom shape points.
        /// </summary>
        public BridgeData Data { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Bridge"/> class, wrapping the provided data.
        /// </summary>
        /// <param name="data">The <see cref="BridgeData"/> to be managed by this edit-realm object.</param>
        public Bridge(BridgeData data)
        {
            Data = data;
        }
    }
}
