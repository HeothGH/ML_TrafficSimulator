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

        segmentLength = Mathf.Max(roadCollider.bounds.size.z, roadCollider.bounds.size.x);

        if (carLengthWithGap <= 0) carLengthWithGap = 6.0f;

        capacity = Mathf.FloorToInt(segmentLength / carLengthWithGap);
        if (capacity < 1) capacity = 1;

        slots = new List<CarAgent>(new CarAgent[capacity]);
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

            if (i == 0)
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
                        // End of the line
                        slots[i] = null;
                        car.MarkAsFinished();
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

                    // WIZUALIZACJA
                    // "S³uchaj, logicznie jesteœ ju¿ na polu 3, ale stoisz na 4. 
                    // Dodajê ci pole 3 do listy celów. JedŸ tam p³ynnie."
                    Vector3 nextSlotPos = GetWorldPositionOfSlot(i - 1);
                    car.AddWaypoint(nextSlotPos);
                }
            }
        }
    }

    public Vector3 GetWorldPositionOfSlot(int slotIndex)
    {
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