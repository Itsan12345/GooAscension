using System.Collections;
using UnityEngine;
using TMPro;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;
    public GameObject dialoguePanel;
    public TMP_Text dialogueText;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public IEnumerator ShowDialogue(string text, float duration = 2.5f)
    {
        dialoguePanel.SetActive(true);
        dialogueText.text = text;
        yield return new WaitForSeconds(duration);
        dialoguePanel.SetActive(false);
    }

    // Optional: Show dialogue and wait for player input to continue
    public IEnumerator ShowDialogueWaitForInput(string text)
    {
        dialoguePanel.SetActive(true);
        dialogueText.text = text;
        bool proceed = false;
        while (!proceed)
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
                proceed = true;
            yield return null;
        }
        dialoguePanel.SetActive(false);
    }
}
