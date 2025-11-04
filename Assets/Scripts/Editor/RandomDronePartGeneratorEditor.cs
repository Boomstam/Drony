using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(RandomDronePartGenerator))]
public class RandomDronePartGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        RandomDronePartGenerator generator = (RandomDronePartGenerator)target;

        GUILayout.Space(10);

        // Randomize All button
        if (GUILayout.Button("Randomize All", GUILayout.Height(40)))
        {
            generator.RandomizeAll();
        }

        GUILayout.Space(5);

        // Randomize Rotor button
        if (GUILayout.Button("Randomize Rotor", GUILayout.Height(30)))
        {
            generator.RandomizeRotor();
        }

        // Randomize Hub button
        if (GUILayout.Button("Randomize Hub", GUILayout.Height(30)))
        {
            generator.RandomizeHub();
        }
    }
}