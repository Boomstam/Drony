using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ProceduralDroneBody))]
public class ProceduralDroneBodyEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        
        ProceduralDroneBody body = (ProceduralDroneBody)target;

        if (GUILayout.Button("Generate Body", GUILayout.Height(40)))
        {
            body.GenerateBody();
        }
    }
}