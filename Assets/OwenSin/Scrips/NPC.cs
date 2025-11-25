using UnityEngine;

public class NPC : MonoBehaviour
{
    public float moveSpeed = 3f;
    public float leaveSpeed = 12f;   // fast speed when leaving

    private Vector3 targetPos;
    private bool isLeaving = false;

    private SpriteRenderer[] spriteRenderers;

    void Awake()
    {
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
    }

    void Update()
    {
        if (!isLeaving)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
        }
        else
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, leaveSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, targetPos) < 0.1f)
                Destroy(gameObject);
        }
    }

    public void SetTarget(Vector3 newTarget)
    {
        targetPos = newTarget;
    }

    public void LeaveScene()
    {
        isLeaving = true;
        targetPos = new Vector3(-14f, transform.position.y, transform.position.z);
    }

    public void SetSortingOrder(int order)
    {
        foreach (SpriteRenderer sr in spriteRenderers)
        {
            sr.sortingOrder = order;
        }
    }

    public void AcceptAndLeave()
    {
        isLeaving = true;
        targetPos = new Vector3(-14f, transform.position.y, transform.position.z);
    }

}
