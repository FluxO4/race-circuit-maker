using OnomiCircuitShaper.Engine.Data;

namespace OnomiCircuitShaper.Engine.EditRealm
{
    /// <summary>
    /// Represents a live, editable railing object. It wraps the <see cref="RailingData"/>
    /// and exists as part of a <see cref="Road"/> during an edit session, providing an
    /// interactive handle to the railing's properties.
    /// Railings are positioned along the edges of roads at specified horizontal positions.
    /// </summary>
    /// <remarks>
    /// [Look here onomi] Railings are generated as simple vertical extrusions along the road path.
    /// The HorizontalPosition property (0 to 1) determines where along the cross-section width the railing is placed.
    /// Min and Max properties define the start and end points along the road's length (normalized 0-1).
    /// RailingHeight controls the vertical extent of the railing mesh.
    /// </remarks>
    public class Railing
    {
        /// <summary>
        /// The raw, serializable data for this railing. Changes made to the live
        /// <see cref="Railing"/> object are stored here.
        /// Contains positioning, height, and length parameters for railing generation.
        /// </summary>
        public RailingData Data { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Railing"/> class.
        /// </summary>
        /// <param name="data">The <see cref="RailingData"/> to be managed by this edit-realm object.</param>
        public Railing(RailingData data)
        {
            Data = data;
        }
    }
}
