using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RotorRotationController))]
[CanEditMultipleObjects]
public class RotorRotationControllerEditor : Editor
{
    private double lastEditorTime;
    private bool wasRotatingInEditor;

    private void OnEnable()
    {
        lastEditorTime = EditorApplication.timeSinceStartup;
        wasRotatingInEditor = false;
        EditorApplication.update += OnEditorUpdate;
    }

    private void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;
    }

    private void OnEditorUpdate()
    {
        // Handle all selected objects
        foreach (Object targetObj in targets)
        {
            RotorRotationController controller = targetObj as RotorRotationController;
            
            if (controller == null)
                continue;

            // Get the rotateInEditor value using SerializedObject
            SerializedObject so = new SerializedObject(controller);
            SerializedProperty rotateInEditorProp = so.FindProperty("rotateInEditor");
            bool isRotatingInEditor = rotateInEditorProp.boolValue;

            // Only update if rotating is enabled and we're not in Play mode
            if (isRotatingInEditor && !Application.isPlaying)
            {
                // Mark scene as dirty when rotation starts
                if (!wasRotatingInEditor)
                {
                    wasRotatingInEditor = true;
                }

                controller.UpdateEditorRotation();
                
                // Mark the transform as dirty to update the scene view
                EditorUtility.SetDirty(controller.transform);
            }
        }

        // Request repaint of scene view if any object is rotating
        if (wasRotatingInEditor)
        {
            SceneView.RepaintAll();
        }
        else
        {
            // Check if any object has rotation enabled
            bool anyRotating = false;
            foreach (Object targetObj in targets)
            {
                RotorRotationController controller = targetObj as RotorRotationController;
                if (controller != null)
                {
                    SerializedObject so = new SerializedObject(controller);
                    SerializedProperty rotateInEditorProp = so.FindProperty("rotateInEditor");
                    if (rotateInEditorProp.boolValue)
                    {
                        anyRotating = true;
                        break;
                    }
                }
            }
            wasRotatingInEditor = anyRotating;
        }

        lastEditorTime = EditorApplication.timeSinceStartup;
    }

    public override void OnInspectorGUI()
    {
        // Update serialized object
        serializedObject.Update();

        // Draw default inspector using serialized properties (supports multi-object editing)
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "Play Mode: Rotors rotate automatically.\n" +
            "Edit Mode: Enable 'Rotate In Editor' to preview rotation.",
            MessageType.Info
        );

        EditorGUILayout.Space();

        // Show calculated degrees per second (only for single selection)
        if (targets.Length == 1)
        {
            RotorRotationController controller = (RotorRotationController)target;
            float degreesPerSecond = (controller.GetRPM() / 60f) * 360f;
            EditorGUILayout.LabelField("Rotation Speed", $"{degreesPerSecond:F2}°/second");
            EditorGUILayout.Space();
        }
        else
        {
            EditorGUILayout.LabelField("Multi-Object Editing", $"{targets.Length} objects selected");
            EditorGUILayout.Space();
        }

        // Reset button (works for all selected objects)
        if (GUILayout.Button("Reset Rotation", GUILayout.Height(30)))
        {
            foreach (Object targetObj in targets)
            {
                RotorRotationController controller = targetObj as RotorRotationController;
                if (controller != null)
                {
                    Undo.RecordObject(controller.transform, "Reset Rotor Rotation");
                    controller.ResetRotation();
                    EditorUtility.SetDirty(controller.transform);
                }
            }
        }

        // Apply modified properties
        serializedObject.ApplyModifiedProperties();
    }
}