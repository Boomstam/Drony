using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RandomDronePartGenerator))]
public class RandomDronePartGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        RandomDronePartGenerator generator = (RandomDronePartGenerator)target;

        GUILayout.Space(10);

        // Randomize All button - light green, biggest
        GUI.backgroundColor = new Color(0.7f, 1f, 0.7f);
        if (GUILayout.Button("Randomize All (Body + Rotors + Arms + Layout)", GUILayout.Height(40)))
        {
            generator.RandomizeAll();
        }
        GUI.backgroundColor = Color.white;

        GUILayout.Space(5);

        // Randomize Layout button
        if (GUILayout.Button("Randomize Layout", GUILayout.Height(30)))
        {
            generator.RandomizeLayout();
        }

        // Randomize Rotor button
        if (GUILayout.Button("Randomize Rotor", GUILayout.Height(30)))
        {
            generator.RandomizeRotor();
        }

        // Randomize Body button
        if (GUILayout.Button("Randomize Body", GUILayout.Height(30)))
        {
            generator.RandomizeBody();
        }

        GUILayout.Space(10);

        // Delete Current Drone button
        GUI.backgroundColor = Color.red;
        if (GUILayout.Button("Delete Current Drone", GUILayout.Height(30)))
        {
            generator.DeleteCurrentDrone();
        }
        GUI.backgroundColor = Color.white;
    }
}