using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ProceduralRotor))]
public class ProceduralRotorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        
        ProceduralRotor rotor = (ProceduralRotor)target;

        if (GUILayout.Button("Generate Rotor", GUILayout.Height(40)))
        {
            rotor.GenerateRotor();
        }
    }
}