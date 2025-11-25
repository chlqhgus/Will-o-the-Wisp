using System.Collections;
using UnityEngine;
using TMPro;

public class NPC : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 3f;
    public float leaveSpeed = 12f;

    [Header("Bubble UI (assign in prefab)")]
    public GameObject bubbleBG;
    public TMP_Text bubbleText;

    [Header("Data")]
    public NPCData data; // assign NPCData asset per prefab (or via spawner)

    // internal
    private Vector3 targetPos;
    private bool isLeaving = false;
    private int queueIndex = -1;
    private bool hasArrived = false; // reached assigned slot
    private SpriteRenderer[] spriteRenderers;

    // Arrival tolerance (how close counts as "arrived")
    public float arriveThreshold = 0.05f;

    void Awake()
    {
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        HideBubble();
    }

    void Update()
    {
        // move
        float speed = isLeaving ? leaveSpeed : moveSpeed;
        transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);

        // arrival detection (only if not leaving)
        if (!isLeaving && queueIndex == 0)
        {
            float dist = Vector3.Distance(transform.position, targetPos);
            if (!hasArrived && dist <= arriveThreshold)
            {
                hasArrived = true;
                OnArrivedFront();
            }
        }
    }

    // set target world position (called by QueueManager)
    public void SetTarget(Vector3 newTarget)
    {
        targetPos = newTarget;
        hasArrived = false; // arrival must be re-evaluated
    }

    // called by QueueManager so NPC knows its index in queue
    public void SetQueueIndex(int index)
    {
        queueIndex = index;

        // if not front, ensure bubble hidden and reset arrived flag
        if (queueIndex != 0)
        {
            hasArrived = false;
            HideBubble();
        }
    }

    // Called when this NPC actually gets to the front slot
    private void OnArrivedFront()
    {
        ShowBubbleRandom(); // show an appropriate line
    }

    // Accept or refused -> leave quickly, hide bubble
    public void LeaveScene()
    {
        isLeaving = true;
        HideBubble();
        // drive off to left
        targetPos = new Vector3(-14f, transform.position.y, transform.position.z);
    }

    // Accept action (you might want different behavior - here same as LeaveScene)
    public void AcceptAndLeave()
    {
        isLeaving = true;
        HideBubble();
        targetPos = new Vector3(-14f, transform.position.y, transform.position.z);
    }

    // Sorting order setter called by manager
    public void SetSortingOrder(int order)
    {
        foreach (SpriteRenderer sr in spriteRenderers)
        {
            sr.sortingOrder = order;
        }
    }

    // -----------------------------------
    // Bubble helpers
    // -----------------------------------
    public void ShowBubble(string text)
    {
        if (bubbleBG != null) bubbleBG.SetActive(true);
        if (bubbleText != null)
        {
            bubbleText.gameObject.SetActive(true);
            bubbleText.text = text;
        }
    }

    public void ShowBubbleRandom()
    {
        if (data != null && data.speechLines != null && data.speechLines.Length > 0)
        {
            string line = data.speechLines[Random.Range(0, data.speechLines.Length)];
            ShowBubble(line);
        }
        else
        {
            // fallback test lines
            ShowBubble("...");
        }
    }

    public void HideBubble()
    {
        if (bubbleBG != null) bubbleBG.SetActive(false);
        if (bubbleText != null) bubbleText.gameObject.SetActive(false);
    }
}


