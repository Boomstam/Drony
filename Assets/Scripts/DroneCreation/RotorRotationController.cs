using UnityEngine;

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