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
    public int priority = 0; // 0 = Poboczna, 1 = Wa¿na, 2 = G³ówna
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
        // 1. Sprz¹tanie po poprzednich próbach
        if (priorityLabelObj != null)
        {
            if (Application.isPlaying) Destroy(priorityLabelObj);
            else DestroyImmediate(priorityLabelObj);
        }

        // 2. Tworzymy obiekt "na czysto" - bez rodzica!
        priorityLabelObj = new GameObject("PriorityLabel");

        // 3. Ustawiamy go w pozycji drogi, ale 3 metry wy¿ej
        priorityLabelObj.transform.position = this.transform.position + Vector3.up * 3f;

        // 4. Ustawiamy rotacjê GLOBALN¥ (patrzy w niebo, góra tekstu na Pó³noc)
        // Dziêki temu tekst bêdzie czytelny dla kamery z góry, niezale¿nie jak skrêca droga
        priorityLabelObj.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        // 5. Ustawiamy idealn¹ skalê (1,1,1)
        priorityLabelObj.transform.localScale = new Vector3(2,2,2);

        // 6. Konfiguracja TextMesh dla ostroœci (Trick: Du¿y Font, Ma³y CharacterSize)
        TextMesh textMesh = priorityLabelObj.AddComponent<TextMesh>();
        textMesh.text = priority.ToString();
        textMesh.characterSize = 0.2f; // Skalujemy znak w dó³...
        textMesh.fontSize = 100;       // ...ale u¿ywamy tekstury wysokiej rozdzielczoœci
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;

        // Kolory priorytetów
        if (priority == 2) textMesh.color = Color.red;       // G³ówna
        else if (priority == 1) textMesh.color = Color.yellow; // Wa¿na
        else textMesh.color = Color.white;                   // Boczna

        // 7. KLUCZOWY MOMENT (Magia Unity):
        // Przypisujemy rodzica z flag¹ 'worldPositionStays = true'.
        // Unity automatycznie przeliczy LocalScale na jakieœ dziwne wartoœci (np. 1, 1, 0.02),
        // ¿eby wizualnie obiekt w œwiecie pozosta³ taki, jak go ustawiliœmy w punkcie 5.
        priorityLabelObj.transform.SetParent(this.transform, true);
    }


    public float GetCalculatedPenalty(float deltaTime)
    {
        int carsCount = 0;
        for (int i = 0; i < capacity; i++)
        {
            if (slots[i] != null) carsCount++;
        }

        if (carsCount == 0) return 0f;

        float timeMultiplier = 1.0f + (0.5f * priority);
        // Maksymalny mno¿nik to 2.0 (dla priority 2)

        return carsCount * deltaTime * timeMultiplier;
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
            car.roadSegment = this; // update referencji fizycznej
        }
        else
        {
            Debug.LogError($"B³¹d krytyczny: Próba wjazdu na pe³n¹ drogê {name}!");
        }
    }

    public void Tick()
    {
        for (int i = 0; i < capacity; i++)
        {
            CarAgent car = slots[i];
            if (car == null) continue;
            if (i == 0) // Pierwszy slot (przy skrzy¿owaniu)
            {
                bool isGreen = (trafficLight == null || trafficLight.IsGreen);

                if (isGreen)
                {
                    RoadSegment nextRoad = car.GetNextRoadFromRoute();

                    if (nextRoad != null)
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
                                pathPoints.Add(startPos);
                                pathPoints.Add(endPos);
                            }

                            car.AddWaypoints(pathPoints);
                            car.AddWaypoint(endPos);

                            slots[i] = null;
                            nextRoad.EnterRoad(car);
                            car.PopRoute();
                        }
                    }
                    else
                    {
                        // Koniec trasy
                        slots[i] = null;
                        car.MarkAsFinished();
                    }
                }
            }
            else // Kolejne sloty
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
        // Upewniamy siê, ¿e d³ugoœæ jest aktualna
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
}