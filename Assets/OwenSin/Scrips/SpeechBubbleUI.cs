using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SpeechBubbleUI : MonoBehaviour
{
    public static SpeechBubbleUI Instance;

    public GameObject bubbleRoot;       // The UI object that contains bubble + text
    public TextMeshProUGUI bubbleText;

    void Awake()
    {
        Instance = this;
        bubbleRoot.SetActive(false);    // hide by default
    }

    public void ShowBubble(string text)
    {
        bubbleText.text = text;
        bubbleRoot.SetActive(true);
    }

    public void HideBubble()
    {
        bubbleRoot.SetActive(false);
    }
}
