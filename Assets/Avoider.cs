using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class Avoider : MonoBehaviour
{
    [Tooltip("The agent that will avoid the avoidee")]
    public NavMeshAgent agent;

    [Tooltip("The object this agent will avoid")]
    public GameObject avoidee;

    [Tooltip("How close the avoidee can get before avoidance kicks in")]
    public float RangeValue = 10f;

    [Tooltip("Speed at which the avoider escapes")]
    public float SpeedValue = 3.5f;

    [Tooltip("Toggle gizmo visualization")]
    public bool showGizmos = true;

    public Vector2 samplingArea = new Vector2(20f, 20f);

    private bool isAvoiding = false;
    private List<Vector3> hidingSpots = new List<Vector3>();
    private RaycastHit hit;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (avoidee == null)
        {
            Debug.LogWarning("An avoidee is required to run this project. Please assign it in the inspector.");
        }
        if (agent == null)
        {
            Debug.LogWarning("A NavMeshAgent is required to run this project. Please add one and bake a NavMesh.");
        }
    }
#endif

    void Start()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        agent.speed = SpeedValue;
    }

    void Update()
    {
        if (agent == null || avoidee == null) return;

        RotationLogic();

        float distance = Vector3.Distance(transform.position, avoidee.transform.position);

        if (distance <= RangeValue && !isAvoiding)
        {
            FindSpot();
        }
    }

    private void RotationLogic()
    {
        Vector3 direction = (avoidee.transform.position - transform.position).normalized;
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
        }
    }

    private void FindSpot()
    {
        isAvoiding = true;
        hidingSpots.Clear();

        PoissonDiscSampler sampler = new PoissonDiscSampler(samplingArea.x, samplingArea.y, 3);

        foreach (var point in sampler.Samples())
        {
            Vector3 worldPoint = transform.position + new Vector3(point.x - samplingArea.x / 2, 0, point.y - samplingArea.y / 2);

            // Check if this point is not visible to the avoidee
            if (!CheckVisibility(worldPoint))
            {
                hidingSpots.Add(worldPoint);
            }
        }

        if (hidingSpots.Count > 0)
        {
            // Choose the closest hiding spot
            Vector3 bestSpot = hidingSpots[0];
            float minDist = Vector3.Distance(transform.position, bestSpot);

            foreach (var spot in hidingSpots)
            {
                float dist = Vector3.Distance(transform.position, spot);
                if (dist < minDist)
                {
                    bestSpot = spot;
                    minDist = dist;
                }
            }

            // Move the agent to that spot
            agent.SetDestination(bestSpot);
        }
        else
        {
            // fallback: run directly away from player
            Vector3 dirAway = (transform.position - avoidee.transform.position).normalized;
            Vector3 runPoint = transform.position + dirAway * RangeValue;
            agent.SetDestination(runPoint);
        }

        isAvoiding = false;
    }

    private bool CheckVisibility(Vector3 point)
    {
        Vector3 eyePos = avoidee.transform.position + Vector3.up * 1.5f;
        Vector3 dir = point - eyePos;
        float dist = dir.magnitude;

        // If avoidee has a clear line to the point, then it's visible
        if (Physics.Raycast(eyePos, dir.normalized, out hit, dist))
        {
            // If the ray hit something *before* the hiding point, then it's blocked
            if (hit.collider.gameObject != gameObject)
                return false;
        }

        // No obstruction → avoidee can see this point
        return true;
    }


    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, RangeValue);

        Gizmos.color = Color.green;
        if (hidingSpots != null)
        {
            foreach (var spot in hidingSpots)
            {
                Gizmos.DrawSphere(spot, 0.2f);
            }
        }
    }
}
