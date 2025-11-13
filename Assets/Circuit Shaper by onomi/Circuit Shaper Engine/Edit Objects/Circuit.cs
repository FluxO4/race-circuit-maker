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
        /// A list of live, editable Road objects that mirror `CircuitData.CircuitRoads`.
        /// </summary>
        public List<Road> Roads { get; private set; } = new List<Road>();


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
            Roads.Clear();

            // Instantiate Curves
            foreach (CircuitCurveData curveData in Data.CircuitCurves)
            {
                CircuitCurve curve = new CircuitCurve(curveData, Settings);
                Curves.Add(curve);
            }
            //Instantiate Roads
            foreach (var roadData in Data.CircuitRoads)
            {
                Road road = new Road(roadData, Settings, this);
                Roads.Add(road);
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
            Roads.Clear();
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
                return curve;
            }
            return null;
        }

        // Function for adding new road from RoadData can be added here.
        public void AddRoad(RoadData roadData)
        {
            if (!Roads.Exists(r => object.ReferenceEquals(r.Data, roadData)))
            {
                Road road = new Road(roadData, Settings, this);
                // Ensure data list contains this road
                if (Data != null)
                {
                    if (Data.CircuitRoads == null)
                        Data.CircuitRoads = new List<RoadData>();
                    if (!Data.CircuitRoads.Contains(roadData))
                        Data.CircuitRoads.Add(roadData);
                }
                Roads.Add(road);
            }
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
