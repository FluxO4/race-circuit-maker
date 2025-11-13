using System;
using System.Numerics;

namespace OnomiCircuitShaper.Engine.Data
{
    /// <summary>
    /// A serializable wrapper for System.Numerics.Vector3 to allow for Unity serialization.
    /// </summary>
    [Serializable]
    public struct SerializableVector3
    {
        public float x;
        public float y;
        public float z;

        // Passthrough properties
        public float Length => new Vector3(x, y, z).Length();
        public float LengthSquared => new Vector3(x, y, z).LengthSquared();

        // Constructors
        public SerializableVector3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        // Implicit conversions
        public static implicit operator Vector3(SerializableVector3 v)
        {
            return new Vector3(v.x, v.y, v.z);
        }

        public static implicit operator SerializableVector3(Vector3 v)
        {
            return new SerializableVector3 { x = v.X, y = v.Y, z = v.Z };
        }

        // Passthrough static methods
        public static SerializableVector3 Normalize(SerializableVector3 v)
        {
            return Vector3.Normalize(v);
        }

        // Operator overloads
        public static SerializableVector3 operator +(SerializableVector3 a, SerializableVector3 b)
        {
            return new SerializableVector3(a.x + b.x, a.y + b.y, a.z + b.z);
        }

        public static SerializableVector3 operator -(SerializableVector3 a, SerializableVector3 b)
        {
            return new SerializableVector3(a.x - b.x, a.y - b.y, a.z - b.z);
        }

        public static SerializableVector3 operator *(SerializableVector3 a, float d)
        {
            return new SerializableVector3(a.x * d, a.y * d, a.z * d);
        }

        public override string ToString()
        {
            return $"({x}, {y}, {z})";
        }
    }

    /// <summary>
    /// A serializable wrapper for System.Numerics.Vector2 to allow for Unity serialization.
    /// </summary>
    [Serializable]
    public struct SerializableVector2
    {
        public float x;
        public float y;

        // Passthrough properties
        public float Length => new Vector2(x, y).Length();
        public float LengthSquared => new Vector2(x, y).LengthSquared();

        // Constructors
        public SerializableVector2(float x, float y)
        {
            this.x = x;
            this.y = y;
        }

        // Implicit conversions
        public static implicit operator Vector2(SerializableVector2 v)
        {
            return new Vector2(v.x, v.y);
        }

        public static implicit operator SerializableVector2(Vector2 v)
        {
            return new SerializableVector2 { x = v.X, y = v.Y };
        }

        // Operator overloads
        public static SerializableVector2 operator +(SerializableVector2 a, SerializableVector2 b)
        {
            return new SerializableVector2(a.x + b.x, a.y + b.y);
        }

        public static SerializableVector2 operator -(SerializableVector2 a, SerializableVector2 b)
        {
            return new SerializableVector2(a.x - b.x, a.y - b.y);
        }

        public static SerializableVector2 operator *(SerializableVector2 a, float d)
        {
            return new SerializableVector2(a.x * d, a.y * d);
        }

        public override string ToString()
        {
            return $"({x}, {y})";
        }
    }
}