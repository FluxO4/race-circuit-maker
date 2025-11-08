using OnomiCircuitShaper.Engine.Data;

namespace OnomiCircuitShaper.Engine.EditRealm
{
    /// <summary>
    /// Represents a live, editable bridge object. It wraps the BridgeData
    /// and exists as part of a Road during an edit session.
    /// </summary>
    public class Bridge
    {
        /// <summary>
        /// The raw, underlying data for this bridge.
        /// </summary>
        public BridgeData Data { get; private set; }

        /// <summary>
        /// Initializes a new instance of the Bridge class.
        /// </summary>
        /// <param name="data">The data to wrap.</param>
        public Bridge(BridgeData data)
        {
            Data = data;
        }
    }
}
