using UnityEngine;

/// <summary>
/// DJI-style world-space drone movement controller.
/// Handles keyboard input or moves toward a target if assigned.
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

    [Header("Tilt Settings (Visual Only)")]
    [SerializeField] private float maxPitchTilt = 25f; // degrees for forward/backward movement
    [SerializeField] private float maxRollTilt = 20f;  // degrees for left/right movement
    [SerializeField] private float tiltSmoothTime = 0.15f;

    [Header("AI / Autopilot")]
    [SerializeField] private Transform target; // Optional move-to target
    [SerializeField] private float targetArrivalThreshold = 0.5f;

    // Current velocities for smoothing
    private Vector3 currentVelocity = Vector3.zero;
    private Vector3 velocitySmoothing = Vector3.zero;
    private float currentYawVelocity = 0f;

    // Current tilt angles for smoothing
    private float currentPitch = 0f;
    private float currentRoll = 0f;
    private float pitchVelocity = 0f;
    private float rollVelocity = 0f;

    private void Update()
    {
        // If target assigned, move toward it; else handle manual input
        if (target != null)
            HandleTargetMovement();
        else
            HandleKeyboardInput();
    }

    private void FixedUpdate()
    {
        ApplyMovement();
    }

    /// <summary>
    /// Manual control via keyboard input.
    /// </summary>
    private void HandleKeyboardInput()
    {
        Vector3 localInput = Vector3.zero;

        // WASD for horizontal movement (local to drone's yaw)
        if (Input.GetKey(KeyCode.W)) localInput += Vector3.forward;
        if (Input.GetKey(KeyCode.S)) localInput += Vector3.back;
        if (Input.GetKey(KeyCode.A)) localInput += Vector3.left;
        if (Input.GetKey(KeyCode.D)) localInput += Vector3.right;

        // Normalize horizontal input
        Vector3 horizontalInput = new Vector3(localInput.x, 0, localInput.z);
        if (horizontalInput.magnitude > 1f)
            horizontalInput.Normalize();

        // Convert local input to world space based on current yaw
        float currentYaw = rb.rotation.eulerAngles.y;
        Quaternion yawRotation = Quaternion.Euler(0, currentYaw, 0);
        Vector3 worldInput = yawRotation * horizontalInput;

        Vector3 targetVelocity = worldInput * horizontalSpeed;

        // Shift/Ctrl for vertical movement
        if (Input.GetKey(KeyCode.LeftShift)) targetVelocity.y = verticalSpeed;
        if (Input.GetKey(KeyCode.LeftControl)) targetVelocity.y = -verticalSpeed;

        // Smooth velocity change
        currentVelocity = Vector3.SmoothDamp(currentVelocity, targetVelocity, ref velocitySmoothing, accelerationTime);

        // Tilt visuals based on local input direction
        ApplyTiltFromInput(horizontalInput);
    }

    /// <summary>
    /// Automatically move toward a target if assigned.
    /// </summary>
    private void HandleTargetMovement()
    {
        if (target == null) return;

        Vector3 toTarget = target.position - rb.position;
        float distance = toTarget.magnitude;

        if (distance < targetArrivalThreshold)
        {
            Hover();
            return;
        }

        Vector3 moveDir = toTarget.normalized;
        Vector3 desiredVelocity = moveDir * horizontalSpeed;

        // Adjust vertical component separately
        float verticalDelta = target.position.y - rb.position.y;
        desiredVelocity.y = Mathf.Clamp(verticalDelta, -1f, 1f) * verticalSpeed;

        // Smooth velocity change
        currentVelocity = Vector3.SmoothDamp(currentVelocity, desiredVelocity, ref velocitySmoothing, accelerationTime);

        // Rotate to face target (yaw)
        RotateToWorldFacing(new Vector3(moveDir.x, 0f, moveDir.z));

        // Apply tilt for visuals based on horizontal movement
        Vector3 horizontalDir = new Vector3(moveDir.x, 0f, moveDir.z);
        ApplyTiltFromInput(horizontalDir);
    }

    /// <summary>
    /// Calculates and smooths visual tilt based on input direction.
    /// </summary>
    private void ApplyTiltFromInput(Vector3 horizontalInput)
    {
        float targetPitch = 0f;
        float targetRoll = 0f;

        if (horizontalInput.magnitude > 0.01f)
        {
            targetPitch = horizontalInput.z * maxPitchTilt; // Forward/back tilt (positive = forward)
            targetRoll = -horizontalInput.x * maxRollTilt;  // Left/right bank
        }

        currentPitch = Mathf.SmoothDamp(currentPitch, targetPitch, ref pitchVelocity, tiltSmoothTime);
        currentRoll = Mathf.SmoothDamp(currentRoll, targetRoll, ref rollVelocity, tiltSmoothTime);
    }

    private void ApplyMovement()
    {
        // Apply world-space velocity
        Vector3 newPosition = rb.position + currentVelocity * Time.fixedDeltaTime;
        rb.MovePosition(newPosition);

        // Handle yaw (Q/E manual)
        float yawInput = 0f;
        if (Input.GetKey(KeyCode.Q)) yawInput = -1f;
        if (Input.GetKey(KeyCode.E)) yawInput = 1f;

        float targetYaw = 0f;
        if (Mathf.Abs(yawInput) > 0.01f)
        {
            float targetYawVelocity = yawInput * yawSpeed;
            currentYawVelocity = Mathf.Lerp(currentYawVelocity, targetYawVelocity, Time.fixedDeltaTime / rotationSmoothTime);
            targetYaw = currentYawVelocity * Time.fixedDeltaTime;
        }
        else
        {
            currentYawVelocity = Mathf.Lerp(currentYawVelocity, 0f, Time.fixedDeltaTime / rotationSmoothTime);
        }

        Vector3 currentEuler = rb.rotation.eulerAngles;
        float newYaw = currentEuler.y + targetYaw;

        // Combine yaw with tilt
        Quaternion finalRotation = Quaternion.Euler(currentPitch, newYaw, currentRoll);
        rb.MoveRotation(finalRotation);
    }

    #region Movement Primitives

    public void MoveInWorldDirection(Vector3 worldDirection, float speed)
    {
        if (worldDirection.sqrMagnitude > 0.01f)
            currentVelocity = worldDirection.normalized * speed;
    }

    public void SetWorldVelocity(Vector3 velocity) => currentVelocity = velocity;

    public void RotateToWorldFacing(Vector3 worldForward)
    {
        if (worldForward.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(worldForward, Vector3.up);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, Time.fixedDeltaTime / rotationSmoothTime));
        }
    }

    public void SetTargetAltitude(float targetY, float speed)
    {
        float currentY = rb.position.y;
        float direction = Mathf.Sign(targetY - currentY);

        if (Mathf.Abs(targetY - currentY) > 0.1f)
            currentVelocity.y = direction * speed;
        else
            currentVelocity.y = 0f;
    }

    public void Hover()
    {
        currentVelocity = Vector3.zero;
        velocitySmoothing = Vector3.zero;
        currentYawVelocity = 0f;
        currentPitch = 0f;
        currentRoll = 0f;
        pitchVelocity = 0f;
        rollVelocity = 0f;
    }

    #endregion

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || rb == null) return;

        Gizmos.color = Color.green;
        Gizmos.DrawRay(rb.position, currentVelocity);

        Gizmos.color = Color.blue;
        Gizmos.DrawRay(rb.position, transform.forward * 2f);

        if (target != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(rb.position, target.position);
        }
    }
}