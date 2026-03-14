using UnityEngine;

public class ParallaxEffect : MonoBehaviour
{
    [Header("Parallax Settings")]
    [Tooltip("How much the background moves left/right (0 = still, 1 = follows perfectly)")]
    public float parallaxEffect;
    
    [Tooltip("How much the background moves up/down. Keep this very low! (e.g., 0.05 to 0.2)")]
    public float parallaxEffectY = 0.1f; // <-- NEW VARIABLE FOR VERTICAL

    private Transform cameraTransform;
    private Vector3 lastCameraPosition;
    
    // Variables for looping
    private float backgroundLength;
    private float startPosition;

    void Start()
    {
        cameraTransform = Camera.main.transform;
        lastCameraPosition = cameraTransform.position;
        startPosition = transform.position.x;

        // Works for SpriteRenderer, TilemapRenderer, or any Renderer on this object
        Renderer backgroundRenderer = GetComponent<Renderer>();
        if (backgroundRenderer == null)
        {
            Debug.LogError($"ParallaxEffect: No Renderer found on {gameObject.name}. Add a SpriteRenderer/TilemapRenderer or move this script to the object that has one.");
            enabled = false;
            return;
        }

        // Find the length of the background graphic to know when to loop it
        backgroundLength = backgroundRenderer.bounds.size.x;
    }

    void LateUpdate()
    {
        // 1. Calculate how much the camera has moved
        Vector3 deltaMovement = cameraTransform.position - lastCameraPosition;
        
        // 2. Move background with parallax effect (Notice we use parallaxEffectY for the Y axis now!)
        transform.position += new Vector3(deltaMovement.x * parallaxEffect, deltaMovement.y * parallaxEffectY, 0);

        // 3. Keep track of how far the camera has moved *relative* to the start point of this background layer
        float cameraRelativePosition = cameraTransform.position.x * (1 - parallaxEffect);

        // 4. If the camera has moved completely past this sprite (to the right)...
        if (cameraRelativePosition > startPosition + backgroundLength)
        {
            // Shift our start point a full length to the right
            startPosition += backgroundLength; 
        }
        // If the camera has moved completely past this sprite (to the left)...
        else if (cameraRelativePosition < startPosition - backgroundLength)
        {
             // Shift our start point a full length to the left
            startPosition -= backgroundLength;
        }

        // Apply the new horizontal position. (Y and Z stay as they are)
        transform.position = new Vector3(startPosition + (cameraTransform.position.x * parallaxEffect), transform.position.y, transform.position.z);

        lastCameraPosition = cameraTransform.position;
    }
}