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
            
            // Reconnect roads to their live CircuitPoint objects and subscribe to events
            ReconnectRoadsToPoints();
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


        /// <summary>
        /// Triggers a rebuild of all roads and their associated meshes.
        /// </summary>
        public void BuildAll()
        {
            // To be implemented.
        }

        /// <summary>
        /// Reconnects roads to their live CircuitPoint objects and establishes road associations.
        /// This must be called after roads are instantiated from data to establish live references.
        /// Uses the queue-based system instead of event subscriptions.
        /// </summary>
        public void ReconnectRoadsToPoints()
        {
            if (Roads == null) return;
            
            // First, clear all existing road associations from all points
            foreach (var curve in Curves)
            {
                foreach (var point in curve.Points)
                {
                    point.AssociatedRoads.Clear();
                }
            }
            
            // Then, rebuild associations for each road
            foreach (var road in Roads)
            {
                if (road.Data?.AssociatedPoints == null || road.Data.AssociatedPoints.Count < 2)
                {
                    continue;
                }
                
                // Find all live points for this road
                var livePoints = new List<CircuitPoint>();
                bool allPointsFound = true;
                
                foreach (var pointData in road.Data.AssociatedPoints)
                {
                    CircuitPoint livePoint = FindPointByData(pointData);
                    if (livePoint != null)
                    {
                        livePoints.Add(livePoint);
                        // Add this road to the point's association list
                        livePoint.AddRoadAssociation(road.Data);
                    }
                    else
                    {
                        allPointsFound = false;
                        UnityEngine.Debug.LogWarning($"[Circuit] Could not find live point for RoadData association");
                        break;
                    }
                }
                
                // Mark for initial rebuild if all points were found
                if (allPointsFound && livePoints.Count >= 2)
                {
                    RoadRebuildQueue.MarkDirty(road.Data);
                }
            }
        }

        /// <summary>
        /// Finds a live CircuitPoint object that wraps the given CircuitPointData.
        /// </summary>
        private CircuitPoint FindPointByData(CircuitPointData pointData)
        {
            foreach (var curve in Curves)
            {
                foreach (var point in curve.Points)
                {
                    if (object.ReferenceEquals(point.Data, pointData))
                    {
                        return point;
                    }
                }
            }
            return null;
        }
    }
}
