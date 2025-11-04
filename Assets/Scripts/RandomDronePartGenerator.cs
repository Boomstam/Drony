using UnityEngine;

public class RandomDronePartGenerator : MonoBehaviour
{
    [Header("Component References")]
    [SerializeField] private ProceduralDroneHub droneHub;
    [SerializeField] private ProceduralRotor droneRotor;

    [Header("Seeds")]
    [SerializeField] private int mainSeed = 42;
    [SerializeField] private int rotorSeed = 100;
    [SerializeField] private int hubSeed = 200;

    // Track previous values to detect manual changes
    private int previousMainSeed;
    private int previousRotorSeed;
    private int previousHubSeed;

    [Header("Meta Parameters")]
    [SerializeField] private int rotorCount = 4; // For future use: 4, 6, or 8 rotors
    [SerializeField] private bool autoGenerateMeshes = true; // Automatically call Generate methods after randomization

    [Header("Randomization Ranges")]
    [SerializeField] private Vector2 bladeLengthRange = new Vector2(0.5f, 2.5f);
    [SerializeField] private Vector2 bladeWidthRange = new Vector2(0.1f, 0.4f);
    [SerializeField] private Vector2 bladeThicknessRange = new Vector2(0.02f, 0.08f);
    [SerializeField] private Vector2 hubRadiusRange = new Vector2(0.08f, 0.3f);
    [SerializeField] private Vector2 hubHeightRange = new Vector2(0.05f, 0.2f);
    [SerializeField] private Vector2 ringThicknessRange = new Vector2(0.03f, 0.12f);
    [SerializeField] private Vector2 hubScaleRange = new Vector2(0.2f, 1.0f);
    [SerializeField, Range(0f, 1f)] private float ringProbability = 0.3f;

    public void RandomizeAll()
    {
        // Generate a new random seed
        mainSeed = Random.Range(0, 1000000);
        Random.InitState(mainSeed);
        
        // Randomize both components
        RandomizeRotorParameters();
        RandomizeHubParameters();
        
        // Generate meshes if auto-generate is enabled
        if (autoGenerateMeshes)
        {
            if (droneRotor != null) droneRotor.GenerateRotor();
            if (droneHub != null) droneHub.GenerateHub();
        }
        
        Debug.Log($"Randomized all with new main seed: {mainSeed}");
    }

    public void RandomizeRotor()
    {
        // Generate a new random seed for the rotor
        rotorSeed = Random.Range(0, 1000000);
        
        RandomizeRotorParameters();
        
        // Generate mesh if auto-generate is enabled
        if (autoGenerateMeshes && droneRotor != null)
        {
            droneRotor.GenerateRotor();
        }
    }

    private void RandomizeRotorParameters()
    {
        if (droneRotor == null)
        {
            Debug.LogError("ProceduralRotor reference is not assigned!");
            return;
        }

        Random.InitState(rotorSeed);

        // Basic blade parameters
        droneRotor.numberOfBlades = Random.Range(2, 9); // 2-8 inclusive
        
        // Blade dimensions with smart constraints
        float baseBladeLengthFactor = Random.value; // 0 to 1
        droneRotor.bladeLength = Mathf.Lerp(bladeLengthRange.x, bladeLengthRange.y, baseBladeLengthFactor);
        
        // More blades → thinner individual blades
        float bladeCountFactor = 1f - ((droneRotor.numberOfBlades - 2) / 6f) * 0.4f; // Reduces width by up to 40%
        float baseBladeWidth = Mathf.Lerp(bladeWidthRange.x, bladeWidthRange.y, Random.value);
        droneRotor.bladeWidth = baseBladeWidth * bladeCountFactor;
        
        droneRotor.bladeThickness = Mathf.Lerp(bladeThicknessRange.x, bladeThicknessRange.y, Random.value);

        // Blade shape
        droneRotor.bladeShape = (BladeShape)Random.Range(0, 3); // Triangular, Rectangular, Curved

        // Blade deformations
        droneRotor.bladeCurveAmount = Random.Range(-1f, 1f);
        droneRotor.petalShape = Random.Range(0f, 3f);
        droneRotor.bladeTwist = Random.Range(-1f, 1f);

        // Hub sizing - proportional to blade length but with variance
        float hubSizeFactor = baseBladeLengthFactor * 0.7f + Random.value * 0.3f; // Some correlation, some independence
        droneRotor.hubRadius = Mathf.Lerp(hubRadiusRange.x, hubRadiusRange.y, hubSizeFactor);
        droneRotor.hubHeight = Mathf.Lerp(hubHeightRange.x, hubHeightRange.y, Random.value);

        // Ring - independent probability
        droneRotor.includeRing = Random.value < ringProbability;
        droneRotor.ringThickness = Mathf.Lerp(ringThicknessRange.x, ringThicknessRange.y, Random.value);

        // Randomize rotor count for future use
        int[] validRotorCounts = { 4, 6, 8 };
        rotorCount = validRotorCounts[Random.Range(0, validRotorCounts.Length)];

        Debug.Log($"Randomized rotor with seed: {rotorSeed} | Blades: {droneRotor.numberOfBlades} | Shape: {droneRotor.bladeShape} | Ring: {droneRotor.includeRing}");
    }

    public void RandomizeHub()
    {
        // Generate a new random seed for the hub
        hubSeed = Random.Range(0, 1000000);
        
        RandomizeHubParameters();
        
        // Generate mesh if auto-generate is enabled
        if (autoGenerateMeshes && droneHub != null)
        {
            droneHub.GenerateHub();
        }
    }

    private void RandomizeHubParameters()
    {
        if (droneHub == null)
        {
            Debug.LogError("ProceduralDroneHub reference is not assigned!");
            return;
        }

        Random.InitState(hubSeed);

        // Base shape
        droneHub.baseShape = (ProceduralDroneHub.BaseShape)Random.Range(0, 2); // Sphere or Cube

        // Scale - independent per axis for variety
        droneHub.scale = new Vector3(
            Mathf.Lerp(hubScaleRange.x, hubScaleRange.y, Random.value),
            Mathf.Lerp(hubScaleRange.x, hubScaleRange.y, Random.value),
            Mathf.Lerp(hubScaleRange.x, hubScaleRange.y, Random.value)
        );

        // Taper deformation
        droneHub.taper = Random.value; // 0 to 1
        droneHub.taperDirection = (ProceduralDroneHub.TaperDirection)Random.Range(0, 2); // BottomToTop or BackToFront

        Debug.Log($"Randomized hub with seed: {hubSeed} | Shape: {droneHub.baseShape} | Taper: {droneHub.taper:F2} | Direction: {droneHub.taperDirection}");
    }

    // Helper method to ensure rotor is proportional to hub size (if both are assigned)
    private void EnsureProportions()
    {
        if (droneHub != null && droneRotor != null)
        {
            // Get average hub scale
            float avgHubScale = (droneHub.scale.x + droneHub.scale.y + droneHub.scale.z) / 3f;
            
            // If hub is large, allow longer blades
            float hubInfluence = Mathf.Clamp(avgHubScale, 0.5f, 1.5f);
            droneRotor.bladeLength *= hubInfluence;
            
            // Clamp to reasonable bounds
            droneRotor.bladeLength = Mathf.Clamp(droneRotor.bladeLength, bladeLengthRange.x, bladeLengthRange.y * 1.2f);
        }
    }

    // Called by editor when inspector values change
    public void OnValidate()
    {
        // Check if main seed was manually changed
        if (mainSeed != previousMainSeed)
        {
            previousMainSeed = mainSeed;
            ApplyMainSeed();
        }

        // Check if rotor seed was manually changed
        if (rotorSeed != previousRotorSeed)
        {
            previousRotorSeed = rotorSeed;
            ApplyRotorSeed();
        }

        // Check if hub seed was manually changed
        if (hubSeed != previousHubSeed)
        {
            previousHubSeed = hubSeed;
            ApplyHubSeed();
        }
    }

    private void ApplyMainSeed()
    {
        Random.InitState(mainSeed);
        
        if (droneRotor != null)
        {
            RandomizeRotorParameters();
            if (autoGenerateMeshes) droneRotor.GenerateRotor();
        }
        
        if (droneHub != null)
        {
            RandomizeHubParameters();
            if (autoGenerateMeshes) droneHub.GenerateHub();
        }
        
        Debug.Log($"Applied main seed: {mainSeed}");
    }

    private void ApplyRotorSeed()
    {
        if (droneRotor != null)
        {
            RandomizeRotorParameters();
            if (autoGenerateMeshes) droneRotor.GenerateRotor();
        }
    }

    private void ApplyHubSeed()
    {
        if (droneHub != null)
        {
            RandomizeHubParameters();
            if (autoGenerateMeshes) droneHub.GenerateHub();
        }
    }
}