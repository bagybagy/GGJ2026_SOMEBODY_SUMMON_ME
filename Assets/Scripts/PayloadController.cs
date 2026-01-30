using UnityEngine;
using UnityEngine.AI;

public class PayloadController : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] Transform goal;
    [SerializeField] NavMeshAgent agent;
    [SerializeField] Animator animator;

    // 接触しているプレイヤー数（剣と体など複数接触対策）
    private int rideCount = 0;

    void Start()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        agent.isStopped = true;
    }

    // 💡 RootにRigidbodyがあれば、子のTrigger接触もここで検知できる
    void OnTriggerEnter(Collider other)
    {
        // Player以外（敵や壁）は無視
        if (other.CompareTag("Player"))
        {
            rideCount++;
            UpdateMoveState();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            rideCount--;
            UpdateMoveState();
        }
    }

    void UpdateMoveState()
    {
        if (goal == null) return;

        // 1つでもPlayerのコライダーが触れていれば進む
        if (rideCount > 0)
        {
            agent.isStopped = false;
            agent.SetDestination(goal.position);
            if (animator != null) animator.SetBool("Run", true);
        }
        else
        {
            agent.isStopped = true;
            if (animator != null) animator.SetBool("Run", false);
        }
    }
}