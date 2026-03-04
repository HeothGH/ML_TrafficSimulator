using System.Collections.Generic;
using UnityEditor.MemoryProfiler;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class RoadSegment : MonoBehaviour
{
    [Header("Settings")]
    public float carLengthWithGap = 6.0f;
    public TrafficLightController trafficLight;
    public int capacity;
    public List<CarAgent> slots;

    [Header("RL Priorities")]
    [Range(0, 2)]
    public int priority = 0; // 0 = Poboczna, 1 = Normalna, 2 = G³ówna
    private GameObject priorityLabelObj;
     
    [System.Serializable]
    public struct Connection
    {
        public RoadSegment targetRoad;
        public Intersection intersection;
    }

    [Header("Connections")]
    public List<Connection> connectedRoads;

    private BoxCollider roadCollider;
    private float segmentLength;

    void Awake()
    {
        roadCollider = GetComponent<BoxCollider>();

        segmentLength = roadCollider.size.z * transform.lossyScale.z;

        if (carLengthWithGap <= 0) carLengthWithGap = 6.0f;

        capacity = Mathf.FloorToInt(segmentLength / carLengthWithGap);
        if (capacity < 1) capacity = 1;

        slots = new List<CarAgent>(new CarAgent[capacity]);
    }
    public void SetupPriorityVisuals()
    {
        if (priorityLabelObj != null)
        {
            if (Application.isPlaying) Destroy(priorityLabelObj);
            else DestroyImmediate(priorityLabelObj);
        }

        priorityLabelObj = new GameObject("PriorityLabel");

        priorityLabelObj.transform.position = this.transform.position + Vector3.up * 3f;

        priorityLabelObj.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        priorityLabelObj.transform.localScale = new Vector3(2,2,2);

        TextMesh textMesh = priorityLabelObj.AddComponent<TextMesh>();
        textMesh.text = priority.ToString();
        textMesh.characterSize = 0.2f;
        textMesh.fontSize = 100;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;

        if (priority == 2) textMesh.color = Color.red;
        else if (priority == 1) textMesh.color = Color.yellow;
        else textMesh.color = Color.white;

        priorityLabelObj.transform.SetParent(this.transform, true);
    }


    public float GetCalculatedPenalty(float deltaTime)
    {
        float penaltySum = 0f;

        float timeMultiplier = 1.0f;
        if (priority == 1) timeMultiplier = 2.0f;
        else if (priority == 2) timeMultiplier = 5.0f;

        for (int i = 0; i < capacity; i++)
        {
            CarAgent car = slots[i];

            if (car != null)
            {
                if (car.CurrentSpeed < 2.0f)
                {
                    penaltySum += deltaTime * timeMultiplier;
                }
            }
        }

        return penaltySum;
    }

    public bool CanEnter()
    {
        return slots[capacity - 1] == null;
    }

    public void EnterRoad(CarAgent car)
    {
        if (CanEnter())
        {
            int entryIndex = capacity - 1;

            slots[entryIndex] = car;
            car.currentRoad = this;
            car.currentSlotIndex = entryIndex;
            car.roadSegment = this;
        }
        else
        {
            Debug.LogError($"B³¹d krytyczny: Próba wjazdu na pe³n¹ drogê {name}!");
        }
    }

    //public void Tick()
    //{
    //    for (int i = 0; i < capacity; i++)
    //    {
    //        CarAgent car = slots[i];

    //        if (car == null) continue;


    //        Vector3 logicalPos = GetWorldPositionOfSlot(i);
    //        float dist = Vector3.Distance(car.transform.position, logicalPos);

    //        if (dist > 12.0f)
    //        {
    //            continue;
    //        }
    //        // ----------------------------------------

    //        if (i == 0) // Pierwszy slot (wyjazd ze skrzy¿owania)
    //        {
    //            bool isGreen = (trafficLight == null || trafficLight.IsGreen);

    //            if (isGreen)
    //            {
    //                RoadSegment nextRoad = car.GetNextRoadFromRoute();

    //                if (nextRoad != null)
    //                {
    //                    Connection conn = connectedRoads.Find(c => c.targetRoad == nextRoad);

    //                    // Tu te¿ wa¿na zmiana: nextRoad.CanEnter() sprawdzi ostatni slot tamtej drogi.
    //                    // Jeœli tamto auto fizycznie jeszcze nie odjecha³o, CanEnter zwróci false.
    //                    if (conn.targetRoad != null && nextRoad.CanEnter())
    //                    {
    //                        Vector3 startPos = GetWorldPositionOfSlot(0);
    //                        Vector3 endPos = nextRoad.GetWorldPositionOfSlot(nextRoad.capacity - 1);

    //                        List<Vector3> pathPoints = new List<Vector3>();
    //                        if (conn.intersection != null)
    //                        {
    //                            pathPoints = conn.intersection.GetPathThroughIntersection(startPos, endPos);
    //                        }
    //                        else
    //                        {
    //                            pathPoints.Add(endPos); // Jazda prosto (usuniêty œrodek skrzy¿owania)
    //                        }

    //                        car.AddWaypoints(pathPoints);

    //                        // Tutaj ju¿ nie musimy dodawaæ endPos jako osobnego waypointa, 
    //                        // bo logika nextRoad zaraz przejmie auto i nada mu cel na slot.

    //                        slots[i] = null;
    //                        nextRoad.EnterRoad(car);
    //                        car.PopRoute();
    //                    }
    //                }
    //                else
    //                {
    //                    // Koniec trasy
    //                    slots[i] = null;
    //                    car.MarkAsFinished();
    //                }
    //            }
    //        }
    //        else // Kolejne sloty (jazda po prostej)
    //        {
    //            // Sprawdzamy czy miejsce przed nami jest wolne
    //            if (slots[i - 1] == null)
    //            {
    //                // PRZESUNIÊCIE LOGICZNE
    //                slots[i - 1] = car;
    //                slots[i] = null;

    //                car.currentSlotIndex = i - 1;

    //                // WIZUALIZACJA
    //                // Poniewa¿ auto "zaliczy³o" bramkê (jest blisko slotu i),
    //                // mo¿emy mu teraz bezpiecznie kazaæ jechaæ do slotu i-1.
    //                Vector3 nextSlotPos = GetWorldPositionOfSlot(i - 1);
    //                car.AddWaypoint(nextSlotPos);
    //            }
    //        }
    //    }
    //}
    public void Tick()
    {
        for (int i = 0; i < capacity; i++)
        {
            CarAgent car = slots[i];

            if (car == null) continue;

            Vector3 logicalPos = GetWorldPositionOfSlot(i);
            float dist = Vector3.Distance(car.transform.position, logicalPos);

            float allowedDistance = (i == capacity - 1) ? 35.0f : (carLengthWithGap * 1.5f);

            if (dist > allowedDistance)
            {
                continue;
            }

            if (i == 0)
            {
                RoadSegment nextRoad = car.GetNextRoadFromRoute();

                if (nextRoad == null)
                {
                    slots[i] = null;
                    car.MarkAsFinished();
                }
                else
                {
                    bool isGreen = (trafficLight == null || trafficLight.IsGreen);

                    if (isGreen)
                    {
                        Connection conn = connectedRoads.Find(c => c.targetRoad == nextRoad);

                        if (conn.targetRoad != null && nextRoad.CanEnter())
                        {
                            Vector3 startPos = GetWorldPositionOfSlot(0);
                            Vector3 endPos = nextRoad.GetWorldPositionOfSlot(nextRoad.capacity - 1);

                            List<Vector3> pathPoints = new List<Vector3>();
                            if (conn.intersection != null)
                            {
                                pathPoints = conn.intersection.GetPathThroughIntersection(startPos, endPos);
                            }
                            else
                            {
                                pathPoints.Add(endPos);
                            }

                            car.AddWaypoints(pathPoints);

                            slots[i] = null;
                            nextRoad.EnterRoad(car);
                            car.PopRoute();
                        }
                    }
                }
            }
            else
            {
                if (slots[i - 1] == null)
                {
                    slots[i - 1] = car;
                    slots[i] = null;

                    car.currentSlotIndex = i - 1;

                    Vector3 nextSlotPos = GetWorldPositionOfSlot(i - 1);
                    car.AddWaypoint(nextSlotPos);
                }
            }
        }
    }

    public Vector3 GetWorldPositionOfSlot(int slotIndex)
    {
        if (segmentLength <= 0.1f && roadCollider != null)
            segmentLength = roadCollider.size.z * transform.lossyScale.z;

        float stepSize = segmentLength / capacity;
        Vector3 roadEnd = transform.position + (transform.forward * (segmentLength * 0.5f));
        float offset = (slotIndex * stepSize) + (stepSize * 0.5f);
        return roadEnd - (transform.forward * offset);
    }

    void OnDrawGizmosSelected()
    {
        if (roadCollider == null) roadCollider = GetComponent<BoxCollider>();
        Gizmos.color = Color.cyan;
        if (capacity > 0)
        {
            for (int i = 0; i < capacity; i++)
            {
                float len = transform.lossyScale.z;
                float step = len / capacity;
                Vector3 start = transform.position + transform.forward * (len * 0.5f);
                Gizmos.DrawWireSphere(start - transform.forward * (i * step + step * 0.5f), 0.5f);
            }
        }
    }
    public void RemoveTrafficLight()
    {
        if (trafficLight != null)
        {
            if (Application.isPlaying) Destroy(trafficLight.gameObject);
            else DestroyImmediate(trafficLight.gameObject);

            trafficLight = null;
        }
    }

    public void InitializeCamera()
    {
        if (trafficLight == null) return;

        GameObject cameraObj = new GameObject("CCTV_Camera");
        cameraObj.transform.SetParent(this.transform);

        TrafficCamera trafficCam = cameraObj.AddComponent<TrafficCamera>();
        trafficCam.Setup(this);
    }
    void OnGUI()
    {
        #if UNITY_EDITOR
        if (UnityEditor.Selection.activeGameObject == this.gameObject)
        {
            Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 3f);

            if (screenPos.z > 0)
            {
                GUI.color = Color.black;
                GUIStyle style = new GUIStyle(GUI.skin.label);
                style.normal.textColor = Color.yellow;
                style.fontSize = 14;
                style.fontStyle = FontStyle.Bold;

                string debugText = $"--- ROAD DEBUG: {name} ---\nCapacity: {capacity}\n";

                for (int i = 0; i < capacity; i++)
                {
                    string carName = slots[i] != null ? slots[i].name : "EMPTY";
                    debugText += $"Slot [{i}]: {carName}\n";
                }

                GUI.Label(new Rect(screenPos.x - 75, Screen.height - screenPos.y, 300, 400), debugText, style);
            }
        }
        #endif
    }
}