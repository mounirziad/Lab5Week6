using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;
using TMPro;

[RequireComponent(typeof(NavMesh))]
public class Avoider : MonoBehaviour
{
    [Tooltip("The agent")]
    public NavMeshAgent agent;

    [Tooltip("The object this agent will avoid")]
    public GameObject avoidee;

    public float RangeValue;

    public float SpeedValue;

    public bool showgizmos = true;

    private bool isAvoiding = false;

    private List<Vector3> pointsplayercansee = new List<Vector3>();


    private List<Vector3> hidingspot = new List<Vector3>();

    public Vector2 samplingArea = new Vector2(20f, 20f);

    RaycastHit hit;

#if UNITY_EDITOR

    private void OnValidate()
    {
        if(avoidee == null)
        {
            Debug.LogWarning("An avoidee is required to run this project. Please create the avoidee object and assign it in inspector");

        }
        if (agent == null)
        {
            Debug.LogWarning("A navmeshagent is required to run this project. Please make the object a NavMesh Agent and bake a NavMesh");
        }
    }

#endif
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    // Update is called once per frame
    void Update()
    {
        if(agent == null || avoidee == null)
        {
            return;
        }

        RotationLogic();
    }

    public void RotationLogic()
    {
        Vector3 directiontoavoidee = (avoidee.transform.position - transform.position).normalized;

        if (directiontoavoidee != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(new Vector3(directiontoavoidee.x, 0, directiontoavoidee.z));

            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
        }
    }

    public void FindSpot()
    {
        isAvoiding = true;

        PoissonDiscSampler poissonDiscSampler = new PoissonDiscSampler(samplingArea.x, samplingArea.y, 5);

        foreach (var point in poissonDiscSampler.Samples())
        {
            Vector3 sampleWorldPoint = transform.position + new Vector3(point.x - samplingArea.x / 2, 0, point.y - samplingArea.y / 2);

            if (CheckVisibility(sampleWorldPoint) == false)
            {
                LineRenderer lineRenderer = new LineRenderer();
                lineRenderer.startColor = Color.green;
                lineRenderer.endColor = Color.green;
            }
        }
    }

    private bool CheckVisibility(Vector3 point)
    {
        if (Physics.Raycast(point + Vector3.up, (avoidee.transform.position - transform.position).normalized, out hit, RangeValue))
        {
            if (hit.transform != avoidee)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        return true;
    }

    private void OnDrawGizmos()
    {
        if(showgizmos == true)
        {
            Gizmos.DrawWireSphere(transform.position, 5);
        }

        if(showgizmos == false)
        {
            return;
        }
    }
}
