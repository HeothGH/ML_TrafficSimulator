using System.Collections.Generic;
using UnityEngine;

public class CarAgent : MonoBehaviour
{
    // Logika
    public RoadSegment currentRoad; 
    public RoadSegment roadSegment;
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

    [Header("Personality")]
    [Tooltip("Warstwa, na której s¹ samochody")]
    public LayerMask carLayer;
    [Range(-1f, 1f)]
    public float aggressionBias = 0f;
    // Implementacja aktywnego tempomatu
    private float detectionRange = 10f;
    private float safeDistance = 2.0f;

    private float baseMaxSpeed;
    private float baseAcceleration;
    private float baseDeceleration;
    private Renderer carRenderer;

    private float currentSpeed = 0f;
    private float spawnTime;

    public int PendingWaypointsCount => waypoints.Count;
    void Awake()
    {
        // Zapamiêtujemy wartoœci ustawione w Inspectorze jako bazowe
        baseMaxSpeed = maxSpeed;
        baseAcceleration = acceleration;
        baseDeceleration = deceleration;

        // Szukamy renderera do zmiany koloru (mo¿e byæ na tym obiekcie lub w dziecku)
        carRenderer = GetComponentInChildren<Renderer>();
    }

    public void Initialize(RoadSegment startRoad, IEnumerable<RoadSegment> path, float bias = 0f)
    {
        route = new Queue<RoadSegment>(path);
        spawnTime = Time.time;

        // Aplikujemy osobowoœæ
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

        safeDistance = 1.5f - (aggressionBias * 0.75f);
        detectionRange = safeDistance * 5.0f;

        if (carRenderer != null)
        {
            if (aggressionBias < 0)
            {
                // Od Zielonego (-1) do ¯ó³tego (0)
                // bias + 1 daje zakres 0..1 dla tej po³ówki
                carRenderer.material.color = Color.Lerp(Color.green, Color.yellow, aggressionBias + 1f);
            }
            else
            {
                // Od ¯ó³tego (0) do Czerwonego (1)
                carRenderer.material.color = Color.Lerp(Color.yellow, Color.red, aggressionBias);
            }
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
        // 1. Pobieranie celu
        if (currentTarget == null && waypoints.Count > 0)
        {
            currentTarget = waypoints.Dequeue();
        }

        if (currentTarget.HasValue)
        {
            float distToTarget = Vector3.Distance(transform.position, currentTarget.Value);
            if (distToTarget < arrivalDistance)
            {
                if (waypoints.Count > 0) currentTarget = waypoints.Dequeue();
                else currentTarget = null;
            }
        }

        // 2. Wstêpna decyzja o prêdkoœci
        bool hasTarget = currentTarget.HasValue;
        float desiredSpeed = hasTarget ? maxSpeed : 0f;

        if (hasTarget)
        {
            Vector3 sensorStart = transform.position + transform.forward * 1.0f + Vector3.up * 0.5f;

            // --- NOWA LOGIKA: Celowanie Raycastem w logiczny slot ---
            Vector3 rayDirection = transform.forward; // Wartoœæ domyœlna

            // Upewniamy siê, ¿e mamy przypisan¹ drogê
            if (roadSegment != null)
            {
                // Pobieramy pozycjê slota docelowego na podstawie logiki
                Vector3 targetSlotPos = roadSegment.GetWorldPositionOfSlot(currentSlotIndex);

                // Wyrównujemy wysokoœæ (Y) do czujnika, ¿eby promieñ nie wbija³ siê w asfalt
                targetSlotPos.y = sensorStart.y;

                // Zabezpieczenie przed b³êdem, gdy auto jest dok³adnie w punkcie docelowym
                if (Vector3.Distance(sensorStart, targetSlotPos) > 0.1f)
                {
                    rayDirection = (targetSlotPos - sensorStart).normalized;
                }
            }

            RaycastHit hit;

            // Debug rysuje promieñ, ¿ebyœ widzia³ co auto "widzi"
            Debug.DrawRay(sensorStart, rayDirection * detectionRange, Color.blue);

            if (Physics.Raycast(sensorStart, rayDirection, out hit, detectionRange, carLayer))
            {
                // Trafiliœmy w coœ na warstwie "Car". SprawdŸmy co to jest.
                CarAgent otherCar = hit.collider.GetComponentInParent<CarAgent>();

                if (otherCar != null)
                {
                    // --- FILTR: Czy to auto jest na tej samej drodze? ---
                    if (otherCar.roadSegment == this.roadSegment)
                    {
                        // TAK - Reagujemy (hamujemy)
                        float distanceToCar = hit.distance;

                        if (distanceToCar < safeDistance)
                        {
                            // Awaryjne hamowanie
                            desiredSpeed = 0f;
                            Debug.DrawRay(sensorStart, rayDirection * distanceToCar, Color.red);
                        }
                        else
                        {
                            // Dostosowanie prêdkoœci
                            float factor = (distanceToCar - safeDistance) / (detectionRange - safeDistance);
                            desiredSpeed = Mathf.Lerp(0f, maxSpeed, factor);
                            Debug.DrawRay(sensorStart, rayDirection * distanceToCar, Color.yellow);
                        }
                    }
                    // Jeœli nie - auto jest na innej drodze, ignorujemy
                }
            }
        }

        // 3. Fizyka (Smooth)
        float speedChange = (desiredSpeed > currentSpeed ? acceleration : deceleration) * Time.deltaTime;
        currentSpeed = Mathf.MoveTowards(currentSpeed, desiredSpeed, speedChange);

        // 4. Ruch
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

    // Narzêdzie do debugowania stanu pojazdu
    // Narzêdzie do debugowania stanu pojazdu
    void OnGUI()
    {
#if UNITY_EDITOR
        // Rysuj UI tylko jeœli ten konkretny samochód jest zaznaczony w Hierarchy/Scene
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

                // --- NOWE: Budowanie stringa z trasy (Queue) ---
                string routeString = "";
                if (route != null && route.Count > 0)
                {
                    foreach (var r in route)
                    {
                        // Dodajemy nazwê drogi do ³añcucha
                        routeString += (r != null ? r.name.Replace("Road_P", "R") : "null") + " -> ";
                    }
                    // Odcinamy ostatnie " -> " dla estetyki
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
                        // Dodajemy nazwê drogi do ³añcucha
                        waypointsString += (w != null ? w.ToString() : "null") + " -> ";
                    }
                    // Odcinamy ostatnie " -> " dla estetyki
                    waypointsString = waypointsString.TrimEnd(' ', '-', '>');
                }
                else
                {
                    waypointsString = "NO WAYPOINTS";
                }
                // ------------------------------------------------

                string debugText = $"--- CAR DEBUG ---\n" +
                                   $"Speed: {currentSpeed:F2} / desired: {(currentTarget.HasValue ? maxSpeed : 0f):F2}\n" +
                                   $"Waypoints Count: {waypoints.Count}\n" +
                                   $"Road Segment: {(roadSegment != null ? roadSegment.name : "NULL")}\n" +
                                   $"Logical Slot Index: {currentSlotIndex}\n" +
                                   $"Is Finished?: {isLogicallyFinished}\n" +
                                   $"Route count: {route.Count}\n" + 
                                   $"Route: {routeString}\n" +
                                   $"Waypoints Queue: {waypointsString}\n" +
                                   $"Current target: {(currentTarget != null ? currentTarget : "NULL")}"
                                   ;

                // Powiêkszy³em nieco wysokoœæ (ostatni parametr Rect z 150 na 200), 
                // ¿eby d³ugi tekst trasy siê zmieœci³
                GUI.Label(new Rect(screenPos.x - 75, Screen.height - screenPos.y, 400, 200), debugText, style);
            }
        }
#endif
    }
}