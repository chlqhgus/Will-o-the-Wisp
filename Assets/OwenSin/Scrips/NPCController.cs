// NPCController.cs
using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class NPCController : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 3f;
    public float leaveSpeed = 12f;
    public float arriveThreshold = 0.05f;

    [Header("Bubble (world-space canvas on prefab)")]
    public GameObject bubbleRoot;   // the world-space Canvas root (child of prefab)
    public TMP_Text bubbleText;
    public Vector3 bubbleOffset = new Vector3(0, 1.6f, 0);

    // internal
    private Vector3 targetPos;
    private bool isLeaving = false;
    private bool hasArrived = false;

    // bound data (from prefab)
    [HideInInspector] public NPCData prefabData;
    [HideInInspector] public PersistentNPCState state; // persistent instance for this prefabID

    // dialogue control
    private List<string> currentSeq = null;
    private int sentenceIndex = 0;
    private Action onSequenceFinished = null;

    void Awake()
    {
        if (bubbleRoot != null) bubbleRoot.SetActive(false);
    }

    void Update()
    {
        // follow target
        float speed = isLeaving ? leaveSpeed : walkSpeed;
        transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);

        // position bubble above head (if using world-space, it's child—no need to update)
        // check arrival
        if (!isLeaving && !hasArrived && Vector3.Distance(transform.position, targetPos) <= arriveThreshold)
        {
            hasArrived = true;
            // QueueManager will start conversation once IsAtTarget() true
        }
    }

    public void SetTarget(Vector3 pos)
    {
        targetPos = pos;
        hasArrived = false;
    }

    public bool IsAtTarget()
    {
        return Vector3.Distance(transform.position, targetPos) <= arriveThreshold;
    }

    // ---- Dialogue sequence controls ----
    public void StartDialogueSequence(List<string> lines, Action onFinished = null)
    {
        if (lines == null || lines.Count == 0)
        {
            onFinished?.Invoke();
            return;
        }

        currentSeq = new List<string>(lines);
        sentenceIndex = 0;
        onSequenceFinished = onFinished;
        ShowSentence();
    }

    void ShowSentence()
    {
        if (currentSeq == null) return;
        if (bubbleRoot != null) bubbleRoot.SetActive(true);
        if (bubbleText != null) bubbleText.text = currentSeq[sentenceIndex];
    }

    // call from UI Next button
    public void NextSentence()
    {
        if (currentSeq == null) return;
        sentenceIndex++;
        if (sentenceIndex >= currentSeq.Count)
        {
            EndSequence();
        }
        else ShowSentence();
    }

    void EndSequence()
    {
        if (bubbleRoot != null) bubbleRoot.SetActive(false);
        var cb = onSequenceFinished;
        onSequenceFinished = null;
        currentSeq = null;
        cb?.Invoke();
    }

    // ---- Player actions ----
    public void OnGivenRice()
    {
        if (state == null) return;
        state.receivedFoodToday = true;
        state.daysWithoutFood = 0;
    }

    public void OnGivenMedicine()
    {
        if (state == null) return;
        state.receivedMedicineToday = true;
        state.isSick = false;
    }

    public void LeaveFast()
    {
        isLeaving = true;
        if (bubbleRoot != null) bubbleRoot.SetActive(false);
        targetPos = new Vector3(-14f, transform.position.y, transform.position.z);
        Destroy(gameObject, 3f);
    }
}
