using UnityEngine;

/// <summary>
/// DJI-style world-space drone movement controller.
/// Handles keyboard input and translates to world-space movement primitives.
/// Uses kinematic Rigidbody for physics-friendly movement.
/// </summary>
public class DroneMovementController : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Rigidbody rb;

    [Header("Movement Settings")]
    [SerializeField] private float horizontalSpeed = 5f;
    [SerializeField] private float verticalSpeed = 3f;
    [SerializeField] private float yawSpeed = 90f; // degrees per second

    [Header("Smoothing")]
    [SerializeField] private float accelerationTime = 0.2f;
    [SerializeField] private float rotationSmoothTime = 0.1f;

    // Current velocities for smoothing
    private Vector3 currentVelocity = Vector3.zero;
    private Vector3 velocitySmoothing = Vector3.zero;
    private float currentYawVelocity = 0f;

    private void Update()
    {
        // Handle keyboard input each frame
        HandleKeyboardInput();
    }

    private void FixedUpdate()
    {
        // Apply movement in physics step
        ApplyMovement();
    }

    private void HandleKeyboardInput()
    {
        Vector3 targetVelocity = Vector3.zero;

        // WASD for horizontal world-space movement
        if (Input.GetKey(KeyCode.W)) targetVelocity += Vector3.forward; // North
        if (Input.GetKey(KeyCode.S)) targetVelocity += Vector3.back;    // South
        if (Input.GetKey(KeyCode.A)) targetVelocity += Vector3.left;    // West
        if (Input.GetKey(KeyCode.D)) targetVelocity += Vector3.right;   // East

        // Normalize horizontal input to prevent faster diagonal movement
        Vector3 horizontalInput = new Vector3(targetVelocity.x, 0, targetVelocity.z);
        if (horizontalInput.magnitude > 1f)
        {
            horizontalInput.Normalize();
        }
        targetVelocity = horizontalInput * horizontalSpeed;

        // Shift/Ctrl for vertical world-space movement
        if (Input.GetKey(KeyCode.LeftShift)) targetVelocity.y = verticalSpeed;   // Up
        if (Input.GetKey(KeyCode.LeftControl)) targetVelocity.y = -verticalSpeed; // Down

        // Smooth velocity changes
        currentVelocity = Vector3.SmoothDamp(
            currentVelocity, 
            targetVelocity, 
            ref velocitySmoothing, 
            accelerationTime
        );
    }

    private void ApplyMovement()
    {
        // Apply world-space velocity
        Vector3 newPosition = rb.position + currentVelocity * Time.fixedDeltaTime;
        rb.MovePosition(newPosition);

        // Handle yaw rotation (Q/E)
        float yawInput = 0f;
        if (Input.GetKey(KeyCode.Q)) yawInput = -1f; // Rotate left
        if (Input.GetKey(KeyCode.E)) yawInput = 1f;  // Rotate right

        if (Mathf.Abs(yawInput) > 0.01f)
        {
            // Smooth yaw rotation
            float targetYawVelocity = yawInput * yawSpeed;
            currentYawVelocity = Mathf.Lerp(currentYawVelocity, targetYawVelocity, Time.fixedDeltaTime / rotationSmoothTime);
            
            // Apply rotation around world up axis
            Quaternion deltaRotation = Quaternion.Euler(0f, currentYawVelocity * Time.fixedDeltaTime, 0f);
            Quaternion newRotation = rb.rotation * deltaRotation;
            rb.MoveRotation(newRotation);
        }
        else
        {
            // Decay yaw velocity when no input
            currentYawVelocity = Mathf.Lerp(currentYawVelocity, 0f, Time.fixedDeltaTime / rotationSmoothTime);
        }
    }

    #region Movement Primitives (for future AI/scripting use)

    /// <summary>
    /// Move in a world-space direction at a given speed.
    /// </summary>
    public void MoveInWorldDirection(Vector3 worldDirection, float speed)
    {
        if (worldDirection.sqrMagnitude > 0.01f)
        {
            currentVelocity = worldDirection.normalized * speed;
        }
    }

    /// <summary>
    /// Set direct world-space velocity.
    /// </summary>
    public void SetWorldVelocity(Vector3 velocity)
    {
        currentVelocity = velocity;
    }

    /// <summary>
    /// Rotate to face a world-space direction.
    /// </summary>
    public void RotateToWorldFacing(Vector3 worldForward)
    {
        if (worldForward.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(worldForward, Vector3.up);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, Time.fixedDeltaTime / rotationSmoothTime));
        }
    }

    /// <summary>
    /// Set target altitude (world Y position).
    /// </summary>
    public void SetTargetAltitude(float targetY, float speed)
    {
        float currentY = rb.position.y;
        float direction = Mathf.Sign(targetY - currentY);
        
        // Only apply vertical velocity if not at target
        if (Mathf.Abs(targetY - currentY) > 0.1f)
        {
            currentVelocity.y = direction * speed;
        }
        else
        {
            currentVelocity.y = 0f;
        }
    }

    /// <summary>
    /// Stop all movement.
    /// </summary>
    public void Hover()
    {
        currentVelocity = Vector3.zero;
        velocitySmoothing = Vector3.zero;
        currentYawVelocity = 0f;
    }

    #endregion

    #region Debug Visualization

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || rb == null) return;

        // Draw velocity vector
        Gizmos.color = Color.green;
        Gizmos.DrawRay(rb.position, currentVelocity);

        // Draw forward direction
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(rb.position, transform.forward * 2f);
    }

    #endregion
}