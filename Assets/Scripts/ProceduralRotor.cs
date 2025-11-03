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
    [SerializeField, Range(0f, 1f)] private float bladeCurveAmount = 0.2f;

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

        triangles = GenerateBladeTriangles(vertices.Length);

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        return mesh;
    }

    private Vector3[] GenerateTriangularBladeVertices(float halfWidth, float halfThickness)
    {
        // Tapered tip - narrows to a point at the end
        float tipWidth = halfWidth * 0.1f; // 10% of base width at tip

        return new Vector3[]
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
    }

    private Vector3[] GenerateRectangularBladeVertices(float halfWidth, float halfThickness)
    {
        // Bullnose style - full width throughout
        return new Vector3[]
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
    }

    private Vector3[] GenerateCurvedBladeVertices(float halfWidth, float halfThickness)
    {
        // Petal/scimitar style - curved swept shape
        int segments = 8;
        List<Vector3> verticesList = new List<Vector3>();

        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            
            // Width tapers from base to tip (like triangular)
            float widthAtPoint = Mathf.Lerp(halfWidth, halfWidth * 0.15f, t);
            
            // Add sideways sweep/curve using a smooth curve
            // This creates the "scimitar" swept-back look
            // bladeCurveAmount controls the intensity (0 = straight, 1 = maximum curve)
            float sweepCurve = t * t * bladeLength * bladeCurveAmount;
            
            // To maintain constant radius, we need to compensate the X position
            // When we sweep in Z, we reduce X so that sqrt(x^2 + z^2) = bladeLength * t
            float targetRadius = t * bladeLength;
            float actualX = Mathf.Sqrt(targetRadius * targetRadius - sweepCurve * sweepCurve);
            
            // Fallback to linear if the math doesn't work out (sweep too large)
            if (float.IsNaN(actualX))
            {
                actualX = targetRadius;
            }

            // Top surface - sweep creates offset in positive Z (trailing edge swept back)
            verticesList.Add(new Vector3(actualX, halfThickness, -widthAtPoint + sweepCurve));
            verticesList.Add(new Vector3(actualX, halfThickness, widthAtPoint + sweepCurve));
        }

        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            float widthAtPoint = Mathf.Lerp(halfWidth, halfWidth * 0.15f, t);
            float sweepCurve = t * t * bladeLength * bladeCurveAmount;
            
            float targetRadius = t * bladeLength;
            float actualX = Mathf.Sqrt(targetRadius * targetRadius - sweepCurve * sweepCurve);
            
            if (float.IsNaN(actualX))
            {
                actualX = targetRadius;
            }

            // Bottom surface
            verticesList.Add(new Vector3(actualX, -halfThickness, -widthAtPoint + sweepCurve));
            verticesList.Add(new Vector3(actualX, -halfThickness, widthAtPoint + sweepCurve));
        }

        return verticesList.ToArray();
    }

    private int[] GenerateBladeTriangles(int vertexCount)
    {
        // For simple quad-based blades (triangular and rectangular)
        if (bladeShape != BladeShape.Curved)
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
        
        // Ring is 10% larger than the rotor blade reach
        float ringRadius = (hubRadius + bladeLength) * 1.1f;
        
        int segments = 32;
        int vertexCount = (segments + 1) * 2;
        int triangleCount = segments * 6;
        
        Vector3[] vertices = new Vector3[vertexCount];
        int[] triangles = new int[triangleCount * 3];
        
        float innerRadius = ringRadius - ringThickness * 0.5f;
        float outerRadius = ringRadius + ringThickness * 0.5f;
        
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