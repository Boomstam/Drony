using UnityEngine;
using System.Collections.Generic;

public class ProceduralDroneTextureGenerator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RandomDronePartGenerator droneGenerator;
    
    [Header("Texture Settings")]
    [SerializeField] private int textureResolution = 512;
    [SerializeField] private bool autoGenerateOnDroneChange = true;
    
    [Header("Color Scheme")]
    [SerializeField] private ColorScheme colorScheme = ColorScheme.Random;
    [SerializeField] private Color primaryColor = new Color(0.2f, 0.2f, 0.25f); // Dark blue-grey
    [SerializeField] private Color secondaryColor = new Color(0.15f, 0.15f, 0.15f); // Darker grey
    [SerializeField] private Color accentColor = new Color(0.3f, 0.6f, 1f); // Cyan glow
    
    [Header("Surface Details")]
    [SerializeField, Range(0f, 1f)] private float panelDensity = 0.3f;
    [SerializeField, Range(0f, 1f)] private float wearAmount = 0.4f;
    [SerializeField, Range(0f, 1f)] private float emissionIntensity = 0.5f;
    [SerializeField, Range(0f, 1f)] private float metallicAmount = 0.7f;
    [SerializeField, Range(0f, 1f)] private float smoothnessAmount = 0.6f;
    
    private int currentSeed;
    private Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();
    
    public enum ColorScheme
    {
        Random,
        Military,
        Civilian,
        SciFi,
        Custom
    }
    
    private void OnEnable()
    {
        if (droneGenerator != null && autoGenerateOnDroneChange)
        {
            // Subscribe to drone generation events if available
            // For now, we'll manually trigger
        }
    }
    
    public void GenerateTextures()
    {
        GenerateTextures(Random.Range(0, 100000));
    }
    
    public void GenerateTextures(int seed)
    {
        currentSeed = seed;
        Random.InitState(seed);
        
        // Select random color scheme if set to Random
        if (colorScheme == ColorScheme.Random)
        {
            SetRandomColorScheme();
        }
        
        // Find all renderers in the drone hierarchy
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null) continue;
            
            // Store original materials if not already stored
            if (!originalMaterials.ContainsKey(renderer))
            {
                originalMaterials[renderer] = renderer.sharedMaterials;
            }
            
            // Create new material instance for this renderer
            Material newMaterial = CreateProceduralMaterial(renderer.gameObject.name);
            
            // Apply to all material slots
            Material[] materials = new Material[renderer.sharedMaterials.Length];
            for (int i = 0; i < materials.Length; i++)
            {
                materials[i] = newMaterial;
            }
            renderer.sharedMaterials = materials;
        }
        
        Debug.Log($"Generated textures with seed: {seed}");
    }
    
    private void SetRandomColorScheme()
    {
        ColorScheme[] schemes = { ColorScheme.Military, ColorScheme.Civilian, ColorScheme.SciFi };
        ColorScheme selectedScheme = schemes[Random.Range(0, schemes.Length)];
        
        switch (selectedScheme)
        {
            case ColorScheme.Military:
                primaryColor = new Color(0.25f, 0.3f, 0.2f); // Olive
                secondaryColor = new Color(0.15f, 0.18f, 0.13f); // Dark olive
                accentColor = new Color(1f, 0.3f, 0.2f); // Red
                break;
                
            case ColorScheme.Civilian:
                primaryColor = new Color(0.9f, 0.9f, 0.95f); // Light grey/white
                secondaryColor = new Color(0.2f, 0.2f, 0.25f); // Dark accent
                accentColor = new Color(0.2f, 0.8f, 0.3f); // Green
                break;
                
            case ColorScheme.SciFi:
                primaryColor = new Color(0.15f, 0.15f, 0.2f); // Dark blue-grey
                secondaryColor = new Color(0.1f, 0.1f, 0.12f); // Almost black
                accentColor = new Color(0.3f, 0.6f, 1f); // Cyan
                break;
        }
    }
    
    private Material CreateProceduralMaterial(string objectName)
    {
        // Create new URP Lit material
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.name = $"Procedural_Drone_Material_{objectName}";
        
        // Generate textures
        Texture2D albedoMap = GenerateAlbedoMap();
        Texture2D metallicMap = GenerateMetallicSmoothnessMap();
        Texture2D normalMap = GenerateNormalMap();
        Texture2D emissionMap = GenerateEmissionMap();
        
        // Apply textures to material
        mat.SetTexture("_BaseMap", albedoMap);
        mat.SetTexture("_MetallicGlossMap", metallicMap);
        mat.SetTexture("_BumpMap", normalMap);
        mat.SetTexture("_EmissionMap", emissionMap);
        
        // Set material properties
        mat.SetFloat("_Metallic", metallicAmount);
        mat.SetFloat("_Smoothness", smoothnessAmount);
        mat.SetColor("_EmissionColor", accentColor * emissionIntensity);
        
        // Set to render both sides (fixes see-through issue)
        mat.SetInt("_Cull", 0); // 0 = Off (both sides), 1 = Front, 2 = Back
        
        // Enable emission
        mat.EnableKeyword("_EMISSION");
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        
        return mat;
    }
    
    private Texture2D GenerateAlbedoMap()
    {
        Texture2D texture = new Texture2D(textureResolution, textureResolution, TextureFormat.RGBA32, true);
        
        for (int y = 0; y < textureResolution; y++)
        {
            for (int x = 0; x < textureResolution; x++)
            {
                Color pixelColor = primaryColor;
                
                // Add panel variations
                if (Random.value < panelDensity * 0.01f)
                {
                    // Panel lines (horizontal and vertical)
                    if (x % 32 < 2 || y % 32 < 2)
                    {
                        pixelColor = Color.Lerp(pixelColor, secondaryColor, 0.8f);
                    }
                }
                
                // Random panel sections
                float panelNoise = Mathf.PerlinNoise(x * 0.05f, y * 0.05f);
                if (panelNoise > 0.6f)
                {
                    pixelColor = Color.Lerp(pixelColor, secondaryColor, 0.5f);
                }
                
                // Add wear/scratches
                float wearNoise = Mathf.PerlinNoise(x * 0.2f + 100f, y * 0.2f + 100f);
                if (wearNoise > (1f - wearAmount * 0.5f))
                {
                    pixelColor = Color.Lerp(pixelColor, primaryColor * 1.3f, wearNoise * 0.3f);
                }
                
                // Add subtle color variation
                float colorVariation = Mathf.PerlinNoise(x * 0.1f, y * 0.1f) * 0.05f;
                pixelColor *= (1f + colorVariation);
                
                texture.SetPixel(x, y, pixelColor);
            }
        }
        
        texture.Apply();
        return texture;
    }
    
    private Texture2D GenerateMetallicSmoothnessMap()
    {
        Texture2D texture = new Texture2D(textureResolution, textureResolution, TextureFormat.RGBA32, true);
        
        for (int y = 0; y < textureResolution; y++)
        {
            for (int x = 0; x < textureResolution; x++)
            {
                // Metallic in R channel, Smoothness in A channel (URP standard)
                float metallic = metallicAmount;
                float smoothness = smoothnessAmount;
                
                // Panel variations affect smoothness
                float panelNoise = Mathf.PerlinNoise(x * 0.05f, y * 0.05f);
                if (panelNoise > 0.6f)
                {
                    smoothness *= 0.7f; // Panels are less smooth
                }
                
                // Wear reduces both metallic and smoothness
                float wearNoise = Mathf.PerlinNoise(x * 0.2f + 100f, y * 0.2f + 100f);
                if (wearNoise > (1f - wearAmount * 0.5f))
                {
                    metallic *= 0.8f;
                    smoothness *= 0.6f;
                }
                
                // Add variation
                float variation = Mathf.PerlinNoise(x * 0.1f + 200f, y * 0.1f + 200f);
                smoothness *= (0.9f + variation * 0.2f);
                
                Color pixel = new Color(metallic, 0f, 0f, smoothness);
                texture.SetPixel(x, y, pixel);
            }
        }
        
        texture.Apply();
        return texture;
    }
    
    private Texture2D GenerateNormalMap()
    {
        Texture2D texture = new Texture2D(textureResolution, textureResolution, TextureFormat.RGBA32, true);
        
        for (int y = 0; y < textureResolution; y++)
        {
            for (int x = 0; x < textureResolution; x++)
            {
                // Start with flat normal (0.5, 0.5, 1.0 in RGB = 0, 0, 1 in normal space)
                Vector3 normal = new Vector3(0.5f, 0.5f, 1f);
                
                // Add panel edge details
                if (x % 32 < 2 || y % 32 < 2)
                {
                    float edgeDepth = 0.02f;
                    normal.z -= edgeDepth;
                }
                
                // Add surface noise
                float noiseX = Mathf.PerlinNoise(x * 0.3f, y * 0.3f) - 0.5f;
                float noiseY = Mathf.PerlinNoise(x * 0.3f + 300f, y * 0.3f + 300f) - 0.5f;
                
                normal.x += noiseX * 0.1f;
                normal.y += noiseY * 0.1f;
                
                // Normalize and convert to color
                normal = normal.normalized;
                Color pixel = new Color(normal.x, normal.y, normal.z, 1f);
                
                texture.SetPixel(x, y, pixel);
            }
        }
        
        texture.Apply();
        return texture;
    }
    
    private Texture2D GenerateEmissionMap()
    {
        Texture2D texture = new Texture2D(textureResolution, textureResolution, TextureFormat.RGBA32, true);
        
        // Number of emission strips/lights
        int numEmissionFeatures = Random.Range(2, 6);
        
        for (int y = 0; y < textureResolution; y++)
        {
            for (int x = 0; x < textureResolution; x++)
            {
                Color emissionColor = Color.black;
                
                // Horizontal emission strips
                for (int i = 0; i < numEmissionFeatures; i++)
                {
                    int stripY = (textureResolution / (numEmissionFeatures + 1)) * (i + 1);
                    float distance = Mathf.Abs(y - stripY);
                    
                    if (distance < 3)
                    {
                        float intensity = 1f - (distance / 3f);
                        emissionColor = Color.Lerp(emissionColor, accentColor, intensity * emissionIntensity);
                    }
                }
                
                // Add some random glow spots (like indicator lights)
                float spotNoise = Mathf.PerlinNoise(x * 0.1f + 500f, y * 0.1f + 500f);
                if (spotNoise > 0.95f)
                {
                    emissionColor = Color.Lerp(emissionColor, accentColor, emissionIntensity * 0.5f);
                }
                
                texture.SetPixel(x, y, emissionColor);
            }
        }
        
        texture.Apply();
        return texture;
    }
    
    public void RestoreOriginalMaterials()
    {
        foreach (var kvp in originalMaterials)
        {
            if (kvp.Key != null)
            {
                kvp.Key.sharedMaterials = kvp.Value;
            }
        }
        originalMaterials.Clear();
        Debug.Log("Restored original materials");
    }
    
    public void SetSeed(int seed)
    {
        currentSeed = seed;
    }
    
    public int GetCurrentSeed()
    {
        return currentSeed;
    }
}