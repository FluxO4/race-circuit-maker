using UnityEngine;
using OnomiCircuitShaper.Unity.Utilities;
using OnomiCircuitShaper.Engine.Processors;

namespace OnomiCircuitShaper.Unity
{
    /// <summary>
    /// Represents a single waypoint GameObject in the Unity scene.
    /// Waypoints are instantiated from a prefab and scaled/rotated to match the road geometry.
    /// </summary>
    public class SceneWaypoint : MonoBehaviour
    {
        /// <summary>
        /// Index of this waypoint within its parent curve.
        /// </summary>
        public int WaypointIndex { get; private set; }

        /// <summary>
        /// Reference to the parent waypoint curve.
        /// </summary>
        public SceneWaypointCurve ParentCurve { get; private set; }

        /// <summary>
        /// Initializes the waypoint with transform data.
        /// </summary>
        public void Initialize(WaypointData data, int index, SceneWaypointCurve parent, Vector3 basePosition, float scale)
        {
            WaypointIndex = index;
            ParentCurve = parent;
            
            // Apply transform with scale and offset
            // Convert position to Unity global space
            transform.position = data.Position.ToGlobalSpace(basePosition, scale);
            transform.rotation = NumericsConverter.ToUnity(data.Rotation);
            // Scale the waypoint scale by the global scale multiplier
            Vector3 waypointScale = NumericsConverter.ToUnity(data.Scale);
            transform.localScale = new Vector3(waypointScale.x * scale, waypointScale.y * scale, waypointScale.z * scale);
            
            // Debug: Log rotation for first few waypoints
            if (index < 3)
            {
                Debug.Log($"Waypoint {index} - Rotation: {transform.rotation.eulerAngles}, Forward: {transform.forward}");
            }
        }

        private void OnDrawGizmos()
        {
            // Draw waypoint as a wire cube
            Gizmos.color = Color.cyan;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        }

        private void OnDrawGizmosSelected()
        {
            // Draw selected waypoint with different color
            Gizmos.color = Color.yellow;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
            
            // Draw arrow showing forward direction
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, transform.forward * 2f);
        }
    }
}
