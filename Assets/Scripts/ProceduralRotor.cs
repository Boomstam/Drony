using UnityEngine;

public class ProceduralRotor : MonoBehaviour
{
    [Header("Mesh Reference")]
    [SerializeField] private MeshFilter meshFilter;

    [Header("Blade Settings")]
    [SerializeField, Range(2, 8)] private int numberOfBlades = 4;
    [SerializeField] private float bladeLength = 1f;
    [SerializeField] private float bladeWidth = 0.2f;
    [SerializeField] private float bladeThickness = 0.05f;

    [Header("Hub Settings")]
    [SerializeField] private float hubRadius = 0.15f;
    [SerializeField] private float hubHeight = 0.1f;

    [Header("Ring Settings")]
    [SerializeField] private bool includeRing = false;
    [SerializeField] private float ringRadius = 1.2f;
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

        // Simple rectangular blade
        float halfWidth = bladeWidth * 0.5f;
        float halfThickness = bladeThickness * 0.5f;

        Vector3[] vertices = new Vector3[]
        {
            // Front face
            new Vector3(0, halfThickness, -halfWidth),
            new Vector3(bladeLength, halfThickness, -halfWidth),
            new Vector3(bladeLength, halfThickness, halfWidth),
            new Vector3(0, halfThickness, halfWidth),

            // Back face
            new Vector3(0, -halfThickness, -halfWidth),
            new Vector3(bladeLength, -halfThickness, -halfWidth),
            new Vector3(bladeLength, -halfThickness, halfWidth),
            new Vector3(0, -halfThickness, halfWidth),
        };

        int[] triangles = new int[]
        {
            // Front
            0, 1, 2,
            0, 2, 3,
            
            // Back
            4, 6, 5,
            4, 7, 6,
            
            // Top
            3, 2, 6,
            3, 6, 7,
            
            // Bottom
            0, 5, 1,
            0, 4, 5,
            
            // Left
            0, 3, 7,
            0, 7, 4,
            
            // Right
            1, 6, 2,
            1, 5, 6,
        };

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        return mesh;
    }

    private Mesh GenerateRing()
    {
        Mesh mesh = new Mesh();
        
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