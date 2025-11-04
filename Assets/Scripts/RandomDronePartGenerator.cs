using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class RandomDronePartGenerator : MonoBehaviour
{
    [Header("Component References")]
    [SerializeField] private ProceduralDroneHub droneHubPrefab;
    [SerializeField] private ProceduralRotor droneRotorPrefab;

    [Header("Generated Instances (Do Not Assign)")]
    private ProceduralDroneHub generatedHub;
    private List<ProceduralRotor> generatedRotors = new List<ProceduralRotor>();
    private List<GameObject> generatedArms = new List<GameObject>();

    [Header("Seeds")]
    [SerializeField] private int mainSeed = 42;
    [SerializeField] private int rotorSeed = 100;
    [SerializeField] private int hubSeed = 200;
    [SerializeField] private int armSeed = 300;

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
    [SerializeField] private Vector2 hubRadiusRange = new Vector2(0.08f, 0.3f);
    [SerializeField] private Vector2 hubHeightRange = new Vector2(0.05f, 0.2f);
    [SerializeField] private Vector2 ringThicknessRange = new Vector2(0.03f, 0.12f);
    [SerializeField] private Vector2 hubScaleRange = new Vector2(0.2f, 1.0f);
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
        mainSeed = Random.Range(0, 1000000);
        rotorSeed = Random.Range(0, 1000000);
        hubSeed = Random.Range(0, 1000000);
        armSeed = Random.Range(0, 1000000);
        
        EnsureComponentsExist();
        
        Random.InitState(hubSeed);
        RandomizeHubInternal();
        
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
        
        #if UNITY_EDITOR
        EditorUtility.SetDirty(this);
        #endif
        
        Debug.Log($"Randomized all | mainSeed: {mainSeed} | rotorSeed: {rotorSeed} | hubSeed: {hubSeed} | armSeed: {armSeed}");
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
        // Delete old instances
        if (generatedHub != null)
        {
            DestroyImmediate(generatedHub.gameObject);
            generatedHub = null;
        }
        
        foreach (var rotor in generatedRotors)
        {
            if (rotor != null)
            {
                DestroyImmediate(rotor.gameObject);
            }
        }
        generatedRotors.Clear();

        foreach (var arm in generatedArms)
        {
            if (arm != null)
            {
                DestroyImmediate(arm);
            }
        }
        generatedArms.Clear();

        // Create new hub instance
        if (droneHubPrefab != null)
        {
            GameObject hubObj = new GameObject("GeneratedHub");
            hubObj.transform.SetParent(transform);
            hubObj.transform.localPosition = Vector3.zero;
            hubObj.transform.localRotation = Quaternion.identity;
            
            MeshFilter hubMF = hubObj.AddComponent<MeshFilter>();
            MeshRenderer hubMR = hubObj.AddComponent<MeshRenderer>();
            generatedHub = hubObj.AddComponent<ProceduralDroneHub>();
            
            MeshRenderer prefabMR = droneHubPrefab.GetComponent<MeshRenderer>();
            if (prefabMR != null && prefabMR.sharedMaterial != null)
            {
                hubMR.sharedMaterial = prefabMR.sharedMaterial;
            }
            
            generatedHub.meshFilter = hubMF;
        }

        // Create rotor instances
        if (droneRotorPrefab != null)
        {
            for (int i = 0; i < rotorCount; i++)
            {
                GameObject rotorObj = new GameObject($"GeneratedRotor_{i}");
                rotorObj.transform.SetParent(transform);
                rotorObj.transform.localPosition = Vector3.zero;
                rotorObj.transform.localRotation = Quaternion.identity;
                
                MeshFilter rotorMF = rotorObj.AddComponent<MeshFilter>();
                MeshRenderer rotorMR = rotorObj.AddComponent<MeshRenderer>();
                ProceduralRotor rotor = rotorObj.AddComponent<ProceduralRotor>();
                
                MeshRenderer prefabMR = droneRotorPrefab.GetComponent<MeshRenderer>();
                if (prefabMR != null && prefabMR.sharedMaterial != null)
                {
                    rotorMR.sharedMaterial = prefabMR.sharedMaterial;
                }
                
                rotor.meshFilter = rotorMF;
                
                generatedRotors.Add(rotor);
            }
        }
    }

    private void RandomizeLayoutInternal(int seed)
    {
        Random.InitState(seed);
        
        float rand = Random.value;
        if (rand < 0.33f) rotorCount = 4;
        else if (rand < 0.66f) rotorCount = 6;
        else rotorCount = 8;

        float hubMaxDimension = 0.5f;
        float rotorMaxLength = 1.0f;
        
        if (generatedHub != null)
        {
            hubMaxDimension = Mathf.Max(generatedHub.scale.x, generatedHub.scale.y, generatedHub.scale.z);
        }
        
        if (generatedRotors.Count > 0 && generatedRotors[0] != null)
        {
            rotorMaxLength = generatedRotors[0].bladeLength + generatedRotors[0].hubRadius;
        }

        float minSafeDistance = (hubMaxDimension * 0.5f) + rotorMaxLength + 0.5f;
        float actualMinDistance = Mathf.Max(rotorDistanceRange.x, minSafeDistance);
        
        currentRotorDistance = Mathf.Lerp(actualMinDistance, rotorDistanceRange.y, Random.value);
        currentVerticalOffset = Mathf.Lerp(rotorVerticalOffsetRange.x, rotorVerticalOffsetRange.y, Random.value);
        currentTiltAngle = Mathf.Lerp(rotorTiltAngleRange.x, rotorTiltAngleRange.y, Random.value);

        float angleStep = 360f / rotorCount;
        for (int i = 0; i < generatedRotors.Count && i < rotorCount; i++)
        {
            if (generatedRotors[i] != null)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector3 position = new Vector3(
                    Mathf.Cos(angle) * currentRotorDistance,
                    currentVerticalOffset,
                    Mathf.Sin(angle) * currentRotorDistance
                );
                
                generatedRotors[i].transform.localPosition = position;
                generatedRotors[i].transform.localRotation = Quaternion.Euler(currentTiltAngle, 0, 0);
            }
        }
        
        Debug.Log($"Randomized layout with seed: {seed} | Rotors: {rotorCount} | Distance: {currentRotorDistance:F2}");
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

        Debug.Log($"Randomized rotor with seed: {rotorSeed}");
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

        float hubRadius = Mathf.Lerp(hubRadiusRange.x, hubRadiusRange.y, Random.value);
        float hubHeight = Mathf.Lerp(hubHeightRange.x, hubHeightRange.y, Random.value);

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

    public void RandomizeHub()
    {
        if (generatedHub == null)
        {
            Debug.LogError("No hub exists! Use Randomize All first.");
            return;
        }

        hubSeed = Random.Range(0, 1000000);
        Random.InitState(hubSeed);
        RandomizeHubInternal();
        
        if (generateArms)
        {
            GenerateArms();
        }
        
        #if UNITY_EDITOR
        EditorUtility.SetDirty(this);
        #endif
        
        Debug.Log($"Randomized hub with seed: {hubSeed}");
    }

    private void RandomizeHubInternal()
    {
        generatedHub.baseShape = (ProceduralDroneHub.BaseShape)Random.Range(0, 2);

        generatedHub.scale = new Vector3(
            Mathf.Lerp(hubScaleRange.x, hubScaleRange.y, Random.value),
            Mathf.Lerp(hubScaleRange.x, hubScaleRange.y, Random.value),
            Mathf.Lerp(hubScaleRange.x, hubScaleRange.y, Random.value)
        );

        generatedHub.taper = Random.value;
        generatedHub.taperDirection = (ProceduralDroneHub.TaperDirection)Random.Range(0, 2);

        generatedHub.GenerateHub();
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
        if (generatedHub != null)
        {
            DestroyImmediate(generatedHub.gameObject);
            generatedHub = null;
        }

        foreach (var rotor in generatedRotors)
        {
            if (rotor != null)
            {
                DestroyImmediate(rotor.gameObject);
            }
        }
        generatedRotors.Clear();

        foreach (var arm in generatedArms)
        {
            if (arm != null)
            {
                DestroyImmediate(arm);
            }
        }
        generatedArms.Clear();

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

        if (generatedHub == null || generatedRotors.Count == 0)
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
        armObj.transform.SetParent(transform);
        armObj.transform.localPosition = Vector3.zero;
        armObj.transform.localRotation = Quaternion.identity;

        MeshFilter mf = armObj.AddComponent<MeshFilter>();
        MeshRenderer mr = armObj.AddComponent<MeshRenderer>();

        // Copy material from hub
        if (generatedHub != null)
        {
            MeshRenderer hubMR = generatedHub.GetComponent<MeshRenderer>();
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
        if (generatedHub == null) return Vector3.zero;

        direction.Normalize();
        
        // Approximate surface point based on hub shape and scale
        Vector3 scaledDirection = direction;
        scaledDirection.x *= generatedHub.scale.x;
        scaledDirection.y *= generatedHub.scale.y;
        scaledDirection.z *= generatedHub.scale.z;

        // Apply taper adjustment
        if (generatedHub.taper > 0f)
        {
            if (generatedHub.taperDirection == ProceduralDroneHub.TaperDirection.BottomToTop)
            {
                float taperFactor = 1f - generatedHub.taper * 0.5f;
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
        // Calculate ring parameters
        float bladeReach = rotor.hubRadius + rotor.bladeLength;
        float ringInnerRadius = bladeReach * 1.1f;
        float ringOuterRadius = ringInnerRadius + rotor.ringThickness;
        float avoidanceRadius = ringOuterRadius + ringClearance;

        // Check if straight line intersects the ring
        Vector3 ringCenter = end;
        ringCenter.y = 0; // Ring is at y=0 in rotor's local space, but we need world space

        Vector3 armDirection = (end - start).normalized;
        float armLength = Vector3.Distance(start, end);

        // Project start point onto ring plane (Y=0 for simplicity)
        Vector3 startOnPlane = start;
        startOnPlane.y = end.y;

        float distanceToRingCenter = Vector3.Distance(new Vector3(startOnPlane.x, 0, startOnPlane.z), 
                                                       new Vector3(end.x, 0, end.z));

        // If the arm passes through the ring area, create waypoints to go around
        if (distanceToRingCenter < avoidanceRadius)
        {
            List<Vector3> waypoints = new List<Vector3>();
            waypoints.Add(start);

            // Calculate two waypoints that arc around the ring
            Vector3 toRotor = (end - start);
            Vector3 toRotorFlat = new Vector3(toRotor.x, 0, toRotor.z).normalized;
            Vector3 perpendicular = Vector3.Cross(toRotorFlat, Vector3.up).normalized;

            // Midpoint at avoidance radius
            float midDistance = Vector3.Distance(start, end) * 0.5f;
            Vector3 midPoint = start + (end - start) * 0.5f;
            
            // Offset midpoint to avoid ring
            Vector3 offsetDirection = (midPoint - new Vector3(end.x, midPoint.y, end.z)).normalized;
            midPoint += offsetDirection * (avoidanceRadius - distanceToRingCenter + ringClearance);

            waypoints.Add(midPoint);
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