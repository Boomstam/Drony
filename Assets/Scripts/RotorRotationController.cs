using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Controls the rotation of drone rotors with configurable RPM.
/// Works in both Play mode and Edit mode (with toggle).
/// </summary>
public class RotorRotationController : MonoBehaviour
{
    [Header("Rotation Settings")]
    [SerializeField, Tooltip("Rotations per minute")]
    private float rpm = 1000f;

    [SerializeField, Tooltip("Rotation axis (default is Y-axis for upward-facing rotors)")]
    private Vector3 rotationAxis = Vector3.up;

    [Header("Editor Mode Settings")]
    [SerializeField, Tooltip("Enable rotation in Edit mode (Editor only)")]
    private bool rotateInEditor = false;

    private float currentRotation = 0f;

    private void Update()
    {
        // Only rotate in Play mode
        if (Application.isPlaying)
        {
            RotateRotor(Time.deltaTime);
        }
    }

    private void RotateRotor(float deltaTime)
    {
        // Convert RPM to degrees per second
        // RPM = rotations per minute
        // Degrees per second = (RPM / 60) * 360
        float degreesPerSecond = (rpm / 60f) * 360f;
        
        // Calculate rotation for this frame
        float rotationThisFrame = degreesPerSecond * deltaTime;
        currentRotation += rotationThisFrame;
        
        // Apply rotation
        transform.Rotate(rotationAxis.normalized, rotationThisFrame, Space.Self);
    }

#if UNITY_EDITOR
    /// <summary>
    /// Called in Edit mode to update rotation when rotateInEditor is enabled
    /// </summary>
    public void UpdateEditorRotation()
    {
        if (rotateInEditor && !Application.isPlaying)
        {
            // Use EditorApplication.timeSinceStartup for consistent timing in editor
            float deltaTime = 0.016f; // Approximate 60 FPS for smooth editor preview
            RotateRotor(deltaTime);
        }
    }
#endif

    /// <summary>
    /// Set the RPM value programmatically
    /// </summary>
    public void SetRPM(float newRPM)
    {
        rpm = Mathf.Max(0f, newRPM);
    }

    /// <summary>
    /// Get the current RPM value
    /// </summary>
    public float GetRPM()
    {
        return rpm;
    }

    /// <summary>
    /// Set the rotation axis
    /// </summary>
    public void SetRotationAxis(Vector3 axis)
    {
        rotationAxis = axis.normalized;
    }

    /// <summary>
    /// Reset rotation to zero
    /// </summary>
    public void ResetRotation()
    {
        currentRotation = 0f;
        transform.localRotation = Quaternion.identity;
    }

    private void OnValidate()
    {
        // Ensure RPM is never negative
        rpm = Mathf.Max(0f, rpm);
    }
}

#if UNITY_EDITOR
/// <summary>
/// Custom editor for RotorRotationController to enable Edit mode rotation
/// </summary>
[CustomEditor(typeof(RotorRotationController))]
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
        RotorRotationController controller = (RotorRotationController)target;
        
        if (controller == null)
            return;

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
            
            // Request repaint of scene view
            SceneView.RepaintAll();
        }
        else
        {
            wasRotatingInEditor = false;
        }

        lastEditorTime = EditorApplication.timeSinceStartup;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        RotorRotationController controller = (RotorRotationController)target;

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "Play Mode: Rotors rotate automatically.\n" +
            "Edit Mode: Enable 'Rotate In Editor' to preview rotation.",
            MessageType.Info
        );

        EditorGUILayout.Space();

        // Show calculated degrees per second
        float degreesPerSecond = (controller.GetRPM() / 60f) * 360f;
        EditorGUILayout.LabelField("Rotation Speed", $"{degreesPerSecond:F2}°/second");

        EditorGUILayout.Space();

        // Reset button
        if (GUILayout.Button("Reset Rotation", GUILayout.Height(30)))
        {
            Undo.RecordObject(controller.transform, "Reset Rotor Rotation");
            controller.ResetRotation();
            EditorUtility.SetDirty(controller.transform);
        }
    }
}
#endif