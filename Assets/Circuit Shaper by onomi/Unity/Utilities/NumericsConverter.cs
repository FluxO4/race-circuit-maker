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
    }
}