using OnomiCircuitShaper.Engine.Data;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom Property Drawer for the SerializableVector3 wrapper.
/// This allows System.Numerics.Vector3 to be edited in the Unity Inspector.
/// </summary>
[CustomPropertyDrawer(typeof(SerializableVector3))]
public class SerializableVector3Drawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        // Find the serialized fields within our wrapper struct
        var x = property.FindPropertyRelative("x");
        var y = property.FindPropertyRelative("y");
        var z = property.FindPropertyRelative("z");

        // Create a temporary UnityEngine.Vector3 for the editor field
        var vec = new Vector3(x.floatValue, y.floatValue, z.floatValue);

        // Draw the field and update the values
        EditorGUI.BeginChangeCheck();
        vec = EditorGUI.Vector3Field(position, label, vec);
        if (EditorGUI.EndChangeCheck())
        {
            x.floatValue = vec.x;
            y.floatValue = vec.y;
            z.floatValue = vec.z;
        }

        EditorGUI.EndProperty();
    }
}

/// <summary>
/// Custom Property Drawer for the SerializableVector2 wrapper.
/// This allows System.Numerics.Vector2 to be edited in the Unity Inspector.
/// </summary>
[CustomPropertyDrawer(typeof(SerializableVector2))]
public class SerializableVector2Drawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        var x = property.FindPropertyRelative("x");
        var y = property.FindPropertyRelative("y");

        var vec = new Vector2(x.floatValue, y.floatValue);

        EditorGUI.BeginChangeCheck();
        vec = EditorGUI.Vector2Field(position, label, vec);
        if (EditorGUI.EndChangeCheck())
        {
            x.floatValue = vec.x;
            y.floatValue = vec.y;
        }

        EditorGUI.EndProperty();
    }
}