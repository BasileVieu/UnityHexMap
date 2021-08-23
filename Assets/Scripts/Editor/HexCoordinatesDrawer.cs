using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(HexCoordinates))]
public class HexCoordinatesDrawer : PropertyDrawer
{
    public override void OnGUI(Rect _position, SerializedProperty _property, GUIContent _label)
    {
        var coordinates = new HexCoordinates(_property.FindPropertyRelative("x").intValue,
                                             _property.FindPropertyRelative("z").intValue);

        _position = EditorGUI.PrefixLabel(_position, _label);

        GUI.Label(_position, coordinates.ToString());
    }
}