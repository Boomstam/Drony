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

    [Header("Seeds")]
    [SerializeField] private int mainSeed = 42;
    [SerializeField] private int rotorSeed = 100;
    [SerializeField] private int hubSeed = 200;

    [Header("Layout Parameters")]
    [SerializeField] private int rotorCount = 4; // 4, 6, or 8 rotors
    [SerializeField] private Vector2 rotorDistanceRange = new Vector2(1.5f, 3.0f); // INCREASED from original to prevent touching
    [SerializeField] private Vector2 rotorVerticalOffsetRange = new Vector2(-0.3f, 0.3f);
    [SerializeField] private Vector2 rotorTiltAngleRange = new Vector2(-15f, 15f);

    [Header("Randomization Ranges")]
    [SerializeField] private Vector2 bladeLengthRange = new Vector2(0.5f, 2.5f);
    [SerializeField] private Vector2 bladeWidthRange = new Vector2(0.1f, 0.4f);
    [SerializeField] private Vector2 bladeThicknessRange = new Vector2(0.02f, 0.08f);
    [SerializeField] private Vector2 hubRadiusRange = new Vector2(0.08f, 0.3f);
    [SerializeField] private Vector2 hubHeightRange = new Vector2(0.05f, 0.2f);
    [SerializeField] private Vector2 ringThicknessRange = new Vector2(0.03f, 0.12f);
    [SerializeField] private Vector2 hubScaleRange = new Vector2(0.2f, 1.0f);
    [SerializeField, Range(0f, 1f)] private float ringProbability = 0.3f;

    // Current randomized layout values (for re-randomizing with same parts)
    private float currentRotorDistance;
    private float currentVerticalOffset;
    private float currentTiltAngle;

    public void RandomizeAll()
    {
        // Generate NEW seeds for everything (as if all buttons were pressed)
        mainSeed = Random.Range(0, 1000000);
        rotorSeed = Random.Range(0, 1000000);
        hubSeed = Random.Range(0, 1000000);
        
        // Create objects if they don't exist
        EnsureComponentsExist();
        
        // Use the new seeds to randomize everything
        Random.InitState(hubSeed);
        RandomizeHubInternal();
        
        Random.InitState(rotorSeed);
        RandomizeRotorInternal();
        
        Random.InitState(mainSeed);
        RandomizeLayoutInternal(mainSeed);
        
        #if UNITY_EDITOR
        EditorUtility.SetDirty(this);
        #endif
        
        Debug.Log($"Randomized all | mainSeed: {mainSeed} | rotorSeed: {rotorSeed} | hubSeed: {hubSeed}");
    }

    public void RandomizeLayout()
    {
        // Generate new main seed for layout
        mainSeed = Random.Range(0, 1000000);
        Random.InitState(mainSeed);
        RandomizeLayoutInternal(mainSeed);
        
        #if UNITY_EDITOR
        EditorUtility.SetDirty(this);
        #endif
    }

    private void EnsureComponentsExist()
    {
        // Delete old instances if they exist
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

        // Create new hub instance as child
        if (droneHubPrefab != null)
        {
            GameObject hubObj = new GameObject("GeneratedHub");
            hubObj.transform.SetParent(transform);
            hubObj.transform.localPosition = Vector3.zero;
            hubObj.transform.localRotation = Quaternion.identity;
            
            MeshFilter hubMF = hubObj.AddComponent<MeshFilter>();
            MeshRenderer hubMR = hubObj.AddComponent<MeshRenderer>();
            generatedHub = hubObj.AddComponent<ProceduralDroneHub>();
            
            // Copy material from prefab
            MeshRenderer prefabMR = droneHubPrefab.GetComponent<MeshRenderer>();
            if (prefabMR != null && prefabMR.sharedMaterial != null)
            {
                hubMR.sharedMaterial = prefabMR.sharedMaterial;
            }
            
            // Assign the meshFilter using the public field
            generatedHub.meshFilter = hubMF;
        }

        // Create multiple rotor instances based on rotorCount
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
                
                // Copy material from prefab
                MeshRenderer prefabMR = droneRotorPrefab.GetComponent<MeshRenderer>();
                if (prefabMR != null && prefabMR.sharedMaterial != null)
                {
                    rotorMR.sharedMaterial = prefabMR.sharedMaterial;
                }
                
                // Assign the meshFilter using the public field
                rotor.meshFilter = rotorMF;
                
                generatedRotors.Add(rotor);
            }
        }
    }

    private void RandomizeLayoutInternal(int seed)
    {
        Random.InitState(seed);
        
        // Randomize rotor count
        float rand = Random.value;
        if (rand < 0.33f) rotorCount = 4;
        else if (rand < 0.66f) rotorCount = 6;
        else rotorCount = 8;

        // Calculate safe minimum distance based on hub size and rotor blade length
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

        // Set minimum distance to ensure no overlap: hub radius + rotor length + safety margin
        float minSafeDistance = (hubMaxDimension * 0.5f) + rotorMaxLength + 0.5f;
        float actualMinDistance = Mathf.Max(rotorDistanceRange.x, minSafeDistance);
        
        // Randomize layout parameters with safe distances
        currentRotorDistance = Mathf.Lerp(actualMinDistance, rotorDistanceRange.y, Random.value);
        currentVerticalOffset = Mathf.Lerp(rotorVerticalOffsetRange.x, rotorVerticalOffsetRange.y, Random.value);
        currentTiltAngle = Mathf.Lerp(rotorTiltAngleRange.x, rotorTiltAngleRange.y, Random.value);

        // Position all rotors in a circle around the hub
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
        
        Debug.Log($"Randomized layout with seed: {seed} | Rotors: {rotorCount} | Distance: {currentRotorDistance:F2} | Safe minimum was: {minSafeDistance:F2}");
    }

    public void RandomizeRotor()
    {
        if (generatedRotors.Count == 0)
        {
            Debug.LogError("No rotors exist! Use Randomize All first.");
            return;
        }

        // Generate new rotor seed
        rotorSeed = Random.Range(0, 1000000);
        Random.InitState(rotorSeed);
        RandomizeRotorInternal();
        
        #if UNITY_EDITOR
        EditorUtility.SetDirty(this);
        #endif

        Debug.Log($"Randomized rotor with seed: {rotorSeed}");
    }

    private void RandomizeRotorInternal()
    {
        // Randomize number of blades
        int numBlades = Random.Range(2, 9); // 2-8 blades

        // Blade dimensions - more blades = thinner blades
        float bladeWidthModifier = 1f - ((numBlades - 2) / 6f) * 0.4f; // Up to 40% thinner with 8 blades
        float bladeLength = Mathf.Lerp(bladeLengthRange.x, bladeLengthRange.y, Random.value);
        float bladeWidth = Mathf.Lerp(bladeWidthRange.x, bladeWidthRange.y, Random.value) * bladeWidthModifier;
        float bladeThickness = Mathf.Lerp(bladeThicknessRange.x, bladeThicknessRange.y, Random.value);

        // Blade shape and deformations
        BladeShape bladeShape = (BladeShape)Random.Range(0, 3);
        float bladeCurveAmount = Random.Range(-1f, 1f);
        float petalShape = Mathf.Lerp(0f, 3f, Random.value);
        float bladeTwist = Random.Range(-1f, 1f);

        // Hub dimensions
        float hubRadius = Mathf.Lerp(hubRadiusRange.x, hubRadiusRange.y, Random.value);
        float hubHeight = Mathf.Lerp(hubHeightRange.x, hubHeightRange.y, Random.value);

        // Ring - probability based
        bool includeRing = Random.value < ringProbability;
        float ringThickness = includeRing ? Mathf.Lerp(ringThicknessRange.x, ringThicknessRange.y, Random.value) : 0.05f;

        // Apply to all rotors
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

        // Generate new hub seed
        hubSeed = Random.Range(0, 1000000);
        Random.InitState(hubSeed);
        RandomizeHubInternal();
        
        #if UNITY_EDITOR
        EditorUtility.SetDirty(this);
        #endif
        
        Debug.Log($"Randomized hub with seed: {hubSeed}");
    }

    private void RandomizeHubInternal()
    {
        // Base shape
        generatedHub.baseShape = (ProceduralDroneHub.BaseShape)Random.Range(0, 2); // Sphere or Cube

        // Scale - independent per axis for variety
        generatedHub.scale = new Vector3(
            Mathf.Lerp(hubScaleRange.x, hubScaleRange.y, Random.value),
            Mathf.Lerp(hubScaleRange.x, hubScaleRange.y, Random.value),
            Mathf.Lerp(hubScaleRange.x, hubScaleRange.y, Random.value)
        );

        // Taper deformation
        generatedHub.taper = Random.value; // 0 to 1
        generatedHub.taperDirection = (ProceduralDroneHub.TaperDirection)Random.Range(0, 2);

        generatedHub.GenerateHub();
    }

    public void DeleteCurrentDrone()
    {
        // Delete generated hub
        if (generatedHub != null)
        {
            DestroyImmediate(generatedHub.gameObject);
            generatedHub = null;
        }

        // Delete all generated rotors
        foreach (var rotor in generatedRotors)
        {
            if (rotor != null)
            {
                DestroyImmediate(rotor.gameObject);
            }
        }
        generatedRotors.Clear();

        Debug.Log("Deleted current drone");
    }
}