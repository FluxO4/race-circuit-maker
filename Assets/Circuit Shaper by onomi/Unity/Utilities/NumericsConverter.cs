using UnityEngine;
using OnomiCircuitShaper.Engine.Data;


namespace OnomiCircuitShaper.Unity.Utilities
{
    /// <summary>
    /// A utility class for converting between Unity's Vector types and System.Numerics.Vector types.
    /// This is useful for interoperability between Unity and other libraries that use System.Numerics.
    /// </summary>
    public static class NumericsConverter
    {
        /// <summary>
        /// Converts a UnityEngine.Vector3 to a System.Numerics.Vector3.
        /// </summary>
        public static System.Numerics.Vector3 ToNumericsVector3(this Vector3 unityVector)
        {
            return new System.Numerics.Vector3(unityVector.x, unityVector.y, unityVector.z);
        }

        /// <summary>
        /// Converts a System.Numerics.Vector3 to a UnityEngine.Vector3.
        /// </summary>
        public static Vector3 ToUnityVector3(this System.Numerics.Vector3 numericsVector)
        {
            return new Vector3(numericsVector.X, numericsVector.Y, numericsVector.Z);
        }

        /// <summary>
        /// Converts a UnityEngine.Vector2 to a System.Numerics.Vector2.
        /// </summary>
        public static System.Numerics.Vector2 ToNumericsVector2(this Vector2 unityVector)
        {
            return new System.Numerics.Vector2(unityVector.x, unityVector.y);
        }

        /// <summary>
        /// Converts a System.Numerics.Vector2 to a UnityEngine.Vector2.
        /// </summary>
        public static Vector2 ToUnityVector2(this System.Numerics.Vector2 numericsVector)
        {
            return new Vector2(numericsVector.X, numericsVector.Y);
        }


        //Make the same for SerializableVector3 and SerializableVector2
        /// <summary>
        /// Converts a UnityEngine.Vector3 to a SerializableVector3.
        /// </summary>
        public static SerializableVector3 ToSerializableVector3(this Vector3 unityVector)
        {
            return new SerializableVector3(unityVector.x, unityVector.y, unityVector.z);
        }

        /// <summary>
        /// Converts a UnityEngine.Vector2 to a SerializableVector2.
        /// </summary>
        public static SerializableVector2 ToSerializableVector2(this Vector2 unityVector)
        {
            return new SerializableVector2(unityVector.x, unityVector.y);
        }

        /// <summary>
        /// Converts a SerializableVector3 to a UnityEngine.Vector3.
        /// </summary>
        public static Vector3 ToUnityVector3(this SerializableVector3 serializableVector)
        {
            return new Vector3(serializableVector.x, serializableVector.y, serializableVector.z);
        }

        /// <summary>
        /// Converts a SerializableVector2 to a UnityEngine.Vector2.
        /// </summary>
        public static Vector2 ToUnityVector2(this SerializableVector2 serializableVector)
        {
            return new Vector2(serializableVector.x, serializableVector.y);
        }

        /// <summary>
        /// Transforms a System.Numerics.Vector3 to global space by applying scale and offset.
        /// </summary>
        public static Vector3 ToGlobalSpace(this System.Numerics.Vector3 localPosition, Vector3 basePosition, float scale)
        {
            return new Vector3(
                localPosition.X * scale + basePosition.x,
                localPosition.Y * scale + basePosition.y,
                localPosition.Z * scale + basePosition.z
            );
        }

        /// <summary>
        /// Transforms a Unity Vector3 from global space back to local space by removing offset and scale.
        /// </summary>
        public static System.Numerics.Vector3 ToLocalSpace(this Vector3 globalPosition, Vector3 basePosition, float scale)
        {
            if (scale == 0) scale = 1; // Prevent division by zero
            return new System.Numerics.Vector3(
                (globalPosition.x - basePosition.x) / scale,
                (globalPosition.y - basePosition.y) / scale,
                (globalPosition.z - basePosition.z) / scale
            );
        }

        /// <summary>
        /// Converts a System.Numerics.Quaternion to a UnityEngine.Quaternion.
        /// </summary>
        public static Quaternion ToUnity(this System.Numerics.Quaternion numericsQuaternion)
        {
            return new Quaternion(
                numericsQuaternion.X,
                numericsQuaternion.Y,
                numericsQuaternion.Z,
                numericsQuaternion.W
            );
        }

        /// <summary>
        /// Converts a UnityEngine.Quaternion to a System.Numerics.Quaternion.
        /// </summary>
        public static System.Numerics.Quaternion ToNumerics(this Quaternion unityQuaternion)
        {
            return new System.Numerics.Quaternion(
                unityQuaternion.x,
                unityQuaternion.y,
                unityQuaternion.z,
                unityQuaternion.w
            );
        }

        /// <summary>
        /// Shortened ToUnity method for Vector3.
        /// </summary>
        public static Vector3 ToUnity(this System.Numerics.Vector3 numericsVector)
        {
            return ToUnityVector3(numericsVector);
        }

        /// <summary>
        /// Shortened ToUnity method for Vector2.
        /// </summary>
        public static Vector2 ToUnity(this System.Numerics.Vector2 numericsVector)
        {
            return ToUnityVector2(numericsVector);
        }
    }
}