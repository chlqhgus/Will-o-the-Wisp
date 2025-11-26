// QueueManager.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class QueueManager : MonoBehaviour
{
    [Header("Scene")]
    public Transform spawnPoint;           // off-screen spawn pos
    public Transform[] queueSlots;         // 4 positions (0 = front)

    [Header("UI Buttons")]
    public Button nextSentenceButton;
    public Button giveRiceButton;
    public Button giveMedButton;
    public Button refuseButton;

    [Header("References")]
    public Inventory inventory;            // assign in inspector

    // runtime
    private List<NPCController> active = new List<NPCController>();
    private int spawnPointer = 0;

    // conversation state
    private NPCController frontCtrl;
    private PersistentNPCState frontState;
    private NPCData frontData;
    private bool inConversation = false;
    private bool refusedOnce = false;

    void Start()
    {
        nextSentenceButton.onClick.AddListener(OnNextSentence);
        giveRiceButton.onClick.AddListener(OnGiveRice);
        giveMedButton.onClick.AddListener(OnGiveMedicine);
        refuseButton.onClick.AddListener(OnRefuse);

        // fill queue initially
        for (int i = 0; i < queueSlots.Length; i++) TrySpawnNext();
    }

    void Update()
    {
        UpdateTargetsAndSorting();

        if (!inConversation && active.Count > 0)
        {
            var c = active[0];
            if (c.IsAtTarget())
            {
                StartConversation(c);
            }
        }
    }

    void UpdateTargetsAndSorting()
    {
        for (int i = 0; i < active.Count; i++)
        {
            if (i < queueSlots.Length) active[i].SetTarget(queueSlots[i].position);
            else active[i].SetTarget(queueSlots[queueSlots.Length - 1].position + new Vector3(3f * (i - (queueSlots.Length - 1)), 0, 0));

            // sorting
            int order = 200 - i;
            var srs = active[i].GetComponentsInChildren<SpriteRenderer>();
            foreach (var sr in srs) sr.sortingOrder = order;
        }
    }

    void StartConversation(NPCController ctrl)
    {
        inConversation = true;
        frontCtrl = ctrl;
        frontState = ctrl.state;
        frontData = ctrl.prefabData;
        refusedOnce = false;

        // pick day1 or returning
        List<string> seq = (!frontState.metBefore) ? frontData.arrivalDay1 : frontData.arrivalReturning;
        frontState.metBefore = true;

        frontCtrl.StartDialogueSequence(seq, OnArrivalFinished);
        nextSentenceButton.gameObject.SetActive(true);
        SetActionButtons(false);
    }

    void OnArrivalFinished()
    {
        nextSentenceButton.gameObject.SetActive(false);
        SetActionButtons(true);
    }

    void OnNextSentence()
    {
        if (frontCtrl != null) frontCtrl.NextSentence();
    }

    void OnGiveRice()
    {
        if (frontCtrl == null || frontState == null) return;
        if (!inventory.UseLotusRice()) return;

        frontCtrl.OnGivenRice();

        var seq = (!frontState.metBefore) ? frontData.acceptRiceDay1 : frontData.acceptRiceReturning;
        StartAcceptRejectFlow(seq);
    }

    void OnGiveMedicine()
    {
        if (frontCtrl == null || frontState == null) return;
        if (!inventory.UseHerbalMedicine()) return;

        frontCtrl.OnGivenMedicine();

        var seq = (!frontState.metBefore) ? frontData.acceptMedDay1 : frontData.acceptMedReturning;
        StartAcceptRejectFlow(seq);
    }

    void OnRefuse()
    {
        if (frontCtrl == null || frontState == null) return;

        frontState.receivedFoodToday = false;
        frontState.daysWithoutFood += 1;

        if (!refusedOnce)
        {
            refusedOnce = true;
            var seq = (!frontState.metBefore) ? frontData.redemandDay1 : frontData.redemandReturning;
            frontCtrl.StartDialogueSequence(seq, OnArrivalFinished);
            nextSentenceButton.gameObject.SetActive(true);
            SetActionButtons(false);
            return;
        }
        else
        {
            var seq = (!frontState.metBefore) ? frontData.rejectRiceDay1 : frontData.rejectRiceReturning;
            StartAcceptRejectFlow(seq, forceLeave: true);
        }
    }

    void StartAcceptRejectFlow(List<string> seq, bool forceLeave = false)
    {
        SetActionButtons(false);
        nextSentenceButton.gameObject.SetActive(true);

        frontCtrl.StartDialogueSequence(seq, () =>
        {
            // remove front
            active.RemoveAt(0);

            // make it leave visually
            frontCtrl.LeaveFast();

            // clear state
            frontCtrl = null;
            frontState = null;
            frontData = null;
            inConversation = false;
            nextSentenceButton.gameObject.SetActive(false);

            // spawn to refill
            TrySpawnToFill();
        });
    }

    void TrySpawnNext()
    {
        var gm = GameManager.I;
        if (gm == null) return;
        if (spawnPointer >= gm.spawnOrderToday.Count) return;

        string prefabID = gm.spawnOrderToday[spawnPointer];
        spawnPointer++;
        if (string.IsNullOrEmpty(prefabID)) return;

        // find prefab with that prefabID
        GameObject prefab = gm.npcPrefabs.Find(p =>
        {
            var holder = p.GetComponent<NPCDataHolder>();
            return holder != null && holder.data != null && holder.data.prefabID == prefabID;
        });

        if (prefab == null) return;

        // instantiate
        GameObject go = Instantiate(prefab, spawnPoint.position, Quaternion.identity);
        var ctrl = go.GetComponent<NPCController>();
        var holderData = prefab.GetComponent<NPCDataHolder>().data;
        ctrl.prefabData = holderData;
        ctrl.state = GameManager.I.GetOrCreateState(holderData.prefabID);

        // if state.isDead was true, destroy immediately and continue
        if (ctrl.state.isDead)
        {
            Destroy(go);
            TrySpawnNext();
            return;
        }

        active.Add(ctrl);
    }

    void TrySpawnToFill()
    {
        while (active.Count < queueSlots.Length)
        {
            TrySpawnNext();
            if (spawnPointer >= GameManager.I.spawnOrderToday.Count) break;
        }
    }

    void SetActionButtons(bool on)
    {
        giveRiceButton.interactable = on;
        giveMedButton.interactable = on;
        refuseButton.interactable = on;
    }
}
