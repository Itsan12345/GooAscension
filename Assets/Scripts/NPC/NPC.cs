using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NPC : MonoBehaviour, IInteractable
{
   public NPCDialogue dialogueData;
   public GameObject dialoguePanel;
   public TMP_Text dialogueText, nameText;
   public Image portraitImage;
   
   [Header("Interaction Settings")]
   public KeyCode interactionKey = KeyCode.F;
   public string playerTag = "Player";
   
   [Header("Player Exclamation Indicator")]
   public GameObject exclamationSprite;
   [Header("Form-Specific Offsets")]
   public float slimeYOffset = 1.5f;
   public float humanYOffset = 2.0f;
   
   [Header("Sound Effect Settings")]
   public SoundEffectLibrary soundEffectLibrary;
   public AudioSource soundEffectAudioSource;
   public string npcSoundGroupName = "NPC";
   public int npcSoundElementIndex = 0;
   public bool playVoiceWithTyping = true;

   [Header("Debug")]
   [Tooltip("Verbose interaction / dialogue logs. Off by default for shipping builds.")]
   [SerializeField] private bool debugLogging;

   private GameObject currentPlayer;

   private int dialogueIndex;
   private bool isTyping, isDialogueActive;
   private bool playerNearby = false;

   private void LogDbg(string message)
   {
       if (debugLogging)
           Debug.Log($"[NPC:{gameObject.name}] {message}");
   }

   void Start()
   {
       // Ensure dialogue panel is disabled at start
       if (dialoguePanel != null)
       {
           dialoguePanel.SetActive(false);
           LogDbg("Dialogue panel disabled at start");
       }
       else
       {
           Debug.LogError($"NPC {gameObject.name}: Dialogue panel is not assigned!");
       }
       
       // Hide exclamation sprite at start
       if (exclamationSprite != null)
       {
           exclamationSprite.SetActive(false);
       }
       
       // Validate dialogue data
       if (dialogueData == null)
       {
           Debug.LogError($"NPC {gameObject.name}: No dialogue data assigned!");
       }
       else
       {
           LogDbg($"Dialogue data loaded — {dialogueData.npcName}");
       }
   }

    public bool CanInteract()
    {
        return !isDialogueActive;
    }
    
    void Update()
    {
        // Check for interaction input when player is nearby
        if (playerNearby && Input.GetKeyDown(interactionKey))
        {
            LogDbg($"{interactionKey} pressed — Interact()");
            Interact();
        }

        // Update exclamation sprite position to follow player if active
        if (exclamationSprite != null && exclamationSprite.activeInHierarchy && currentPlayer != null)
        {
            Vector3 offset = GetExclamationOffset();
            exclamationSprite.transform.position = currentPlayer.transform.position + offset;
        }
    }

    public void Interact()
    {
        LogDbg("Interact()");

        // Check for null dialogue data
        if (dialogueData == null)
        {
            Debug.LogError($"NPC {gameObject.name}: Cannot interact - no dialogue data!");
            return;
        }
        
        // If game is paused and no dialogue is active
        if (PauseController.IsGamePaused && !isDialogueActive)
        {
            LogDbg("Cannot interact — game is paused");
            return;
        }

        if (isDialogueActive)
        {
            LogDbg("Advance dialogue line");
            NextLine();
        }
        else
        {
            LogDbg("Starting dialogue");
            StartDialogue();
        }
    }


    void StartDialogue()
    {
        LogDbg("StartDialogue()");
        EnsureDialogueUIHierarchyEnabled();
        EnsureDialoguePanelVisibleScale();
        
        // Play NPC interaction sound effect
        if (soundEffectLibrary != null)
        {
            if (soundEffectAudioSource != null)
            {
                soundEffectLibrary.PlaySoundEffect(soundEffectAudioSource, npcSoundGroupName, npcSoundElementIndex);
                LogDbg($"Playing sound effect '{npcSoundGroupName}'");
            }
            else
            {
                Debug.LogWarning($"NPC {gameObject.name}: soundEffectAudioSource is not assigned! Assign the SoundEffectController's AudioSource in the inspector.");
            }
        }
        else
        {
            Debug.LogWarning($"NPC {gameObject.name}: SoundEffectLibrary not assigned!");
        }
        
        // Hide exclamation sprite during dialogue
        ShowExclamationSprite(false);
        
        isDialogueActive = true;
        dialogueIndex = 0;
        
        if (nameText != null)
            nameText.SetText(dialogueData.npcName);
        else
            Debug.LogError($"NPC {gameObject.name}: nameText is null!");
            
        if (portraitImage != null)
            portraitImage.sprite = dialogueData.npcPortrait;
        else
            Debug.LogError($"NPC {gameObject.name}: portraitImage is null!");
            
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(true);
            LogDbg("Dialogue panel activated");
        }
        else
        {
            Debug.LogError($"NPC {gameObject.name}: dialoguePanel is null!");
        }
        
        PauseController.SetPause(true);
        StartCoroutine(TypeLine());
    }

    void NextLine()
    {
        if (dialogueData?.dialogueLines == null || dialogueText == null)
        {
            EndDialogue();
            return;
        }

        if (isTyping)
        {
            StopAllCoroutines();
            dialogueText.SetText(dialogueData.dialogueLines[dialogueIndex]);
            isTyping = false;
        }
        else if (++dialogueIndex < dialogueData.dialogueLines.Length)
        {
            StartCoroutine(TypeLine());
        }
        else
        {
            EndDialogue();
        }
    }

    IEnumerator TypeLine()
    {
        if (dialogueData?.dialogueLines == null || dialogueText == null)
            yield break;

        if (dialogueIndex < 0 || dialogueIndex >= dialogueData.dialogueLines.Length)
            yield break;

        isTyping = true;
        dialogueText.SetText("");

        string currentLine = dialogueData.dialogueLines[dialogueIndex];
        string displayText = "";

        foreach (char letter in currentLine)
        {
            displayText += letter;
            dialogueText.SetText(displayText);
            
            // Play voice sound effect for each letter to simulate talking
            if (playVoiceWithTyping && soundEffectLibrary != null && soundEffectAudioSource != null)
            {
                soundEffectLibrary.PlaySoundEffect(soundEffectAudioSource, npcSoundGroupName, npcSoundElementIndex);
            }
            
            yield return new WaitForSecondsRealtime(dialogueData.typingSpeed);
        }   

        isTyping = false;

        // AutoProgress
        if (dialogueData.autoProgressLines != null
            && dialogueIndex < dialogueData.autoProgressLines.Length
            && dialogueData.autoProgressLines[dialogueIndex])
        {
            yield return new WaitForSecondsRealtime(dialogueData.autoProgressDelay);
            // Display NextLine
            NextLine();
        }
    }

    public void EndDialogue()
    {
        StopAllCoroutines();
        isDialogueActive = false;
        
        if (dialogueText != null)
            dialogueText.SetText("");
            
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
            
        PauseController.SetPause(false);
        
        // --- NEW: START THE QUEST WHEN DIALOGUE ENDS ---
        if (KillQuestManager.Instance != null && !KillQuestManager.Instance.isQuestActive)
        {
            LogDbg("Starting Kill Quest!");
            KillQuestManager.Instance.StartQuest();
        }
        // -----------------------------------------------
        
        // Show exclamation again if player is still nearby
        if (playerNearby && CanInteract() && currentPlayer != null)
        {
            ShowExclamationSprite(true);
        }
    }

    private void EnsureDialoguePanelVisibleScale()
    {
        if (dialoguePanel == null) return;

        Transform current = dialoguePanel.transform;
        while (current != null)
        {
            Vector3 localScale = current.localScale;
            if (Mathf.Approximately(localScale.x, 0f) || Mathf.Approximately(localScale.y, 0f) || Mathf.Approximately(localScale.z, 0f))
            {
                current.localScale = Vector3.one;
                Debug.LogWarning($"NPC {gameObject.name}: Corrected zero scale on '{current.name}' so dialogue UI is visible.");
            }

            current = current.parent;
        }
    }

    private void EnsureDialogueUIHierarchyEnabled()
    {
        if (dialoguePanel == null) return;

        // Ensure the dialogue panel and all parents up to this NPC are active.
        Transform current = dialoguePanel.transform;
        while (current != null)
        {
            if (!current.gameObject.activeSelf)
            {
                current.gameObject.SetActive(true);
                Debug.LogWarning($"NPC {gameObject.name}: Activated inactive UI object '{current.name}' to display dialogue.");
            }

            if (current == transform)
            {
                break;
            }

            current = current.parent;
        }

        Canvas canvas = dialoguePanel.GetComponentInParent<Canvas>(true);
        if (canvas != null && !canvas.enabled)
        {
            canvas.enabled = true;
            Debug.LogWarning($"NPC {gameObject.name}: Re-enabled Canvas '{canvas.name}' for dialogue UI.");
        }

        CanvasScaler canvasScaler = dialoguePanel.GetComponentInParent<CanvasScaler>(true);
        if (canvasScaler != null && !canvasScaler.enabled)
        {
            canvasScaler.enabled = true;
            Debug.LogWarning($"NPC {gameObject.name}: Re-enabled CanvasScaler '{canvasScaler.name}' for dialogue UI.");
        }

        GraphicRaycaster graphicRaycaster = dialoguePanel.GetComponentInParent<GraphicRaycaster>(true);
        if (graphicRaycaster != null && !graphicRaycaster.enabled)
        {
            graphicRaycaster.enabled = true;
            Debug.LogWarning($"NPC {gameObject.name}: Re-enabled GraphicRaycaster '{graphicRaycaster.name}' for dialogue UI.");
        }
    }
    
    // =========================================================
    // Collision Detection for Player Interaction
    // =========================================================
    private void OnTriggerEnter2D(Collider2D other)
    {
        LogDbg($"OnTriggerEnter2D '{other.gameObject.name}' tag={other.tag}");

        if (other.CompareTag(playerTag))
        {
            playerNearby = true;

            // Find the parent GameObject with PlayerMovement component
            currentPlayer = FindPlayerWithMovement(other.gameObject);

            LogDbg($"Player in range — press {interactionKey} to talk to {dialogueData?.npcName ?? "NPC"}");
            
            // Show exclamation sprite above player if dialogue is not active
            if (CanInteract())
            {
                ShowExclamationSprite(true);
            }
        }
    }
    
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            playerNearby = false;
            currentPlayer = null;
            LogDbg("Player left interaction zone");
            
            // Hide exclamation sprite when player leaves
            ShowExclamationSprite(false);
        }
    }
    
    /// <summary>
    /// Shows or hides the exclamation sprite above the player
    /// </summary>
    /// <param name="show">Whether to show or hide the exclamation sprite</param>
    private void ShowExclamationSprite(bool show)
    {
        if (exclamationSprite != null)
        {
            exclamationSprite.SetActive(show);
            
            // Position the exclamation sprite above the player if showing
            if (show && currentPlayer != null)
            {
                Vector3 offset = GetExclamationOffset();
                exclamationSprite.transform.position = currentPlayer.transform.position + offset;
            }
        }
        else if (show)
        {
            Debug.LogWarning($"NPC {gameObject.name}: Exclamation sprite GameObject is not assigned!");
        }
    }
    
    /// <summary>
    /// Gets the appropriate exclamation offset based on the player's current form
    /// </summary>
    /// <returns>The offset vector for the exclamation sprite</returns>
    private Vector3 GetExclamationOffset()
    {
        if (currentPlayer != null)
        {
            PlayerMovement playerMovement = currentPlayer.GetComponent<PlayerMovement>();
            if (playerMovement != null)
            {
                bool isHuman = playerMovement.IsHuman;
                float yOffset = isHuman ? humanYOffset : slimeYOffset;
        
                return new Vector3(0f, yOffset, 0f);
            }
            else
            {
                Debug.LogWarning($"NPC {gameObject.name}: PlayerMovement component not found on {currentPlayer.name}!");
            }
        }
        else
        {
            Debug.LogWarning($"NPC {gameObject.name}: currentPlayer is null!");
        }
        return new Vector3(0f, slimeYOffset, 0f);
    }
    
    /// <summary>
    /// Finds the GameObject with PlayerMovement component, checking the object and its parents
    /// </summary>
    /// <param name="startObject">The GameObject to start searching from</param>
    /// <returns>The GameObject with PlayerMovement component, or null if not found</returns>
    private GameObject FindPlayerWithMovement(GameObject startObject)
    {
        GameObject current = startObject;
        
        // Check the current object and walk up the parent hierarchy
        while (current != null)
        {
            PlayerMovement playerMovement = current.GetComponent<PlayerMovement>();
            if (playerMovement != null)
            {
                LogDbg($"Found PlayerMovement on {current.name}");
                return current;
            }
            
            // Move to parent
            Transform parent = current.transform.parent;
            current = parent != null ? parent.gameObject : null;
        }
        
        Debug.LogWarning($"NPC {gameObject.name}: PlayerMovement component not found in hierarchy starting from {startObject.name}");
        return startObject; // Fallback to original object
    }
}
