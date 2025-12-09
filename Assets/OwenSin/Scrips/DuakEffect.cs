using UnityEngine;
using System.Collections.Generic;

public class DuakEffect : MonoBehaviour
{
    [Header("NPC names that Duak may target")]
    public List<string> possibleTargets = new List<string>();

    /// <summary>
    /// Call this when Duak is RE-REJECTED.
    /// Picks a random NPC from the list and marks them to need medicine tomorrow.
    /// </summary>
    public void ApplyDuakCurse()
    {
        if (possibleTargets.Count == 0)
        {
            Debug.LogWarning("[DuakEffect] No possible targets set.");
            return;
        }

        // pick random target
        string targetName = possibleTargets[Random.Range(0, possibleTargets.Count)];

        var state = NPCStateManager.Instance.GetOrCreateState(targetName);
        state.forceMedicineTomorrow = true;

        Debug.Log($"[DuakEffect] {targetName} has been cursed. They will need medicine tomorrow.");
    }
}
