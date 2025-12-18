using OnomiCircuitShaper.Engine.Data;
using System.Collections.Generic;
using System.Numerics;

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
        /// A list of live, editable Curve objects that mirror `CircuitData.CircuitCurves`.
        /// Keep this list in the same order as the data list for easy indexing and syncing.
        /// </summary>
        public List<CircuitCurve> Curves { get; private set; } = new List<CircuitCurve>();


        /// <summary>
        /// Initializes the editing session. It populates the dictionaries with live
        /// wrapper objects (Curves, Roads, Points) based on the provided raw data.
        /// </summary>
        public void BeginEditFromData(CircuitData circuitData, CircuitAndEditorSettings settings)
        {
            this.Data = circuitData;
            this.Settings = settings;
            //instantiate all the live EditRealm objects.

            //Clear existing data
            Curves.Clear();

            // Instantiate Curves
            foreach (CircuitCurveData curveData in Data.CircuitCurves)
            {
                CircuitCurve curve = new CircuitCurve(curveData, Settings);
                Curves.Add(curve);
            }
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
        }


        //Function for adding new curve from CircuitCurveData, and new road from RoadData can be added here.
        public CircuitCurve AddCurve(CircuitCurveData curveData = null)
        {
            if (curveData == null)
            {
                curveData = new CircuitCurveData();
            }
            if (!Curves.Exists(c => object.ReferenceEquals(c.Data, curveData)))
            {
                CircuitCurve curve = new CircuitCurve(curveData, Settings);
                
                Curves.Add(curve);
                // Add to persistent data
                if (Data != null && !Data.CircuitCurves.Contains(curveData))
                {
                    Data.CircuitCurves.Add(curveData);
                }
                return curve;
            }
            return null;
        }


        // Function for deleting curve
        public void DeleteCurve(CircuitCurve curve)
        {
            if (Curves.Contains(curve))
            {
                Curves.Remove(curve);
                // Also remove from data
                if (Data != null && Data.CircuitCurves.Contains(curve.Data))
                {
                    Data.CircuitCurves.Remove(curve.Data);
                }
            }
        }
    }
}
