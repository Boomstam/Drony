using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ProceduralDroneTextureGenerator))]
public class ProceduralDroneTextureGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ProceduralDroneTextureGenerator generator = (ProceduralDroneTextureGenerator)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Texture Generation", EditorStyles.boldLabel);

        // Generate with new seed button - primary action
        GUI.backgroundColor = new Color(0.7f, 1f, 0.7f);
        if (GUILayout.Button("Generate New Textures", GUILayout.Height(40)))
        {
            generator.GenerateTextures();
            EditorUtility.SetDirty(target);
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(5);

        // Regenerate with same seed
        if (GUILayout.Button("Regenerate (Same Seed)", GUILayout.Height(30)))
        {
            generator.GenerateTextures(generator.GetCurrentSeed());
            EditorUtility.SetDirty(target);
        }

        EditorGUILayout.Space(5);

        // Show current seed
        EditorGUILayout.LabelField("Current Seed", generator.GetCurrentSeed().ToString());

        EditorGUILayout.Space(10);

        // Restore original materials button
        GUI.backgroundColor = new Color(1f, 0.8f, 0.6f);
        if (GUILayout.Button("Restore Original Materials", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog("Restore Materials",
                "This will restore all original materials. Continue?",
                "Yes", "Cancel"))
            {
                generator.RestoreOriginalMaterials();
                EditorUtility.SetDirty(target);
            }
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(10);

        // Help box
        EditorGUILayout.HelpBox(
            "Generate New Textures: Creates new procedural textures with a random seed.\n\n" +
            "Regenerate (Same Seed): Recreates textures using the current seed for consistent results.\n\n" +
            "Auto Generate: When enabled, textures regenerate when the drone geometry changes.",
            MessageType.Info
        );
    }
}