using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public enum QuestType { KillMobs, FindLever }

public class KillQuestManager : MonoBehaviour
{
    public static KillQuestManager Instance { get; private set; }

    [Header("Quest Settings")]
    [Tooltip("Choose what kind of quest this level has.")]
    public QuestType currentQuest = QuestType.KillMobs;
    
    public bool isQuestActive = false;
    private bool isQuestComplete = false;

    [Header("Kill Quest Targets (Level 1)")]
    public int targetSlimes = 1;
    public int targetSentinels = 2;

    private int slimesKilled = 0;
    private int sentinelsKilled = 0;

    [Header("UI References (Leave Empty in Level 2!)")]
    public TMP_Text questText;
    public GameObject questUIPanel;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // --- NEW: Automatically reconnect to the Level 1 Canvas! ---
        if (questUIPanel == null || questText == null)
        {
            ReconnectToPersistentUI();
        }
    }

    private void ReconnectToPersistentUI()
    {
        // This trick finds the UI from Level 1 even if it is currently SetActive(false)!
        TMP_Text[] allTexts = Resources.FindObjectsOfTypeAll<TMP_Text>();
        foreach (TMP_Text t in allTexts)
        {
            // Look for the exact name of your text object from your hierarchy ("KillCount")
            if (t.gameObject.scene.name != null && t.gameObject.name == "KillCount")
            {
                questText = t;
                questUIPanel = t.transform.parent.gameObject; // The panel is the parent!
                Debug.Log("QuestManager successfully hijacked the Level 1 UI!");
                break;
            }
        }
    }

    private void Start()
    {
        // If this is the Lever Quest (Level 2), start it automatically!
        if (currentQuest == QuestType.FindLever)
        {
            isQuestActive = true;
            if (questUIPanel != null) questUIPanel.SetActive(true);
            UpdateQuestUI();
        }
        else // Otherwise (Level 1), hide it and wait for the NPC
        {
            if (questUIPanel != null) questUIPanel.SetActive(false);
            if (questText != null) questText.text = "";
        }
    }

    // Called by your NPC
    public void StartQuest()
    {
        if (currentQuest == QuestType.KillMobs)
        {
            isQuestActive = true;
            isQuestComplete = false;
            slimesKilled = 0;
            sentinelsKilled = 0;

            if (questUIPanel != null) questUIPanel.SetActive(true);
            UpdateQuestUI();
        }
    }

    // Called by EnemyHealth script
    public void OnEnemyKilled(string enemyType)
    {
        if (!isQuestActive || isQuestComplete || currentQuest != QuestType.KillMobs) return;

        if (enemyType == "Slime" && slimesKilled < targetSlimes)
        {
            slimesKilled++;
        }
        else if (enemyType == "Sentinel" && sentinelsKilled < targetSentinels)
        {
            sentinelsKilled++;
        }

        CheckQuestCompletion();
        UpdateQuestUI();
    }

    private void CheckQuestCompletion()
    {
        if (slimesKilled >= targetSlimes && sentinelsKilled >= targetSentinels)
        {
            isQuestComplete = true;
            Debug.Log("Quest Complete! Player can now proceed to the exit.");
        }
    }

    // Call this from your Lever script when the player pulls it!
    public void CompleteLeverQuest()
    {
        if (currentQuest == QuestType.FindLever && !isQuestComplete)
        {
            isQuestComplete = true;
            
            if (questText != null)
                questText.text = "<b>— QUEST —</b>\nHidden room unlocked! Enter the teleporter.";
            
            Debug.Log("Lever pulled! Teleporter room is open.");
        }
    }

    private void UpdateQuestUI()
    {
        if (questText == null) return;

        // --- LEVEL 2 QUEST TEXT ---
        if (currentQuest == QuestType.FindLever)
        {
            questText.text = "<b>— QUEST —</b>\nFind the lever at the bottom to unlock the hidden room.";
        }
        // --- LEVEL 1 QUEST TEXT ---
        else if (currentQuest == QuestType.KillMobs)
        {
            if (isQuestComplete)
                questText.text = "<b>— QUEST —</b>\nProceed to next level";
            else
                questText.text = $"<b>— QUEST —</b>\n" +
                                 $"Kill Slime: {slimesKilled} / {targetSlimes}\n" +
                                 $"Kill Sentinel: {sentinelsKilled} / {targetSentinels}";
        }
    }
}