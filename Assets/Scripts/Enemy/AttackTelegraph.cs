using UnityEngine;
using System.Collections;

public class AttackTelegraph : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private Sprite telegraphSprite;
    private Color originalColor;
    private Coroutine telegraphCoroutine;

    [Header("Telegraph Settings")]
    [SerializeField] private float telegraphDuration = 0.5f;
    [SerializeField] private Color telegraphColor = new Color(1f, 0.3f, 0.3f, 0.7f); // Red tint
    [SerializeField] private float telegraphSize = 1f; // Size of the telegraph indicator

    private void Awake()
    {
        // Create a sprite renderer if one doesn't exist
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        // Create a simple white circle sprite if none exists
        if (spriteRenderer.sprite == null)
        {
            telegraphSprite = CreateDefaultSprite();
            spriteRenderer.sprite = telegraphSprite;
        }
        else
        {
            telegraphSprite = spriteRenderer.sprite;
        }

        // Store original color
        originalColor = spriteRenderer.color;
        
        // Start invisible
        SetVisibility(false);
    }

    /// <summary>
    /// Creates a simple default white circle sprite
    /// </summary>
    private Sprite CreateDefaultSprite()
    {
        // Create a simple white circle texture
        int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "TelegraphCircle";

        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float radius = size * 0.4f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                if (distance <= radius)
                {
                    pixels[y * size + x] = Color.white;
                }
                else
                {
                    pixels[y * size + x] = Color.clear;
                }
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        // Create sprite from texture
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100);
        sprite.name = "TelegraphCircle";

        return sprite;
    }

    /// <summary>
    /// Shows the telegraph warning before an attack
    /// </summary>
    public void ShowTelegraph()
    {
        // Stop any existing telegraph
        if (telegraphCoroutine != null)
        {
            StopCoroutine(telegraphCoroutine);
        }

        telegraphCoroutine = StartCoroutine(TelegraphPulseRoutine());
    }

    private IEnumerator TelegraphPulseRoutine()
    {
        SetVisibility(true);
        SetColor(telegraphColor);

        // Pulse effect - scale up and glow, then fade out
        float elapsedTime = 0f;
        Vector3 startScale = transform.localScale;
        Vector3 targetScale = startScale * (1f + telegraphSize * 0.2f); // Scale based on size parameter

        // Scale up while fading in slightly
        while (elapsedTime < telegraphDuration * 0.5f)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / (telegraphDuration * 0.5f);
            
            transform.localScale = Vector3.Lerp(startScale, targetScale, progress);
            
            Color color = telegraphColor;
            color.a = Mathf.Lerp(telegraphColor.a, telegraphColor.a * 0.8f, progress);
            SetColor(color);

            yield return null;
        }

        // Fade out and shrink back
        elapsedTime = 0f;
        while (elapsedTime < telegraphDuration * 0.5f)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / (telegraphDuration * 0.5f);
            
            transform.localScale = Vector3.Lerp(targetScale, startScale, progress);
            
            Color color = telegraphColor;
            color.a = Mathf.Lerp(telegraphColor.a * 0.8f, 0f, progress);
            SetColor(color);

            yield return null;
        }

        // Ensure fully invisible
        SetVisibility(false);
        transform.localScale = startScale;
        
        // Restore original appearance
        SetColor(originalColor);
    }

    private void SetVisibility(bool visible)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = visible;
        }
    }

    private void SetColor(Color color)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = color;
        }
    }

    public float GetTelegraphDuration()
    {
        return telegraphDuration;
    }
}
