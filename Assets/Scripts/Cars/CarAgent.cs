using System.Collections.Generic;
using UnityEngine;

public class CarAgent : MonoBehaviour
{
    public RoadSegment currentRoad;
    public int currentSlotIndex;

    public Queue<RoadSegment> route = new Queue<RoadSegment>();

    // Wizualizacja
    private Vector3 visualTargetPosition;
    public float visualSpeed = 10f; // Prêdkoœæ wizualna (m/s)

    // Konfiguracja
    public float carLength = 4.5f;

    // Statystyki przejazdu
    private float spawnTime;

    public void Initialize(RoadSegment startRoad, IEnumerable<RoadSegment> path)
    {
        route = new Queue<RoadSegment>(path);

        if (startRoad != null)
        {
            startRoad.EnterRoad(this);
            visualTargetPosition = startRoad.GetWorldPositionOfSlot(currentSlotIndex);
            transform.position = visualTargetPosition;
            transform.rotation = startRoad.transform.rotation;
        }
    }

    void Update()
    {
        if (Vector3.Distance(transform.position, visualTargetPosition) > 0.05f)
        {
            transform.position = Vector3.MoveTowards(transform.position, visualTargetPosition, Time.deltaTime * visualSpeed);

            Vector3 direction = (visualTargetPosition - transform.position).normalized;
            if (direction != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 5f);
            }
        }
    }

    // Metoda wywo³ywana przez RoadSegment, gdy auto zmienia slot lub drogê
    public void UpdateVisualTarget(Vector3 newPos)
    {
        visualTargetPosition = newPos;
    }

    public RoadSegment GetNextRoadFromRoute()
    {
        if (route.Count > 0)
            return route.Peek();
        return null;
    }

    public void PopRoute()
    {
        if (route.Count > 0) route.Dequeue();
    }
}