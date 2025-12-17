using UnityEngine;
using System.Collections.Generic;
using OnomiCircuitShaper.Engine.Processors;
using OnomiCircuitShaper.Engine.Data;

namespace OnomiCircuitShaper.Unity
{
    /// <summary>
    /// Represents a collection of waypoints for a single circuit curve.
    /// Manages the GameObject hierarchy: Waypoint Curve → Individual Waypoints
    /// </summary>
    public class SceneWaypointCurve : MonoBehaviour
    {
        /// <summary>
        /// Index of the circuit curve this waypoint curve represents.
        /// </summary>
        public int CurveIndex { get; private set; }

        /// <summary>
        /// All waypoint objects belonging to this curve.
        /// </summary>
        private List<SceneWaypoint> _waypoints = new List<SceneWaypoint>();

        /// <summary>
        /// Reference to the parent OnomiCircuitShaper component.
        /// </summary>
        public OnomiCircuitShaper ParentShaper { get; private set; }

        /// <summary>
        /// Creates waypoints for this curve from the provided waypoint data.
        /// </summary>
        public void CreateWaypoints(
            WaypointData[] waypointData,
            GameObject prefab,
            int curveIndex,
            OnomiCircuitShaper parent,
            Vector3 basePosition,
            float scale)
        {
            CurveIndex = curveIndex;
            ParentShaper = parent;

            // Clear existing waypoints
            ClearWaypoints();

            if (prefab == null)
            {
                Debug.LogWarning("SceneWaypointCurve: Prefab is null, cannot create waypoints.");
                return;
            }

            // Instantiate waypoints
            for (int i = 0; i < waypointData.Length; i++)
            {
                GameObject waypointObj = Instantiate(prefab, transform);
                waypointObj.name = $"Waypoint {i}";

                SceneWaypoint waypoint = waypointObj.GetComponent<SceneWaypoint>();
                if (waypoint == null)
                {
                    waypoint = waypointObj.AddComponent<SceneWaypoint>();
                }

                waypoint.Initialize(waypointData[i], i, this, basePosition, scale);
                _waypoints.Add(waypoint);
            }

            Debug.Log($"Created {_waypoints.Count} waypoints for curve {curveIndex}");
        }

        /// <summary>
        /// Removes all waypoint GameObjects.
        /// </summary>
        public void ClearWaypoints()
        {
            foreach (var waypoint in _waypoints)
            {
                if (waypoint != null)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(waypoint.gameObject);
                    }
                    else
                    {
                        DestroyImmediate(waypoint.gameObject);
                    }
                }
            }
            _waypoints.Clear();
        }

        private void OnDestroy()
        {
            ClearWaypoints();
        }

        private void OnDrawGizmos()
        {
            // Draw lines connecting waypoints
            if (_waypoints.Count < 2)
            {
                return;
            }

            Gizmos.color = Color.cyan;
            for (int i = 0; i < _waypoints.Count - 1; i++)
            {
                if (_waypoints[i] != null && _waypoints[i + 1] != null)
                {
                    Gizmos.DrawLine(_waypoints[i].transform.position, _waypoints[i + 1].transform.position);
                }
            }
        }
    }
}
