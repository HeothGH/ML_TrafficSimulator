using System.Collections.Generic;
using UnityEngine;

public class CarAgent : MonoBehaviour
{
    [Header("Logic")]
    public RoadSegment currentRoad;
    public RoadSegment roadSegment;
    public int currentSlotIndex;
    public Queue<RoadSegment> route = new Queue<RoadSegment>();
    private bool isLogicallyFinished = false;

    private Queue<Vector3> waypoints = new Queue<Vector3>();
    private Vector3? currentTarget = null;

    [Header("Car Physics")]
    public float maxSpeed = 20f;
    public float acceleration = 5f;
    public float deceleration = 10f;
    public float turnSpeed = 5f;
    public float arrivalDistance = 2.0f;

    [Header("Personality")]
    public LayerMask carLayer;
    [Range(-1f, 1f)]
    public float aggressionBias = 0f;

    private float detectionRange = 10f;
    private float safeDistance = 2.0f;

    [Header("Watchdog (Anti-Deadlock)")]
    [Tooltip("Ile sekund auto musi staæ w miejscu, ¿eby odpaliæ Tryb Ducha")]
    public float stuckThreshold = 8.0f;
    private float stuckTimer = 0f;
    private float ghostTimer = 0f;

    private float baseMaxSpeed;
    private float baseAcceleration;
    private float baseDeceleration;
    private Renderer carRenderer;

    private float currentSpeed = 0f;
    public float CurrentSpeed => currentSpeed;
    private float spawnTime;

    public int PendingWaypointsCount => waypoints.Count;

    void Awake()
    {
        baseMaxSpeed = maxSpeed;
        baseAcceleration = acceleration;
        baseDeceleration = deceleration;
        carRenderer = GetComponentInChildren<Renderer>();
    }

    public void Initialize(RoadSegment startRoad, IEnumerable<RoadSegment> path, float bias = 0f)
    {
        route = new Queue<RoadSegment>(path);
        spawnTime = Time.time;

        aggressionBias = bias;
        ApplyAggressionProfile();

        if (StatsManager.Instance != null) StatsManager.Instance.RegisterCarSpawn();

        if (startRoad != null)
        {
            startRoad.EnterRoad(this);
            transform.position = startRoad.GetWorldPositionOfSlot(currentSlotIndex);
            transform.rotation = startRoad.transform.rotation;
        }
    }

    private void ApplyAggressionProfile()
    {
        maxSpeed = baseMaxSpeed * (1.0f + (aggressionBias * 0.2f));
        acceleration = baseAcceleration * (1.0f + (aggressionBias * 0.4f));
        deceleration = baseDeceleration * (1.0f + (aggressionBias * 0.3f));

        safeDistance = 2.0f - (aggressionBias * 0.5f);

        float stoppingDistance = (maxSpeed * maxSpeed) / (2f * deceleration);
        detectionRange = stoppingDistance + safeDistance + 2.0f;

        if (carRenderer != null)
        {
            if (aggressionBias < 0)
                carRenderer.material.color = Color.Lerp(Color.green, Color.yellow, aggressionBias + 1f);
            else
                carRenderer.material.color = Color.Lerp(Color.yellow, Color.red, aggressionBias);
        }
    }

    public void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Lane"))
        {
            roadSegment = other.GetComponent<RoadSegment>();
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

    void FixedUpdate()
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
            float dynamicArrival = Mathf.Max(arrivalDistance, currentSpeed * Time.fixedDeltaTime * 1.5f);
            float distToTarget = Vector3.Distance(transform.position, currentTarget.Value);

            if (distToTarget < dynamicArrival)
            {
                if (waypoints.Count > 0) currentTarget = waypoints.Dequeue();
                else currentTarget = null;
            }
        }

        bool hasTarget = currentTarget.HasValue;
        float desiredSpeed = hasTarget ? maxSpeed : 0f;

        if (hasTarget)
        {
            Vector3 sensorStart = transform.position + Vector3.up * 0.5f + transform.forward * 1.5f;

            RaycastHit hit;

            if (Physics.Raycast(sensorStart, transform.forward, out hit, detectionRange, carLayer))
            {
                CarAgent otherCar = hit.collider.GetComponentInParent<CarAgent>();

                if (otherCar != null && otherCar != this)
                {
                    Vector3 toOther = otherCar.transform.position - transform.position;
                    float dist = toOther.magnitude;

                    // Zabezpieczenie na skrajne na³o¿enie siê obiektów
                    if (dist > 0.1f)
                    {
                        Vector3 dirToOther = toOther.normalized;

                        if (Vector3.Dot(transform.forward, dirToOther) > 0.3f)
                        {
                            float angle = Vector3.Angle(transform.forward, otherCar.transform.forward);
                            if (angle < 135f)
                            {
                                float actualDistance = hit.distance + 1.5f;

                                if (actualDistance <= safeDistance)
                                {
                                    desiredSpeed = 0f;
                                }
                                else
                                {
                                    float maxSafeSpeed = Mathf.Sqrt(2f * deceleration * (actualDistance - safeDistance));
                                    desiredSpeed = Mathf.Min(maxSpeed, maxSafeSpeed);
                                }

                                Debug.DrawRay(sensorStart, transform.forward * hit.distance, Color.red);
                            }
                            else
                            {
                                Debug.DrawRay(sensorStart, transform.forward * detectionRange, Color.green);
                            }
                        }
                        else
                        {
                            Debug.DrawRay(sensorStart, transform.forward * detectionRange, Color.green);
                        }
                    }
                }
            }
            else
            {
                Debug.DrawRay(sensorStart, transform.forward * detectionRange, Color.green);
            }
        }

        if (currentSpeed < 0.5f && hasTarget)
        {
            stuckTimer += Time.fixedDeltaTime;
            if (stuckTimer > stuckThreshold)
            {
                ghostTimer = 5.0f;
                stuckTimer = 0f;
            }
        }
        else
        {
            stuckTimer = 0f; // Resetujemy timer, gdy auto jedzie
        }

        
        if (ghostTimer > 0f)
        {
            ghostTimer -= Time.fixedDeltaTime;
            desiredSpeed = maxSpeed;
        }
        else if (ghostTimer <= 0f && ghostTimer > -1f)
        {
            ApplyAggressionProfile();
            ghostTimer = -2f;
        }

        float speedChange = (desiredSpeed > currentSpeed ? acceleration : deceleration) * Time.fixedDeltaTime;
        currentSpeed = Mathf.MoveTowards(currentSpeed, desiredSpeed, speedChange);

        if (currentSpeed > 0.1f && currentTarget.HasValue)
        {
            Vector3 direction = (currentTarget.Value - transform.position).normalized;
            transform.position += direction * currentSpeed * Time.fixedDeltaTime;

            if (direction != Vector3.zero)
            {
                Quaternion lookRot = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.fixedDeltaTime * turnSpeed);
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

        if (StatsManager.Instance != null && isLogicallyFinished)
        {
            StatsManager.Instance.RegisterCarFinish(lifeTime);
        }

        if (isLogicallyFinished)
        {
            Debug.Log($"[Traffic] Samochód {name} dojecha³ do celu! Czas: {lifeTime:F2}s, ³¹czny aktualny czas przejazdów: {StatsManager.Instance.totalTravelTime}s");
        }
        else
        {
            Debug.Log($"[Traffic] Samochód {name} zniszczony przed celem (czas: {lifeTime:F2}s)");
            StatsManager.Instance.RegisterCarWronglyDestroyed();
        }
    }

    public void MarkAsFinished()
    {
        isLogicallyFinished = true;
    }

    void OnGUI()
    {
#if UNITY_EDITOR
        if (UnityEditor.Selection.activeGameObject == this.gameObject)
        {
            Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 2f);

            if (screenPos.z > 0)
            {
                GUI.color = Color.black;
                GUIStyle style = new GUIStyle(GUI.skin.label);
                style.normal.textColor = Color.white;
                style.fontSize = 14;
                style.fontStyle = FontStyle.Bold;

                string routeString = "";
                if (route != null && route.Count > 0)
                {
                    foreach (var r in route)
                    {
                        routeString += (r != null ? r.name.Replace("Road_P", "R") : "null") + " -> ";
                    }
                    routeString = routeString.TrimEnd(' ', '-', '>');
                }
                else
                {
                    routeString = "EMPTY / REACHED TARGET";
                }
                string waypointsString = "";
                if (waypoints != null && waypoints.Count > 0)
                {
                    foreach (var w in waypoints)
                    {
                        waypointsString += (w != null ? w.ToString() : "null") + " -> ";
                    }
                    waypointsString = waypointsString.TrimEnd(' ', '-', '>');
                }
                else
                {
                    waypointsString = "NO WAYPOINTS";
                }

                string debugText = $"--- CAR DEBUG ---\n" +
                                   $"Speed: {currentSpeed:F2} / desired: {(currentTarget.HasValue ? maxSpeed : 0f):F2}\n" +
                                   $"Waypoints Count: {waypoints.Count}\n" +
                                   $"Road Segment: {(roadSegment != null ? roadSegment.name : "NULL")}\n" +
                                   $"Logical Slot Index: {currentSlotIndex}\n" +
                                   $"Is Finished?: {isLogicallyFinished}\n" +
                                   $"Route count: {route.Count}\n" +
                                   $"Route: {routeString}\n" +
                                   $"Waypoints Queue: {waypointsString}\n" +
                                   $"Current target: {(currentTarget != null ? currentTarget : "NULL")}";

                GUI.Label(new Rect(screenPos.x - 75, Screen.height - screenPos.y, 400, 200), debugText, style);
            }
        }
#endif
    }
}