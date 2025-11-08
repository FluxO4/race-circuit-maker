using OnomiCircuitShaper.Engine.Data;
using System.Collections.Generic;

namespace OnomiCircuitShaper.Engine.EditRealm
{
    /// <summary>
    /// The top-level object for an active editing session. It creates, holds, and manages all the
    /// live "Edit Realm" objects (Curves, Roads, etc.) based on the raw CircuitData.
    /// </summary>
    public class Circuit
    {
        /// <summary>
        /// The raw circuit data being edited.
        /// </summary>
        public CircuitData Data { get; private set; }

        /// <summary>
        /// The settings for the current edit session.
        /// </summary>
        public CircuitAndEditorSettings Settings { get; private set; }

        /// <summary>
        /// A dictionary mapping the raw CurveData to the live, editable Curve objects.
        /// </summary>
        public Dictionary<CurveData, Curve> Curves { get; private set; } = new Dictionary<CurveData, Curve>();

        /// <summary>
        /// A dictionary mapping the raw RoadData to the live, editable Road objects.
        /// </summary>
        public Dictionary<RoadData, Road> Roads { get; private set; } = new Dictionary<RoadData, Road>();

        /// <summary>
        /// Initializes the editing session. It populates the dictionaries with live
        /// wrapper objects (Curves, Roads, Points) based on the provided raw data.
        /// </summary>
        public void BeginEditFromData(CircuitData circuitData, CircuitAndEditorSettings settings)
        {
            this.Data = circuitData;
            this.Settings = settings;
            // Implementation will instantiate all the live EditRealm objects.
        }

        /// <summary>
        /// Ends the editing session, clearing all live data. The modified raw data
        /// persists in the CircuitData object.
        /// </summary>
        public void EndEdit()
        {
            Data = null;
            Settings = null;
            Curves.Clear();
            Roads.Clear();
        }

        /// <summary>
        /// Triggers a rebuild of all roads and their associated meshes.
        /// </summary>
        public void BuildAll()
        {
            // To be implemented.
        }
    }
}
