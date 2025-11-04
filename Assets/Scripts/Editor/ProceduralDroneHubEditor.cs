using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ProceduralDroneHub))]
public class ProceduralDroneHubEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        
        ProceduralDroneHub hub = (ProceduralDroneHub)target;

        if (GUILayout.Button("Generate Hub", GUILayout.Height(40)))
        {
            hub.GenerateHub();
        }
    }
}