using System.Collections.Generic;
using UnityEngine;

public class CarAgent : MonoBehaviour
{
    // Logika
    public RoadSegment currentRoad;
    public int currentSlotIndex;
    public Queue<RoadSegment> route = new Queue<RoadSegment>();
    private bool isLogicallyFinished = false;

    // Fizyka i Kolejka Celów
    private Queue<Vector3> waypoints = new Queue<Vector3>();
    private Vector3? currentTarget = null;

    [Header("Car Physics")]
    [Tooltip("Maksymalna prêdkoœæ (m/s)")]
    public float maxSpeed = 20f;      // 
    [Tooltip("Jak szybko rusza (m/s^2)")]
    public float acceleration = 5f;  // 
    [Tooltip("Jak szybko hamuje (m/s^2)")]
    public float deceleration = 10f;  // 
    [Tooltip("Prêdkoœæ skrêtu")]
    public float turnSpeed = 5f;      // 
    [Tooltip("Odleg³oœæ od punktu w celu zakolejkowania kolejnego.")]
    public float arrivalDistance = 1.0f;

    private float currentSpeed = 0f;
    private float spawnTime;

    public void Initialize(RoadSegment startRoad, IEnumerable<RoadSegment> path)
    {
        route = new Queue<RoadSegment>(path);
        spawnTime = Time.time;

        if (StatsManager.Instance != null) StatsManager.Instance.RegisterCarSpawn();

        if (startRoad != null)
        {
            startRoad.EnterRoad(this);
            transform.position = startRoad.GetWorldPositionOfSlot(currentSlotIndex);
            transform.rotation = startRoad.transform.rotation;
        }
    }


    public void AddWaypoint(Vector3 nextPoint)
    {
        waypoints.Enqueue(nextPoint);
    }

    public void AddWaypoints(IEnumerable<Vector3> points)
    {
        foreach (var p in points) waypoints.Enqueue(p);
    }

    void Update()
    {
        HandleMovement();
        if (waypoints.Count == 0 && isLogicallyFinished)
        {
            Destroy(gameObject);
        }
    }

    void HandleMovement()
    {
        if (currentTarget == null && waypoints.Count > 0)
        {
            currentTarget = waypoints.Dequeue();
        }

        if (currentTarget.HasValue)
        {
            float distance = Vector3.Distance(transform.position, currentTarget.Value);

            if (distance < arrivalDistance)
            {
                if (waypoints.Count > 0)
                {
                    currentTarget = waypoints.Dequeue();
                }
                else
                {
                    currentTarget = null;
                }
            }
        }

        // Fizyka prêdkoœci - Interpolacja liniowa
        bool shouldMove = currentTarget.HasValue;

        float targetSpeed = shouldMove ? maxSpeed : 0f;

        // P³ynna zmiana prêdkoœci (v = a * t)
        float speedChange = (shouldMove ? acceleration : deceleration) * Time.deltaTime;
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, speedChange);

        // Wykonanie Ruchu
        if (currentSpeed > 0.1f && currentTarget.HasValue)
        {
            Vector3 direction = (currentTarget.Value - transform.position).normalized;

            transform.position += direction * currentSpeed * Time.deltaTime;

            if (direction != Vector3.zero)
            {
                Quaternion lookRot = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * turnSpeed);
            }
        }
    }

    public RoadSegment GetNextRoadFromRoute()
    {
        if (route.Count > 0) return route.Peek();
        return null;
    }

    public void PopRoute() => route.Dequeue();

    private void OnDestroy()
    {
        float lifeTime = Time.time - spawnTime;
        if (StatsManager.Instance != null) StatsManager.Instance.RegisterCarFinish(lifeTime);
    }

    public void MarkAsFinished()
    {
        isLogicallyFinished = true;
    }
}