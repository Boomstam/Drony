using UnityEngine;

public class ProceduralDroneHub : MonoBehaviour
{
    [Header("Mesh Reference")]
    [SerializeField] public MeshFilter meshFilter;

    [Header("Hub Base Shape")]
    [SerializeField] public BaseShape baseShape = BaseShape.Sphere;
    [SerializeField] public Vector3 scale = new Vector3(0.5f, 0.5f, 0.5f); // XYZ independent scaling

    private const int SPHERE_SEGMENTS = 24; // Hidden resolution parameter

    [Header("Deformation")]
    [SerializeField, Range(0f, 1f)] public float taper = 0f; // 0 = no taper, 1 = extreme narrowing
    [SerializeField] public TaperDirection taperDirection = TaperDirection.BottomToTop;

    public enum BaseShape
    {
        Sphere,
        Cube
    }

    public enum TaperDirection
    {
        BottomToTop,    // Narrows from bottom (Y-) to top (Y+)
        BackToFront     // Narrows from back (X-) to front (X+)
    }

    public void GenerateHub()
    {
        if (meshFilter == null)
        {
            Debug.LogError("MeshFilter is not assigned!");
            return;
        }

        Mesh mesh = baseShape == BaseShape.Sphere ? GenerateSphereMesh() : GenerateCubeMesh();
        mesh.name = "Procedural Drone Hub";

        meshFilter.mesh = mesh;
    }

    private Mesh GenerateSphereMesh()
    {
        Mesh mesh = new Mesh();

        int verticalSegments = SPHERE_SEGMENTS;
        int horizontalSegments = SPHERE_SEGMENTS * 2;

        int vertexCount = (verticalSegments + 1) * (horizontalSegments + 1);
        Vector3[] vertices = new Vector3[vertexCount];

        // Generate UV sphere vertices
        int vertIndex = 0;
        for (int v = 0; v <= verticalSegments; v++)
        {
            float verticalAngle = Mathf.PI * (float)v / verticalSegments; // 0 to PI (top to bottom)
            float y = Mathf.Cos(verticalAngle);
            float ringRadius = Mathf.Sin(verticalAngle);

            for (int h = 0; h <= horizontalSegments; h++)
            {
                float horizontalAngle = 2f * Mathf.PI * (float)h / horizontalSegments; // 0 to 2PI
                float x = ringRadius * Mathf.Cos(horizontalAngle);
                float z = ringRadius * Mathf.Sin(horizontalAngle);

                // Base sphere vertex (unit sphere)
                Vector3 baseVertex = new Vector3(x, y, z);

                // Apply deformations
                Vector3 deformedVertex = ApplyDeformations(baseVertex);

                vertices[vertIndex++] = deformedVertex;
            }
        }

        // Generate triangles
        int triangleCount = verticalSegments * horizontalSegments * 6;
        int[] triangles = new int[triangleCount];
        int triIndex = 0;

        for (int v = 0; v < verticalSegments; v++)
        {
            for (int h = 0; h < horizontalSegments; h++)
            {
                int current = v * (horizontalSegments + 1) + h;
                int next = current + horizontalSegments + 1;

                // Two triangles per quad
                triangles[triIndex++] = current;
                triangles[triIndex++] = next;
                triangles[triIndex++] = current + 1;

                triangles[triIndex++] = current + 1;
                triangles[triIndex++] = next;
                triangles[triIndex++] = next + 1;
            }
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        return mesh;
    }

    private Vector3 ApplyDeformations(Vector3 vertex)
    {
        // Start with base sphere vertex
        Vector3 result = vertex;

        // Apply independent axis scaling
        result.x *= scale.x;
        result.y *= scale.y;
        result.z *= scale.z;

        // Apply taper (narrowing effect)
        if (taper > 0f)
        {
            float taperFactor = CalculateTaperFactor(vertex);
            
            if (taperDirection == TaperDirection.BottomToTop)
            {
                // Taper in XZ plane based on Y position
                result.x *= taperFactor;
                result.z *= taperFactor;
            }
            else // BackToFront
            {
                // Taper in YZ plane based on X position
                result.y *= taperFactor;
                result.z *= taperFactor;
            }
        }

        return result;
    }

    private Mesh GenerateCubeMesh()
    {
        Mesh mesh = new Mesh();

        // Subdivided cube for smooth deformations
        int subdivisionsPerFace = 8;
        int vertsPerEdge = subdivisionsPerFace + 1;

        // We'll generate 6 faces with subdivisions
        Vector3[] vertices = new Vector3[vertsPerEdge * vertsPerEdge * 6];
        int[] triangles = new int[subdivisionsPerFace * subdivisionsPerFace * 6 * 6]; // 6 faces, 2 tris per quad, 3 verts per tri

        int vertIndex = 0;
        int triIndex = 0;

        // Generate each face of the cube with subdivisions
        GenerateCubeFace(vertices, triangles, ref vertIndex, ref triIndex, Vector3.forward, Vector3.up, Vector3.right, subdivisionsPerFace);    // Front
        GenerateCubeFace(vertices, triangles, ref vertIndex, ref triIndex, Vector3.back, Vector3.up, Vector3.left, subdivisionsPerFace);      // Back
        GenerateCubeFace(vertices, triangles, ref vertIndex, ref triIndex, Vector3.right, Vector3.up, Vector3.back, subdivisionsPerFace);     // Right
        GenerateCubeFace(vertices, triangles, ref vertIndex, ref triIndex, Vector3.left, Vector3.up, Vector3.forward, subdivisionsPerFace);   // Left
        GenerateCubeFace(vertices, triangles, ref vertIndex, ref triIndex, Vector3.up, Vector3.back, Vector3.right, subdivisionsPerFace);     // Top
        GenerateCubeFace(vertices, triangles, ref vertIndex, ref triIndex, Vector3.down, Vector3.forward, Vector3.right, subdivisionsPerFace); // Bottom

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        return mesh;
    }

    private void GenerateCubeFace(Vector3[] vertices, int[] triangles, ref int vertIndex, ref int triIndex, 
                                   Vector3 normal, Vector3 up, Vector3 right, int subdivisions)
    {
        int startVertIndex = vertIndex;
        int vertsPerEdge = subdivisions + 1;

        // Generate grid of vertices for this face
        for (int y = 0; y <= subdivisions; y++)
        {
            for (int x = 0; x <= subdivisions; x++)
            {
                float u = (float)x / subdivisions - 0.5f; // -0.5 to 0.5
                float v = (float)y / subdivisions - 0.5f; // -0.5 to 0.5

                // Base cube vertex (unit cube, centered at origin)
                Vector3 baseVertex = normal * 0.5f + right * u + up * v;

                // Apply deformations
                Vector3 deformedVertex = ApplyDeformations(baseVertex);

                vertices[vertIndex++] = deformedVertex;
            }
        }

        // Generate triangles for this face
        for (int y = 0; y < subdivisions; y++)
        {
            for (int x = 0; x < subdivisions; x++)
            {
                int current = startVertIndex + y * vertsPerEdge + x;
                int next = current + vertsPerEdge;

                // Two triangles per quad
                triangles[triIndex++] = current;
                triangles[triIndex++] = next;
                triangles[triIndex++] = current + 1;

                triangles[triIndex++] = current + 1;
                triangles[triIndex++] = next;
                triangles[triIndex++] = next + 1;
            }
        }
    }

    private float CalculateTaperFactor(Vector3 vertex)
    {
        // Calculate how much to narrow based on position along taper axis
        float t; // 0 = wide end, 1 = narrow end

        if (taperDirection == TaperDirection.BottomToTop)
        {
            // Y goes from -1 to +1, map to 0 to 1
            t = (vertex.y + 1f) * 0.5f;
        }
        else // BackToFront
        {
            // X goes from -1 to +1, map to 0 to 1
            t = (vertex.x + 1f) * 0.5f;
        }

        // At t=0 (wide end): factor = 1 (no narrowing)
        // At t=1 (narrow end): factor = 1-taper (narrowed by taper amount)
        float narrowFactor = 1f - taper;
        return Mathf.Lerp(1f, narrowFactor, t);
    }
}