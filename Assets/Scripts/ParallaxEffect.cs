using UnityEngine;

public class ParallaxEffect : MonoBehaviour
{
    [Tooltip("How much the background moves relative to the camera (0 = still, 1 = follows perfectly)")]
    public float parallaxEffect;

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
        
        // 2. Move background with parallax effect
        transform.position += new Vector3(deltaMovement.x * parallaxEffect, deltaMovement.y * parallaxEffect, 0);

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

        // Apply the new position. It needs its original startPosition PLUS the movement offset
        transform.position = new Vector3(startPosition + (cameraTransform.position.x * parallaxEffect), transform.position.y, transform.position.z);

        lastCameraPosition = cameraTransform.position;
    }
}