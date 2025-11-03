using UnityEngine;
using System.Collections.Generic;

public enum BladeShape
{
    Triangular,    // Tapered tip
    Rectangular,   // Bullnose style
    Curved         // Petal/scimitar style
}

public class ProceduralRotor : MonoBehaviour
{
    [Header("Mesh Reference")]
    [SerializeField] private MeshFilter meshFilter;

    [Header("Blade Settings")]
    [SerializeField, Range(2, 8)] private int numberOfBlades = 4;
    [SerializeField] private float bladeLength = 1f;
    [SerializeField] private float bladeWidth = 0.2f;
    [SerializeField] private float bladeThickness = 0.05f;
    [SerializeField] private BladeShape bladeShape = BladeShape.Triangular;
    [SerializeField, Range(-1f, 1f)] private float bladeCurveAmount = 0.2f; // Negative curves backward, positive curves forward
    [SerializeField, Range(0f, 3f)] private float petalShape = 1f; // 0 = hourglass, 1 = straight, >1 = petal bulge
    [SerializeField, Range(-1f, 1f)] private float bladeTwist = 0f; // Twist along blade length: -1 = -180°, 0 = flat, 1 = +180°

    [Header("Hub Settings")]
    [SerializeField] private float hubRadius = 0.15f;
    [SerializeField] private float hubHeight = 0.1f;

    [Header("Ring Settings")]
    [SerializeField] private bool includeRing = false;
    [SerializeField] private float ringThickness = 0.05f;

    public void GenerateRotor()
    {
        if (meshFilter == null)
        {
            Debug.LogError("MeshFilter is not assigned!");
            return;
        }

        Mesh mesh = new Mesh();
        mesh.name = "Procedural Rotor";

        // We'll combine all parts into one mesh
        CombineInstance[] combine = includeRing ? new CombineInstance[numberOfBlades + 2] : new CombineInstance[numberOfBlades + 1];
        int combineIndex = 0;

        // Generate hub
        Mesh hubMesh = GenerateHub();
        combine[combineIndex].mesh = hubMesh;
        combine[combineIndex].transform = Matrix4x4.identity;
        combineIndex++;

        // Generate blades
        float angleStep = 360f / numberOfBlades;
        for (int i = 0; i < numberOfBlades; i++)
        {
            float angle = i * angleStep;
            Mesh bladeMesh = GenerateBlade();
            combine[combineIndex].mesh = bladeMesh;
            combine[combineIndex].transform = Matrix4x4.Rotate(Quaternion.Euler(0, angle, 0)) * 
                                              Matrix4x4.Translate(new Vector3(hubRadius, 0, 0));
            combineIndex++;
        }

        // Generate ring if enabled
        if (includeRing)
        {
            Mesh ringMesh = GenerateRing();
            combine[combineIndex].mesh = ringMesh;
            combine[combineIndex].transform = Matrix4x4.identity;
        }

        mesh.CombineMeshes(combine, true, true);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        meshFilter.sharedMesh = mesh;

        // Clean up temporary meshes
        DestroyImmediate(hubMesh);
        foreach (var ci in combine)
        {
            if (ci.mesh != null && ci.mesh != mesh)
            {
                DestroyImmediate(ci.mesh);
            }
        }
    }

    private Mesh GenerateHub()
    {
        Mesh mesh = new Mesh();
        
        int segments = 16;
        int vertexCount = (segments + 1) * 2 + 2; // sides + top cap + bottom cap
        int triangleCount = segments * 6 + segments * 6; // sides + caps
        
        Vector3[] vertices = new Vector3[vertexCount];
        int[] triangles = new int[triangleCount * 3];
        
        float halfHeight = hubHeight * 0.5f;
        int vertIndex = 0;
        int triIndex = 0;

        // Generate cylinder sides
        for (int i = 0; i <= segments; i++)
        {
            float angle = (float)i / segments * Mathf.PI * 2f;
            float x = Mathf.Cos(angle) * hubRadius;
            float z = Mathf.Sin(angle) * hubRadius;

            vertices[vertIndex] = new Vector3(x, halfHeight, z);
            vertices[vertIndex + 1] = new Vector3(x, -halfHeight, z);

            if (i < segments)
            {
                int current = vertIndex;
                int next = vertIndex + 2;

                // Two triangles for this segment
                triangles[triIndex++] = current;
                triangles[triIndex++] = next;
                triangles[triIndex++] = current + 1;

                triangles[triIndex++] = current + 1;
                triangles[triIndex++] = next;
                triangles[triIndex++] = next + 1;
            }

            vertIndex += 2;
        }

        // Top cap center
        int topCenterIndex = vertIndex;
        vertices[vertIndex++] = new Vector3(0, halfHeight, 0);

        // Bottom cap center
        int bottomCenterIndex = vertIndex;
        vertices[vertIndex++] = new Vector3(0, -halfHeight, 0);

        // Top cap triangles
        for (int i = 0; i < segments; i++)
        {
            triangles[triIndex++] = topCenterIndex;
            triangles[triIndex++] = i * 2;
            triangles[triIndex++] = ((i + 1) % segments) * 2;
        }

        // Bottom cap triangles
        for (int i = 0; i < segments; i++)
        {
            triangles[triIndex++] = bottomCenterIndex;
            triangles[triIndex++] = ((i + 1) % segments) * 2 + 1;
            triangles[triIndex++] = i * 2 + 1;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        return mesh;
    }

    private Mesh GenerateBlade()
    {
        Mesh mesh = new Mesh();

        float halfWidth = bladeWidth * 0.5f;
        float halfThickness = bladeThickness * 0.5f;

        Vector3[] vertices;
        int[] triangles;

        switch (bladeShape)
        {
            case BladeShape.Triangular:
                vertices = GenerateTriangularBladeVertices(halfWidth, halfThickness);
                break;
            case BladeShape.Rectangular:
                vertices = GenerateRectangularBladeVertices(halfWidth, halfThickness);
                break;
            case BladeShape.Curved:
                vertices = GenerateCurvedBladeVertices(halfWidth, halfThickness);
                break;
            default:
                vertices = GenerateRectangularBladeVertices(halfWidth, halfThickness);
                break;
        }

        // Determine if we need segmented triangles (for petal shapes or curved blades)
        bool needsSegmentedTriangles = (Mathf.Abs(petalShape - 1f) > 0.01f) || (bladeShape == BladeShape.Curved);
        triangles = GenerateBladeTriangles(vertices.Length, needsSegmentedTriangles);

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        return mesh;
    }

    private Vector3[] GenerateTriangularBladeVertices(float halfWidth, float halfThickness)
    {
        // Tapered tip with optional petal/hourglass shape
        // Use multiple segments if petal shape is not at neutral (1.0)
        if (Mathf.Abs(petalShape - 1f) > 0.01f)
        {
            return GeneratePetalShapedBladeVertices(halfWidth, halfThickness, true);
        }
        
        // Simple 4-vertex triangle for non-petal
        float tipWidth = halfWidth * 0.15f;

        Vector3[] vertices = new Vector3[]
        {
            // Front face (top)
            new Vector3(0, halfThickness, -halfWidth),           // Base left
            new Vector3(bladeLength, halfThickness, -tipWidth),  // Tip left
            new Vector3(bladeLength, halfThickness, tipWidth),   // Tip right
            new Vector3(0, halfThickness, halfWidth),            // Base right

            // Back face (bottom)
            new Vector3(0, -halfThickness, -halfWidth),
            new Vector3(bladeLength, -halfThickness, -tipWidth),
            new Vector3(bladeLength, -halfThickness, tipWidth),
            new Vector3(0, -halfThickness, halfWidth),
        };
        
        // Apply tilt to all vertices
        for (int i = 0; i < vertices.Length; i++)
        {
            float t = vertices[i].x / bladeLength; // Calculate t based on X position
            vertices[i] = ApplyBladeTwist(vertices[i], t);
        }
        
        return vertices;
    }

    private Vector3[] GenerateRectangularBladeVertices(float halfWidth, float halfThickness)
    {
        // Bullnose style with optional petal/hourglass shape
        if (Mathf.Abs(petalShape - 1f) > 0.01f)
        {
            return GeneratePetalShapedBladeVertices(halfWidth, halfThickness, false);
        }
        
        // Simple rectangular blade
        Vector3[] vertices = new Vector3[]
        {
            // Front face (top)
            new Vector3(0, halfThickness, -halfWidth),
            new Vector3(bladeLength, halfThickness, -halfWidth),
            new Vector3(bladeLength, halfThickness, halfWidth),
            new Vector3(0, halfThickness, halfWidth),

            // Back face (bottom)
            new Vector3(0, -halfThickness, -halfWidth),
            new Vector3(bladeLength, -halfThickness, -halfWidth),
            new Vector3(bladeLength, -halfThickness, halfWidth),
            new Vector3(0, -halfThickness, halfWidth),
        };
        
        // Apply tilt to all vertices
        for (int i = 0; i < vertices.Length; i++)
        {
            float t = vertices[i].x / bladeLength; // Calculate t based on X position
            vertices[i] = ApplyBladeTwist(vertices[i], t);
        }
        
        return vertices;
    }

    private Vector3[] GeneratePetalShapedBladeVertices(float halfWidth, float halfThickness, bool tapered)
    {
        // Generate blade with petal shape using multiple segments
        int segments = 8;
        List<Vector3> verticesList = new List<Vector3>();

        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            float x = t * bladeLength;
            
            // Calculate width at this point using petal curve
            float widthAtPoint = CalculateBladeWidthAtPoint(t, halfWidth);

            // Top surface
            Vector3 topLeft = new Vector3(x, halfThickness, -widthAtPoint);
            Vector3 topRight = new Vector3(x, halfThickness, widthAtPoint);
            
            verticesList.Add(ApplyBladeTwist(topLeft, t));
            verticesList.Add(ApplyBladeTwist(topRight, t));
        }

        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            float x = t * bladeLength;
            float widthAtPoint = CalculateBladeWidthAtPoint(t, halfWidth);

            // Bottom surface
            Vector3 bottomLeft = new Vector3(x, -halfThickness, -widthAtPoint);
            Vector3 bottomRight = new Vector3(x, -halfThickness, widthAtPoint);
            
            verticesList.Add(ApplyBladeTwist(bottomLeft, t));
            verticesList.Add(ApplyBladeTwist(bottomRight, t));
        }

        return verticesList.ToArray();
    }

    private Vector3[] GenerateCurvedBladeVertices(float halfWidth, float halfThickness)
    {
        // Petal/scimitar style - curved swept shape
        int segments = 8;
        List<Vector3> verticesList = new List<Vector3>();

        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            
            // Width calculation using petal shape
            float widthAtPoint = CalculateBladeWidthAtPoint(t, halfWidth);
            
            // Add sideways sweep/curve using a smooth curve
            // bladeCurveAmount controls intensity and direction
            // Positive = sweep forward, Negative = sweep backward
            float sweepCurve = t * t * bladeLength * bladeCurveAmount;
            
            // To maintain constant radius, we need to compensate the X position
            // When we sweep in Z, we reduce X so that sqrt(x^2 + z^2) = bladeLength * t
            float targetRadius = t * bladeLength;
            float sweepCurveAbs = Mathf.Abs(sweepCurve);
            float actualX = Mathf.Sqrt(Mathf.Max(0, targetRadius * targetRadius - sweepCurveAbs * sweepCurveAbs));
            
            // Fallback to linear if the math doesn't work out (sweep too large)
            if (float.IsNaN(actualX))
            {
                actualX = targetRadius;
            }

            // Top surface - sweep creates offset in Z
            Vector3 topLeft = new Vector3(actualX, halfThickness, -widthAtPoint + sweepCurve);
            Vector3 topRight = new Vector3(actualX, halfThickness, widthAtPoint + sweepCurve);
            
            verticesList.Add(ApplyBladeTwist(topLeft, t));
            verticesList.Add(ApplyBladeTwist(topRight, t));
        }

        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            float widthAtPoint = CalculateBladeWidthAtPoint(t, halfWidth);
            float sweepCurve = t * t * bladeLength * bladeCurveAmount;
            
            float targetRadius = t * bladeLength;
            float sweepCurveAbs = Mathf.Abs(sweepCurve);
            float actualX = Mathf.Sqrt(Mathf.Max(0, targetRadius * targetRadius - sweepCurveAbs * sweepCurveAbs));
            
            if (float.IsNaN(actualX))
            {
                actualX = targetRadius;
            }

            // Bottom surface
            Vector3 bottomLeft = new Vector3(actualX, -halfThickness, -widthAtPoint + sweepCurve);
            Vector3 bottomRight = new Vector3(actualX, -halfThickness, widthAtPoint + sweepCurve);
            
            verticesList.Add(ApplyBladeTwist(bottomLeft, t));
            verticesList.Add(ApplyBladeTwist(bottomRight, t));
        }

        return verticesList.ToArray();
    }

    private Vector3 ApplyBladeTwist(Vector3 position, float t)
    {
        // t goes from 0 (base) to 1 (tip)
        // Twist progressively rotates the blade's cross-section around the radial direction
        // This creates the propeller pitch/twist effect
        
        if (Mathf.Abs(bladeTwist) < 0.01f)
        {
            return position; // No twist, skip calculation
        }
        
        // Calculate rotation angle at this point along the blade
        // bladeTwist ranges from -1 to 1, corresponding to -180° to +180°
        float maxTwistDegrees = 180f;
        float twistAngleAtPoint = bladeTwist * maxTwistDegrees * t; // Linear progression from 0 to max twist
        float twistRadians = twistAngleAtPoint * Mathf.Deg2Rad;
        
        // The blade is generated extending in +X direction (radially outward)
        // The blade's "thickness" is in Y, and "width" is in Z
        // We want to twist the thickness (Y) and width (Z) as we go along the blade (X)
        // BUT: we need to twist around the RADIAL axis (which is the blade's local forward/X axis)
        
        // Actually, for a propeller twist, we want the blade face to rotate
        // The blade starts "flat" (facing up in +Y), and twists so the face angles
        // This means rotating the Y-Z cross-section around the X-axis (radial axis)
        
        float x = position.x; // X position stays the same (distance along blade)
        float y = position.y; // Thickness direction
        float z = position.z; // Width direction
        
        // Rotate the cross-section (Y-Z) around the blade's radial axis (X)
        float rotatedY = y * Mathf.Cos(twistRadians) - z * Mathf.Sin(twistRadians);
        float rotatedZ = y * Mathf.Sin(twistRadians) + z * Mathf.Cos(twistRadians);
        
        return new Vector3(x, rotatedY, rotatedZ);
    }

    private float CalculateBladeWidthAtPoint(float t, float halfWidth)
    {
        // t goes from 0 (base) to 1 (tip)
        
        // Bulge curve: parabola that peaks at t=0.5
        float bulgeCurve = 4f * t * (1f - t); // Peaks at 0.5 with value of 1.0
        
        float tipWidth = halfWidth * 0.1f;
        
        // Base linear taper (what we get at petalShape = 1)
        float linearWidth = Mathf.Lerp(halfWidth, tipWidth, t);
        
        if (petalShape < 1f)
        {
            // HOURGLASS MODE (0 to 1)
            // At 0: pinches to near-zero at the middle
            // At 1: linear taper
            
            float hourglassAmount = 1f - petalShape; // 0 = no hourglass, 1 = extreme hourglass
            
            // Pinch width: inverse of bulge curve (narrow in middle, wide at ends)
            float pinchFactor = 1f - bulgeCurve; // 0 at middle, 1 at ends
            
            // Extreme hourglass can go very narrow
            float minPinchWidth = halfWidth * 0.01f; // Nearly a point
            float hourglassWidth = Mathf.Lerp(linearWidth, minPinchWidth, bulgeCurve * hourglassAmount);
            
            return hourglassWidth;
        }
        else if (petalShape > 1f)
        {
            // PETAL BULGE MODE (1+)
            // petalShape value is the width multiplier at the peak
            
            float bulgeMultiplier = petalShape; // 2.0 = 200% width, 3.0 = 300%, etc.
            float maxBulgeWidth = halfWidth * bulgeMultiplier;
            
            // Petal: starts narrow, bulges out, tapers to tip
            float petalWidth = Mathf.Lerp(halfWidth * 0.6f, maxBulgeWidth, bulgeCurve);
            
            // Quadratic taper to tip
            petalWidth = Mathf.Lerp(petalWidth, tipWidth, t * t);
            
            return petalWidth;
        }
        else
        {
            // petalShape == 1: straight linear taper
            return linearWidth;
        }
    }

    private int[] GenerateBladeTriangles(int vertexCount, bool needsSegmentedTriangles)
    {
        // For simple quad-based blades (triangular and rectangular without petal)
        if (!needsSegmentedTriangles)
        {
            return new int[]
            {
                // Front (top face)
                0, 1, 2,
                0, 2, 3,
                
                // Back (bottom face)
                4, 6, 5,
                4, 7, 6,
                
                // Top edge
                3, 2, 6,
                3, 6, 7,
                
                // Bottom edge
                0, 5, 1,
                0, 4, 5,
                
                // Left edge
                0, 3, 7,
                0, 7, 4,
                
                // Right edge
                1, 6, 2,
                1, 5, 6,
            };
        }

        // For curved blades with multiple segments
        List<int> trianglesList = new List<int>();
        int verticesPerRow = 2;
        // Total vertices is split: half for top, half for bottom
        int verticesPerSurface = vertexCount / 2;
        int segments = (verticesPerSurface / verticesPerRow) - 1;

        // Top surface
        for (int i = 0; i < segments; i++)
        {
            int baseIndex = i * verticesPerRow;
            
            trianglesList.Add(baseIndex);
            trianglesList.Add(baseIndex + verticesPerRow);
            trianglesList.Add(baseIndex + 1);
            
            trianglesList.Add(baseIndex + 1);
            trianglesList.Add(baseIndex + verticesPerRow);
            trianglesList.Add(baseIndex + verticesPerRow + 1);
        }

        // Bottom surface
        int bottomOffset = (segments + 1) * verticesPerRow;
        for (int i = 0; i < segments; i++)
        {
            int baseIndex = bottomOffset + i * verticesPerRow;
            
            trianglesList.Add(baseIndex);
            trianglesList.Add(baseIndex + 1);
            trianglesList.Add(baseIndex + verticesPerRow);
            
            trianglesList.Add(baseIndex + 1);
            trianglesList.Add(baseIndex + verticesPerRow + 1);
            trianglesList.Add(baseIndex + verticesPerRow);
        }

        // Left edge
        for (int i = 0; i < segments; i++)
        {
            int topIndex = i * verticesPerRow;
            int bottomIndex = bottomOffset + i * verticesPerRow;
            
            trianglesList.Add(topIndex);
            trianglesList.Add(bottomIndex + verticesPerRow);
            trianglesList.Add(bottomIndex);
            
            trianglesList.Add(topIndex);
            trianglesList.Add(topIndex + verticesPerRow);
            trianglesList.Add(bottomIndex + verticesPerRow);
        }

        // Right edge
        for (int i = 0; i < segments; i++)
        {
            int topIndex = i * verticesPerRow + 1;
            int bottomIndex = bottomOffset + i * verticesPerRow + 1;
            
            trianglesList.Add(topIndex);
            trianglesList.Add(bottomIndex);
            trianglesList.Add(bottomIndex + verticesPerRow);
            
            trianglesList.Add(topIndex);
            trianglesList.Add(bottomIndex + verticesPerRow);
            trianglesList.Add(topIndex + verticesPerRow);
        }

        return trianglesList.ToArray();
    }

    private Mesh GenerateRing()
    {
        Mesh mesh = new Mesh();
        
        // Ring inner edge should be 10% larger than the rotor blade reach
        float bladeReach = hubRadius + bladeLength;
        float ringInnerRadius = bladeReach * 1.1f;
        
        int segments = 32;
        int vertexCount = (segments + 1) * 2;
        int triangleCount = segments * 6;
        
        Vector3[] vertices = new Vector3[vertexCount];
        int[] triangles = new int[triangleCount * 3];
        
        // Ring extends outward from the inner radius
        float innerRadius = ringInnerRadius;
        float outerRadius = ringInnerRadius + ringThickness;
        
        int vertIndex = 0;
        int triIndex = 0;

        // Generate torus (simplified as a flat ring)
        for (int i = 0; i <= segments; i++)
        {
            float angle = (float)i / segments * Mathf.PI * 2f;
            float cosAngle = Mathf.Cos(angle);
            float sinAngle = Mathf.Sin(angle);

            vertices[vertIndex] = new Vector3(cosAngle * innerRadius, 0, sinAngle * innerRadius);
            vertices[vertIndex + 1] = new Vector3(cosAngle * outerRadius, 0, sinAngle * outerRadius);

            if (i < segments)
            {
                int current = vertIndex;
                int next = vertIndex + 2;

                // Two triangles for this segment (top face)
                triangles[triIndex++] = current;
                triangles[triIndex++] = current + 1;
                triangles[triIndex++] = next;

                triangles[triIndex++] = next;
                triangles[triIndex++] = current + 1;
                triangles[triIndex++] = next + 1;
            }

            vertIndex += 2;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        return mesh;
    }
}