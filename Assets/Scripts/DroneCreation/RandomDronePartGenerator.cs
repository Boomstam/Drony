using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class RandomDronePartGenerator : MonoBehaviour
{
    [SerializeField] private Camera camPrefab;
    
    [Header("Component References")]
    [SerializeField] private ProceduralDroneBody droneBodyPrefab;
    [SerializeField] private ProceduralRotor droneRotorPrefab;
    [SerializeField] private ProceduralDroneTextureGenerator textureGenerator;
    [SerializeField] private DroneMovementController dronePrefab;
    
    [Header("Generated Instances (Do Not Assign)")]
    private DroneMovementController droneContainer;
    private ProceduralDroneBody generatedBody;
    private List<ProceduralRotor> generatedRotors = new List<ProceduralRotor>();
    private List<GameObject> generatedArms = new List<GameObject>();

    [Header("Seeds")]
    [SerializeField] private int mainSeed = 42;
    [SerializeField] private int rotorSeed = 100;
    [SerializeField] private int bodySeed = 200;
    [SerializeField] private int armSeed = 300;
    [SerializeField] private int textureSeed = 400;

    [Header("Layout Parameters")]
    [SerializeField] private int rotorCount = 4; // 4, 6, or 8 rotors
    [SerializeField] private Vector2 rotorDistanceRange = new Vector2(1.5f, 3.0f);
    [SerializeField] private Vector2 rotorVerticalOffsetRange = new Vector2(-0.3f, 0.3f);
    [SerializeField] private Vector2 rotorTiltAngleRange = new Vector2(-15f, 15f);

    [Header("Arm Parameters")]
    [SerializeField] private bool generateArms = true;
    [SerializeField] private ArmShape armShape = ArmShape.Cylindrical;
    [SerializeField] private Vector2 armThicknessRange = new Vector2(0.03f, 0.1f);
    [SerializeField] private bool autoScaleArmThickness = true;
    [SerializeField, Range(0f, 1f)] private float armThicknessScale = 0.5f; // When auto-scaling is on
    [SerializeField] private int armSegments = 12; // Resolution for cylindrical arms
    [SerializeField] private float ringClearance = 0.15f; // Extra space around ring when bending

    [Header("Randomization Ranges")]
    [SerializeField] private Vector2 bladeLengthRange = new Vector2(0.5f, 2.5f);
    [SerializeField] private Vector2 bladeWidthRange = new Vector2(0.1f, 0.4f);
    [SerializeField] private Vector2 bladeThicknessRange = new Vector2(0.02f, 0.08f);
    [SerializeField] private Vector2 rotorHubRadiusRange = new Vector2(0.08f, 0.3f);
    [SerializeField] private Vector2 rotorHubHeightRange = new Vector2(0.05f, 0.2f);
    [SerializeField] private Vector2 ringThicknessRange = new Vector2(0.03f, 0.12f);
    [SerializeField] private Vector2 bodyScaleRange = new Vector2(0.2f, 1.0f);
    [SerializeField, Range(0f, 1f)] private float ringProbability = 0.3f;

    // Current randomized layout values
    private float currentRotorDistance;
    private float currentVerticalOffset;
    private float currentTiltAngle;

    public enum ArmShape
    {
        Cylindrical,
        Rectangular
    }

    public void RandomizeAll()
    {
        DeleteCurrentDrone();
        
        mainSeed = Random.Range(0, 1000000);
        rotorSeed = Random.Range(0, 1000000);
        bodySeed = Random.Range(0, 1000000);
        armSeed = Random.Range(0, 1000000);
        textureSeed = Random.Range(0, 1000000);
        
        // CRITICAL: Determine rotor count BEFORE creating components
        Random.InitState(mainSeed);
        float rand = Random.value;
        if (rand < 0.33f) rotorCount = 4;
        else if (rand < 0.66f) rotorCount = 6;
        else rotorCount = 8;
        
        EnsureComponentsExist();
        
        Random.InitState(bodySeed);
        RandomizeBodyInternal();
        
        Random.InitState(rotorSeed);
        RandomizeRotorInternal();
        
        Random.InitState(mainSeed);
        RandomizeLayoutInternal(mainSeed);

        Random.InitState(armSeed);
        RandomizeArmsInternal();
        
        if (generateArms)
        {
            GenerateArms();
        }
        
        // Generate textures AFTER all geometry is created
        if (textureGenerator != null)
        {
            textureGenerator.SetSeed(textureSeed);
            textureGenerator.GenerateTextures(textureSeed);
        }
        
        // Position camera under the drone
        PositionCamera();
        
        #if UNITY_EDITOR
        EditorUtility.SetDirty(this);
        #endif
        
        Debug.Log($"Randomized all | mainSeed: {mainSeed} | rotorSeed: {rotorSeed} | bodySeed: {bodySeed} | armSeed: {armSeed} | textureSeed: {textureSeed}");
    }

    private void PositionCamera()
    {
        Camera cam = Instantiate(camPrefab);

        // Parent camera to drone container
        cam.transform.SetParent(droneContainer.transform, false);
    }

    public void RandomizeLayout()
    {
        mainSeed = Random.Range(0, 1000000);
        Random.InitState(mainSeed);
        RandomizeLayoutInternal(mainSeed);
        
        if (generateArms)
        {
            GenerateArms();
        }
        
        #if UNITY_EDITOR
        EditorUtility.SetDirty(this);
        #endif
    }

    private void EnsureComponentsExist()
    {
        // Delete old drone container and everything in it
        if (droneContainer != null)
        {
            DestroyImmediate(droneContainer);
            droneContainer = null;
        }
        
        // Clear references
        generatedBody = null;
        generatedRotors.Clear();
        generatedArms.Clear();

        // Create new drone container GameObject
        droneContainer = Instantiate(dronePrefab);
        droneContainer.transform.SetParent(transform);
        droneContainer.transform.localPosition = Vector3.zero;
        droneContainer.transform.localRotation = Quaternion.identity;

        // Create new hub instance
        if (droneBodyPrefab != null)
        {
            generatedBody = Instantiate(droneBodyPrefab, droneContainer.transform);
            generatedBody.gameObject.name = "GeneratedBody";
            generatedBody.gameObject.SetActive(true);
            generatedBody.transform.localPosition = Vector3.zero;
            generatedBody.transform.localRotation = Quaternion.identity;
        }

        // Create rotor instances
        if (droneRotorPrefab != null)
        {
            for (int i = 0; i < rotorCount; i++)
            {
                ProceduralRotor rotor = Instantiate(droneRotorPrefab, droneContainer.transform);
                rotor.gameObject.name = $"GeneratedRotor_{i}";
                rotor.transform.localPosition = Vector3.zero;
                rotor.transform.localRotation = Quaternion.identity;
                
                generatedRotors.Add(rotor);
            }
        }

        // Debug.Log($"[EnsureComponentsExist] Created DroneContainer with DroneMovementController (which creates its own Rigidbody)");
    }

    private void RandomizeLayoutInternal(int seed)
    {
        Random.InitState(seed);
        
        // Note: rotorCount is now set BEFORE this method is called (in RandomizeAll)
        // This ensures EnsureComponentsExist() creates the correct number of rotors

        float hubMaxDimension = 0.5f;
        float rotorTotalReach = 1.0f;
        
        if (generatedBody != null)
        {
            hubMaxDimension = Mathf.Max(generatedBody.scale.x, generatedBody.scale.y, generatedBody.scale.z);
            // Debug.Log($"[Layout] Hub max dimension: {hubMaxDimension}");
        }
        
        if (generatedRotors.Count > 0 && generatedRotors[0] != null)
        {
            ProceduralRotor rotor = generatedRotors[0];
            
            // Debug.Log($"[Layout] Rotor properties - Hub Radius: {rotor.hubRadius}, Blade Length: {rotor.bladeLength}, Blade Width: {rotor.bladeWidth}, Include Ring: {rotor.includeRing}, Ring Thickness: {rotor.ringThickness}");
            
            // Calculate total rotor reach including:
            // - Hub radius
            // - Blade length
            // - Ring thickness (if ring is enabled)
            // - Blade width (extends perpendicular, but needs buffer)
            float bladeReach = rotor.hubRadius + rotor.bladeLength;
            // Debug.Log($"[Layout] Blade reach (hub + length): {bladeReach}");
            
            float ringReach = rotor.includeRing ? (bladeReach * 1.1f + rotor.ringThickness) : bladeReach;
            // Debug.Log($"[Layout] Ring reach: {ringReach}");
            
            // Add blade width as additional buffer since blades can stick out at angles
            rotorTotalReach = ringReach + rotor.bladeWidth * 0.5f;
            // Debug.Log($"[Layout] Total rotor reach (with blade width buffer): {rotorTotalReach}");
        }

        // Calculate minimum safe distance based on rotor count and rotor-to-rotor spacing
        // For N rotors arranged in a circle, adjacent rotors are (360/N) degrees apart
        // Using law of cosines: distance between adjacent rotor centers = 2 * R * sin(angle/2)
        // We need: 2 * R * sin(angle/2) >= 2 * rotorTotalReach + safetyBuffer
        // Therefore: R >= (2 * rotorTotalReach + safetyBuffer) / (2 * sin(angle/2))
        
        float angleStep = 360f / rotorCount;
        float angleBetweenRotors = angleStep * Mathf.Deg2Rad;
        float halfAngle = angleBetweenRotors / 2f;
        
        // Minimum distance between adjacent rotor centers needed to prevent intersection
        float minRotorToRotorDistance = 2f * rotorTotalReach + 0.4f; // 0.4 is safety buffer between rotors
        
        // Calculate required radius from center to achieve this rotor-to-rotor distance
        float minRadiusForRotorSpacing = minRotorToRotorDistance / (2f * Mathf.Sin(halfAngle));
        // Debug.Log($"[Layout] Rotor count: {rotorCount}, Angle between: {angleStep}°, Min rotor-to-rotor distance: {minRotorToRotorDistance}, Required radius: {minRadiusForRotorSpacing}");
        
        // Also ensure rotors clear the hub
        float minRadiusForHubClearance = (hubMaxDimension * 0.5f) + rotorTotalReach + 0.3f;
        // Debug.Log($"[Layout] Min radius for hub clearance: {minRadiusForHubClearance}");
        
        // Use the larger of the two requirements
        float minSafeDistance = Mathf.Max(minRadiusForRotorSpacing, minRadiusForHubClearance);
        // Debug.Log($"[Layout] Calculated min safe distance: {minSafeDistance}");
        
        float actualMinDistance = Mathf.Max(rotorDistanceRange.x, minSafeDistance);
        // Debug.Log($"[Layout] Actual min distance after range check: {actualMinDistance} (range min: {rotorDistanceRange.x}, range max: {rotorDistanceRange.y})");
        
        float randomValue = Random.value;
        // Debug.Log($"[Layout] Random value for distance lerp: {randomValue}, Lerp range: [{actualMinDistance}, {rotorDistanceRange.y}]");
        
        // CRITICAL FIX: If calculated minimum is greater than range maximum, use the minimum
        float effectiveMax = Mathf.Max(actualMinDistance, rotorDistanceRange.y);
        if (effectiveMax > rotorDistanceRange.y)
        {
            Debug.LogWarning($"[Layout] Calculated min distance ({actualMinDistance}) exceeds range max ({rotorDistanceRange.y})! Using min as the distance.");
        }
        
        currentRotorDistance = Mathf.Lerp(actualMinDistance, effectiveMax, randomValue);
        currentVerticalOffset = Mathf.Lerp(rotorVerticalOffsetRange.x, rotorVerticalOffsetRange.y, Random.value);
        currentTiltAngle = Mathf.Lerp(rotorTiltAngleRange.x, rotorTiltAngleRange.y, Random.value);

        // Debug.Log($"[Layout] Final rotor distance: {currentRotorDistance}, Vertical offset: {currentVerticalOffset}, Tilt angle: {currentTiltAngle}");
        // Debug.Log($"[Layout] Rotor count: {rotorCount}, Generated rotors: {generatedRotors.Count}");

        // Safety check: Ensure we have the right number of rotors
        if (generatedRotors.Count != rotorCount)
        {
            Debug.LogWarning($"[Layout] Mismatch! Expected {rotorCount} rotors but have {generatedRotors.Count}. This should not happen!");
        }

        // Position all active rotors in a symmetric circle
        // angleStep already calculated above for distance calculation
        for (int i = 0; i < generatedRotors.Count; i++)
        {
            if (generatedRotors[i] != null)
            {
                if (i < rotorCount)
                {
                    // Position active rotors
                    float angle = i * angleStep * Mathf.Deg2Rad;
                    Vector3 position = new Vector3(
                        Mathf.Cos(angle) * currentRotorDistance,
                        currentVerticalOffset,
                        Mathf.Sin(angle) * currentRotorDistance
                    );
                    
                    generatedRotors[i].transform.localPosition = position;
                    generatedRotors[i].transform.localRotation = Quaternion.Euler(currentTiltAngle, 0, 0);
                    generatedRotors[i].gameObject.SetActive(true);
                    
                    // Debug.Log($"[Layout] Rotor {i}: Angle={angle * Mathf.Rad2Deg:F1}°, Position=({position.x:F2}, {position.y:F2}, {position.z:F2}), Distance from center={position.magnitude:F2}");
                }
                else
                {
                    // Deactivate extra rotors (shouldn't happen with the fix, but safety check)
                    generatedRotors[i].gameObject.SetActive(false);
                    // Debug.Log($"[Layout] Rotor {i}: Deactivated (extra)");
                }
            }
        }
        
        // Debug.Log($"Randomized layout with seed: {seed} | Rotors: {rotorCount} | Distance: {currentRotorDistance:F2}");
    }

    public void RandomizeRotor()
    {
        if (generatedRotors.Count == 0)
        {
            Debug.LogError("No rotors exist! Use Randomize All first.");
            return;
        }

        rotorSeed = Random.Range(0, 1000000);
        Random.InitState(rotorSeed);
        RandomizeRotorInternal();
        
        if (generateArms)
        {
            GenerateArms();
        }
        
        #if UNITY_EDITOR
        EditorUtility.SetDirty(this);
        #endif

        // Debug.Log($"Randomized rotor with seed: {rotorSeed}");
    }

    private void RandomizeRotorInternal()
    {
        int numBlades = Random.Range(2, 9);

        float bladeWidthModifier = 1f - ((numBlades - 2) / 6f) * 0.4f;
        float bladeLength = Mathf.Lerp(bladeLengthRange.x, bladeLengthRange.y, Random.value);
        float bladeWidth = Mathf.Lerp(bladeWidthRange.x, bladeWidthRange.y, Random.value) * bladeWidthModifier;
        float bladeThickness = Mathf.Lerp(bladeThicknessRange.x, bladeThicknessRange.y, Random.value);

        BladeShape bladeShape = (BladeShape)Random.Range(0, 3);
        float bladeCurveAmount = Random.Range(-1f, 1f);
        float petalShape = Mathf.Lerp(0f, 3f, Random.value);
        float bladeTwist = Random.Range(-1f, 1f);

        // Randomize rotor hub dimensions
        float hubRadius = Mathf.Lerp(rotorHubRadiusRange.x, rotorHubRadiusRange.y, Random.value);
        float hubHeight = Mathf.Lerp(rotorHubHeightRange.x, rotorHubHeightRange.y, Random.value);

        bool includeRing = Random.value < ringProbability;
        float ringThickness = includeRing ? Mathf.Lerp(ringThicknessRange.x, ringThicknessRange.y, Random.value) : 0.05f;

        foreach (var rotor in generatedRotors)
        {
            if (rotor != null)
            {
                rotor.numberOfBlades = numBlades;
                rotor.bladeLength = bladeLength;
                rotor.bladeWidth = bladeWidth;
                rotor.bladeThickness = bladeThickness;
                rotor.bladeShape = bladeShape;
                rotor.bladeCurveAmount = bladeCurveAmount;
                rotor.petalShape = petalShape;
                rotor.bladeTwist = bladeTwist;
                rotor.hubRadius = hubRadius;
                rotor.hubHeight = hubHeight;
                rotor.includeRing = includeRing;
                rotor.ringThickness = ringThickness;
                rotor.GenerateRotor();
            }
        }
    }

    public void RandomizeBody()
    {
        if (generatedBody == null)
        {
            Debug.LogError("No hub exists! Use Randomize All first.");
            return;
        }

        bodySeed = Random.Range(0, 1000000);
        Random.InitState(bodySeed);
        RandomizeBodyInternal();
        
        if (generateArms)
        {
            GenerateArms();
        }
        
        #if UNITY_EDITOR
        EditorUtility.SetDirty(this);
        #endif
        
        Debug.Log($"Randomized hub with seed: {bodySeed}");
    }

    private void RandomizeBodyInternal()
    {
        generatedBody.baseShape = (ProceduralDroneBody.BaseShape)Random.Range(0, 2);

        generatedBody.scale = new Vector3(
            Mathf.Lerp(bodyScaleRange.x, bodyScaleRange.y, Random.value),
            Mathf.Lerp(bodyScaleRange.x, bodyScaleRange.y, Random.value),
            Mathf.Lerp(bodyScaleRange.x, bodyScaleRange.y, Random.value)
        );

        generatedBody.taper = Random.value;
        generatedBody.taperDirection = (ProceduralDroneBody.TaperDirection)Random.Range(0, 2);

        generatedBody.GenerateBody();
    }

    private void RandomizeArmsInternal()
    {
        armShape = (ArmShape)Random.Range(0, 2);
        
        if (!autoScaleArmThickness)
        {
            armThicknessScale = Random.value;
        }
    }

    public void DeleteCurrentDrone()
    {
        if (droneContainer != null)
        {
            DestroyImmediate(droneContainer);
            droneContainer = null;
        }

        // Clear all references
        generatedBody = null;
        generatedRotors.Clear();
        generatedArms.Clear();

        // FAILSAFE: Delete any remaining children that weren't tracked
        while (transform.childCount > 0)
        {
            DestroyImmediate(transform.GetChild(0).gameObject);
        }

        Debug.Log("Deleted current drone");
    }

    // ============ ARM GENERATION ============

    private void GenerateArms()
    {
        // Clean up old arms
        foreach (var arm in generatedArms)
        {
            if (arm != null)
            {
                DestroyImmediate(arm);
            }
        }
        generatedArms.Clear();

        if (generatedBody == null || generatedRotors.Count == 0)
        {
            return;
        }

        // Generate an arm for each rotor
        for (int i = 0; i < generatedRotors.Count; i++)
        {
            if (generatedRotors[i] != null)
            {
                GameObject arm = GenerateArmToRotor(i);
                if (arm != null)
                {
                    generatedArms.Add(arm);
                }
            }
        }
    }

    private GameObject GenerateArmToRotor(int rotorIndex)
    {
        ProceduralRotor rotor = generatedRotors[rotorIndex];
        Vector3 rotorWorldPos = rotor.transform.position;
        Vector3 rotorLocalPos = rotor.transform.localPosition;

        // Calculate hub surface point (direction towards rotor)
        Vector3 directionToRotor = rotorLocalPos.normalized;
        Vector3 hubSurfacePoint = GetHubSurfacePoint(directionToRotor);

        // Calculate rotor hub surface point (bottom of rotor hub)
        Vector3 rotorHubBottom = rotorLocalPos;
        rotorHubBottom.y -= rotor.hubHeight * 0.5f;

        // Check if we need to avoid the ring
        bool needsRingAvoidance = rotor.includeRing;
        
        GameObject armObj = new GameObject($"Arm_{rotorIndex}");
        armObj.transform.SetParent(droneContainer.transform);
        armObj.transform.localPosition = Vector3.zero;
        armObj.transform.localRotation = Quaternion.identity;

        MeshFilter mf = armObj.AddComponent<MeshFilter>();
        MeshRenderer mr = armObj.AddComponent<MeshRenderer>();

        // Copy material from hub
        if (generatedBody != null)
        {
            MeshRenderer hubMR = generatedBody.GetComponent<MeshRenderer>();
            if (hubMR != null && hubMR.sharedMaterial != null)
            {
                mr.sharedMaterial = hubMR.sharedMaterial;
            }
        }

        // Calculate arm thickness
        float thickness = CalculateArmThickness(Vector3.Distance(hubSurfacePoint, rotorHubBottom));

        // Generate the arm mesh
        Mesh armMesh;
        if (needsRingAvoidance)
        {
            armMesh = GenerateArmWithRingAvoidance(hubSurfacePoint, rotorHubBottom, rotor, thickness);
        }
        else
        {
            armMesh = GenerateStraightArm(hubSurfacePoint, rotorHubBottom, thickness);
        }

        mf.mesh = armMesh;

        return armObj;
    }

    private Vector3 GetHubSurfacePoint(Vector3 direction)
    {
        if (generatedBody == null) return Vector3.zero;

        direction.Normalize();
        
        // Approximate surface point based on hub shape and scale
        Vector3 scaledDirection = direction;
        scaledDirection.x *= generatedBody.scale.x;
        scaledDirection.y *= generatedBody.scale.y;
        scaledDirection.z *= generatedBody.scale.z;

        // Apply taper adjustment
        if (generatedBody.taper > 0f)
        {
            if (generatedBody.taperDirection == ProceduralDroneBody.TaperDirection.BottomToTop)
            {
                float taperFactor = 1f - generatedBody.taper * 0.5f;
                scaledDirection.x *= taperFactor;
                scaledDirection.z *= taperFactor;
            }
        }

        return scaledDirection * 0.5f;
    }

    private float CalculateArmThickness(float armLength)
    {
        if (autoScaleArmThickness)
        {
            // Scale thickness based on arm length
            float baseThickness = Mathf.Lerp(armThicknessRange.x, armThicknessRange.y, armThicknessScale);
            return baseThickness * Mathf.Lerp(0.5f, 1.5f, armLength / rotorDistanceRange.y);
        }
        else
        {
            return Mathf.Lerp(armThicknessRange.x, armThicknessRange.y, armThicknessScale);
        }
    }

    private Mesh GenerateStraightArm(Vector3 start, Vector3 end, float thickness)
    {
        Mesh mesh = new Mesh();

        if (armShape == ArmShape.Cylindrical)
        {
            mesh = GenerateCylindricalArm(start, end, thickness);
        }
        else
        {
            mesh = GenerateRectangularArm(start, end, thickness);
        }

        return mesh;
    }

    private Mesh GenerateArmWithRingAvoidance(Vector3 start, Vector3 end, ProceduralRotor rotor, float thickness)
    {
        // Calculate ring parameters in world space
        float bladeReach = rotor.hubRadius + rotor.bladeLength;
        float ringInnerRadius = bladeReach * 1.1f;
        float ringOuterRadius = ringInnerRadius + rotor.ringThickness;
        float avoidanceRadius = ringOuterRadius + ringClearance;

        // Ring center in world space (rotor position, at rotor's Y level)
        Vector3 ringCenter = rotor.transform.localPosition;

        // Check if the arm path intersects with the ring's cylindrical volume
        // We need to check the horizontal distance from the arm path to the ring center
        Vector3 startFlat = new Vector3(start.x, ringCenter.y, start.z);
        Vector3 endFlat = new Vector3(end.x, ringCenter.y, end.z);
        Vector3 ringCenterFlat = new Vector3(ringCenter.x, ringCenter.y, ringCenter.z);

        // Find closest point on line segment to ring center
        Vector3 lineDirection = (endFlat - startFlat).normalized;
        float lineLength = Vector3.Distance(startFlat, endFlat);
        
        Vector3 toRingCenter = ringCenterFlat - startFlat;
        float projectionLength = Vector3.Dot(toRingCenter, lineDirection);
        projectionLength = Mathf.Clamp(projectionLength, 0, lineLength);
        
        Vector3 closestPoint = startFlat + lineDirection * projectionLength;
        float distanceToRing = Vector3.Distance(closestPoint, ringCenterFlat);

        // If the arm passes through the ring area, create waypoints to go around
        if (distanceToRing < avoidanceRadius)
        {
            List<Vector3> waypoints = new List<Vector3>();
            waypoints.Add(start);

            // Decide whether to go up or down based on the start position
            bool goUp = start.y < ringCenter.y;
            float verticalOffset = (avoidanceRadius - distanceToRing + ringClearance) * 1.5f;
            
            // Calculate the point where we need to start avoiding (approaching the ring)
            Vector3 toEnd = end - start;
            float totalDistance = toEnd.magnitude;
            
            // First waypoint: start curving up/down before hitting the ring
            float approachDistance = totalDistance * 0.33f; // Start avoiding at 1/3 of the way
            Vector3 approachPoint = start + toEnd.normalized * approachDistance;
            approachPoint.y += goUp ? verticalOffset * 0.5f : -verticalOffset * 0.5f;
            waypoints.Add(approachPoint);

            // Second waypoint: maximum clearance point (at the ring's location horizontally)
            Vector3 clearancePoint = start + toEnd.normalized * (totalDistance * 0.5f);
            clearancePoint.y += goUp ? verticalOffset : -verticalOffset;
            waypoints.Add(clearancePoint);

            // Third waypoint: start descending/ascending back down/up
            float exitDistance = totalDistance * 0.67f; // End avoiding at 2/3 of the way
            Vector3 exitPoint = start + toEnd.normalized * exitDistance;
            exitPoint.y += goUp ? verticalOffset * 0.5f : -verticalOffset * 0.5f;
            waypoints.Add(exitPoint);

            waypoints.Add(end);

            return GenerateSegmentedArm(waypoints, thickness);
        }
        else
        {
            // No ring intersection, use straight arm
            return GenerateStraightArm(start, end, thickness);
        }
    }

    private Mesh GenerateSegmentedArm(List<Vector3> waypoints, float thickness)
    {
        if (waypoints.Count < 2) return new Mesh();

        List<CombineInstance> segments = new List<CombineInstance>();

        for (int i = 0; i < waypoints.Count - 1; i++)
        {
            Mesh segmentMesh = GenerateStraightArm(waypoints[i], waypoints[i + 1], thickness);
            CombineInstance ci = new CombineInstance();
            ci.mesh = segmentMesh;
            ci.transform = Matrix4x4.identity;
            segments.Add(ci);
        }

        Mesh combinedMesh = new Mesh();
        combinedMesh.CombineMeshes(segments.ToArray(), true, true);

        // Clean up temporary meshes
        foreach (var ci in segments)
        {
            if (ci.mesh != null)
            {
                DestroyImmediate(ci.mesh);
            }
        }

        return combinedMesh;
    }

    private Mesh GenerateCylindricalArm(Vector3 start, Vector3 end, float radius)
    {
        Mesh mesh = new Mesh();

        Vector3 direction = (end - start);
        float length = direction.magnitude;
        direction.Normalize();

        int segments = armSegments;
        int vertexCount = (segments + 1) * 2 + 2; // sides + caps
        int triangleCount = segments * 6 + segments * 6;

        Vector3[] vertices = new Vector3[vertexCount];
        int[] triangles = new int[triangleCount * 3];

        // Calculate perpendicular vectors for circle
        Vector3 perpendicular = Vector3.Cross(direction, Vector3.up);
        if (perpendicular.magnitude < 0.001f)
        {
            perpendicular = Vector3.Cross(direction, Vector3.right);
        }
        perpendicular.Normalize();
        Vector3 perpendicular2 = Vector3.Cross(direction, perpendicular).normalized;

        int vertIndex = 0;
        int triIndex = 0;

        // Generate cylinder sides
        for (int i = 0; i <= segments; i++)
        {
            float angle = (float)i / segments * Mathf.PI * 2f;
            float x = Mathf.Cos(angle) * radius;
            float y = Mathf.Sin(angle) * radius;

            Vector3 offset = perpendicular * x + perpendicular2 * y;

            vertices[vertIndex] = start + offset;
            vertices[vertIndex + 1] = end + offset;

            if (i < segments)
            {
                int current = vertIndex;
                int next = vertIndex + 2;

                triangles[triIndex++] = current;
                triangles[triIndex++] = next;
                triangles[triIndex++] = current + 1;

                triangles[triIndex++] = current + 1;
                triangles[triIndex++] = next;
                triangles[triIndex++] = next + 1;
            }

            vertIndex += 2;
        }

        // Cap centers
        int startCapCenter = vertIndex;
        vertices[vertIndex++] = start;
        int endCapCenter = vertIndex;
        vertices[vertIndex++] = end;

        // Start cap
        for (int i = 0; i < segments; i++)
        {
            triangles[triIndex++] = startCapCenter;
            triangles[triIndex++] = ((i + 1) % segments) * 2;
            triangles[triIndex++] = i * 2;
        }

        // End cap
        for (int i = 0; i < segments; i++)
        {
            triangles[triIndex++] = endCapCenter;
            triangles[triIndex++] = i * 2 + 1;
            triangles[triIndex++] = ((i + 1) % segments) * 2 + 1;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        return mesh;
    }

    private Mesh GenerateRectangularArm(Vector3 start, Vector3 end, float thickness)
    {
        Mesh mesh = new Mesh();

        Vector3 direction = (end - start);
        float length = direction.magnitude;
        direction.Normalize();

        // Calculate perpendicular vectors
        Vector3 perpendicular = Vector3.Cross(direction, Vector3.up);
        if (perpendicular.magnitude < 0.001f)
        {
            perpendicular = Vector3.Cross(direction, Vector3.right);
        }
        perpendicular.Normalize();
        Vector3 perpendicular2 = Vector3.Cross(direction, perpendicular).normalized;

        float halfThickness = thickness * 0.5f;

        // Create box vertices
        Vector3[] corners = new Vector3[8];
        
        // Start face (4 corners)
        corners[0] = start - perpendicular * halfThickness - perpendicular2 * halfThickness;
        corners[1] = start + perpendicular * halfThickness - perpendicular2 * halfThickness;
        corners[2] = start + perpendicular * halfThickness + perpendicular2 * halfThickness;
        corners[3] = start - perpendicular * halfThickness + perpendicular2 * halfThickness;

        // End face (4 corners)
        corners[4] = end - perpendicular * halfThickness - perpendicular2 * halfThickness;
        corners[5] = end + perpendicular * halfThickness - perpendicular2 * halfThickness;
        corners[6] = end + perpendicular * halfThickness + perpendicular2 * halfThickness;
        corners[7] = end - perpendicular * halfThickness + perpendicular2 * halfThickness;

        Vector3[] vertices = new Vector3[24]; // 4 vertices per face * 6 faces
        int[] triangles = new int[36]; // 2 triangles per face * 6 faces * 3 vertices

        // Build faces
        int vertIndex = 0;
        int triIndex = 0;

        // Front face
        AddQuad(vertices, triangles, ref vertIndex, ref triIndex, corners[0], corners[1], corners[2], corners[3]);
        // Back face
        AddQuad(vertices, triangles, ref vertIndex, ref triIndex, corners[5], corners[4], corners[7], corners[6]);
        // Top face
        AddQuad(vertices, triangles, ref vertIndex, ref triIndex, corners[3], corners[2], corners[6], corners[7]);
        // Bottom face
        AddQuad(vertices, triangles, ref vertIndex, ref triIndex, corners[1], corners[0], corners[4], corners[5]);
        // Left face
        AddQuad(vertices, triangles, ref vertIndex, ref triIndex, corners[4], corners[0], corners[3], corners[7]);
        // Right face
        AddQuad(vertices, triangles, ref vertIndex, ref triIndex, corners[1], corners[5], corners[6], corners[2]);

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        return mesh;
    }

    private void AddQuad(Vector3[] vertices, int[] triangles, ref int vertIndex, ref int triIndex,
                         Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3)
    {
        int startVert = vertIndex;
        
        vertices[vertIndex++] = v0;
        vertices[vertIndex++] = v1;
        vertices[vertIndex++] = v2;
        vertices[vertIndex++] = v3;

        triangles[triIndex++] = startVert;
        triangles[triIndex++] = startVert + 1;
        triangles[triIndex++] = startVert + 2;

        triangles[triIndex++] = startVert;
        triangles[triIndex++] = startVert + 2;
        triangles[triIndex++] = startVert + 3;
    }
}