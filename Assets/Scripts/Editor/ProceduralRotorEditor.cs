using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ProceduralRotor))]
public class ProceduralRotorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        ProceduralRotor rotor = (ProceduralRotor)target;
        
        serializedObject.Update();

        // Mesh Reference
        EditorGUILayout.PropertyField(serializedObject.FindProperty("meshFilter"));
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Blade Settings", EditorStyles.boldLabel);
        
        // Basic blade settings
        EditorGUILayout.PropertyField(serializedObject.FindProperty("numberOfBlades"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("bladeLength"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("bladeWidth"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("bladeThickness"));
        
        // Blade shape
        SerializedProperty bladeShapeProperty = serializedObject.FindProperty("bladeShape");
        EditorGUILayout.PropertyField(bladeShapeProperty);
        
        // Only show curve amount if blade shape is Curved
        BladeShape currentShape = (BladeShape)bladeShapeProperty.enumValueIndex;
        if (currentShape == BladeShape.Curved)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("bladeCurveAmount"));
        }
        
        // Always show petal shape - works with all blade types
        EditorGUILayout.PropertyField(serializedObject.FindProperty("petalShape"));
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Hub Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("hubRadius"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("hubHeight"));
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Ring Settings", EditorStyles.boldLabel);
        
        SerializedProperty includeRingProperty = serializedObject.FindProperty("includeRing");
        EditorGUILayout.PropertyField(includeRingProperty);
        
        // Only show ring thickness if ring is enabled
        if (includeRingProperty.boolValue)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("ringThickness"));
        }

        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space();
        
        if (GUILayout.Button("Generate Rotor", GUILayout.Height(40)))
        {
            rotor.GenerateRotor();
        }
    }
}