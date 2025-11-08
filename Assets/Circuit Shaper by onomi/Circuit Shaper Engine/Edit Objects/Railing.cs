using OnomiCircuitShaper.Engine.Data;

namespace OnomiCircuitShaper.Engine.EditRealm
{
    /// <summary>
    /// Represents a live, editable railing object. It wraps the RailingData
    /// and exists as part of a Road during an edit session.
    /// </summary>
    public class Railing
    {
        /// <summary>
        /// The raw, underlying data for this railing.
        /// </summary>
        public RailingData Data { get; private set; }

        /// <summary>
        /// Initializes a new instance of the Railing class.
        /// </summary>
        /// <param name="data">The data to wrap.</param>
        public Railing(RailingData data)
        {
            Data = data;
        }
    }
}
