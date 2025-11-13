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
        /// Strongly-typed to concrete curve implementations to match the EditRealm generics.
        /// </summary>
        public Dictionary<CircuitCurveData, CircuitCurve> Curves { get; private set; } = new Dictionary<CircuitCurveData, CircuitCurve>();

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
            //instantiate all the live EditRealm objects.

            //Clear existing data
            Curves.Clear();
            Roads.Clear();

            // Instantiate Curves
            foreach (CircuitCurveData curveData in Data.CircuitCurves)
            {
                CircuitCurve curve = new CircuitCurve(curveData, Settings);
                // For each curve, instantiate its points
                foreach (CircuitPointData circuitPointData in curveData.CurvePoints)
                {
                    // For each point, set up its cross section curve
                    CrossSectionCurve crossSectionCurve = new CrossSectionCurve(circuitPointData.CrossSectionCurve, Settings);

                    // Instantiate cross-section points
                    foreach (CrossSectionPointData csPointData in circuitPointData.CrossSectionCurve.CurvePoints)
                    {
                        CrossSectionPoint csPoint = new CrossSectionPoint(csPointData, Settings, null);
                        crossSectionCurve.Points.Add(csPointData, csPoint);
                    }

                    // Create the live circuit point and wire it to its cross-section
                    CircuitPoint point = new CircuitPoint(circuitPointData, Settings, crossSectionCurve);
                    curve.Points.Add(circuitPointData, point);
                }

                Curves.Add(curveData, curve);
            }
            //Instantiate Roads
            foreach (var roadData in Data.CircuitRoads)
            {
                Road road = new Road(roadData, Settings, this);
                Roads.Add(roadData, road);
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
            if (!Curves.ContainsKey(curveData))
            {
                CircuitCurve curve = new CircuitCurve(curveData, Settings);
                // For each curve, instantiate its points
                foreach (CircuitPointData circuitPointData in curveData.CurvePoints)
                {
                    // For each point, set up its cross section curve
                    CrossSectionCurve crossSectionCurve = new CrossSectionCurve(circuitPointData.CrossSectionCurve, Settings);

                    // Instantiate cross-section points
                    foreach (CrossSectionPointData csPointData in circuitPointData.CrossSectionCurve.CurvePoints)
                    {
                        CrossSectionPoint csPoint = new CrossSectionPoint(csPointData, Settings, null);
                        crossSectionCurve.Points.Add(csPointData, csPoint);
                    }

                    // Create the live circuit point and wire it to its cross-section
                    CircuitPoint point = new CircuitPoint(circuitPointData, Settings, crossSectionCurve);
                    curve.Points.Add(circuitPointData, point);
                }

                Curves.Add(curveData, curve);
                return curve;
            }
            return null;
        }

        // Function for adding new road from RoadData can be added here.
        public void AddRoad(RoadData roadData)
        {
            if (!Roads.ContainsKey(roadData))
            {
                Road road = new Road(roadData, Settings, this);
                Roads.Add(roadData, road);
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
