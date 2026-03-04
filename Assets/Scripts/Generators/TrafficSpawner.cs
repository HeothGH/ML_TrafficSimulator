using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TrafficSpawner : MonoBehaviour
{
    [Header("Referencje")]
    public SimulationManager simulationManager;
    public CarAgent carPrefab;

    [Header("Scenariusz")]
    public int seed = 12345;
    public int totalCarsInScenario = 30;

    private struct PendingCarRequest
    {
        public RoadSegment startRoad;
        public Queue<RoadSegment> path;
        public float aggressionBias;
    }

    private Queue<PendingCarRequest> spawnQueue = new Queue<PendingCarRequest>();
    private System.Random rng;
    private List<CarAgent> activeCars = new List<CarAgent>();

    void Start()
    {
        rng = new System.Random(seed);

        GenerateScenario();
    }

    void Update()
    {
        activeCars.RemoveAll(c => c == null);

        ProcessSpawnQueue();
    }

    private void GenerateScenario()
    {
        var allRoads = simulationManager.allRoads;

        if (allRoads == null || allRoads.Count < 2)
        {
            Debug.LogWarning("TrafficSpawner: Za ma³o dróg w SimulationManagerze!");
            return;
        }

        int carsCreated = 0;
        int safetyCounter = 0;

        Debug.Log($"Generowanie scenariusza (A*) dla {totalCarsInScenario} aut...");

        while (carsCreated < totalCarsInScenario && safetyCounter < totalCarsInScenario * 20)
        {
            safetyCounter++;

            RoadSegment startRoad = allRoads[rng.Next(allRoads.Count)];
            RoadSegment endRoad = allRoads[rng.Next(allRoads.Count)];

            if (startRoad == endRoad) continue;

            Queue<RoadSegment> route = FindPathAStar(startRoad, endRoad);

            if (route != null && route.Count > 0)
            {
                float randomBias = (float)rng.NextDouble() * 2.0f - 1.0f;
                spawnQueue.Enqueue(new PendingCarRequest
                {
                    startRoad = startRoad,
                    path = route,
                    aggressionBias = randomBias
                });
                carsCreated++;
            }
        }
        Debug.Log($"Utworzono kolejkê {spawnQueue.Count} pojazdów.");
    }

    private void ProcessSpawnQueue()
    {
        if (spawnQueue.Count == 0) return;

        PendingCarRequest nextCar = spawnQueue.Peek();
        Vector3 spawnPos = nextCar.startRoad.GetWorldPositionOfSlot(nextCar.startRoad.capacity - 1);
        if (Physics.CheckSphere(spawnPos, 2.5f, carPrefab.carLayer))
        {
            return;
        }
        if (nextCar.startRoad.CanEnter())
        {
            CarAgent car = Instantiate(carPrefab, Vector3.zero, Quaternion.identity);
            car.Initialize(nextCar.startRoad, nextCar.path, nextCar.aggressionBias);

            activeCars.Add(car);
            spawnQueue.Dequeue();
        }
    }


    private Queue<RoadSegment> FindPathAStar(RoadSegment start, RoadSegment target)
    {
        List<RoadSegment> openSet = new List<RoadSegment> { start };

        Dictionary<RoadSegment, RoadSegment> cameFrom = new Dictionary<RoadSegment, RoadSegment>();

        Dictionary<RoadSegment, float> gScore = new Dictionary<RoadSegment, float>();
        gScore[start] = 0;

        Dictionary<RoadSegment, float> fScore = new Dictionary<RoadSegment, float>();
        fScore[start] = Heuristic(start, target);

        while (openSet.Count > 0)
        {
            RoadSegment current = GetNodeWithLowestFScore(openSet, fScore);

            if (current == target)
            {
                return ConstructPath(cameFrom, target);
            }

            openSet.Remove(current);

            foreach (var connection in current.connectedRoads)
            {
                RoadSegment neighbor = connection.targetRoad;

                float distance = Vector3.Distance(current.transform.position, neighbor.transform.position);
                float tentativeGScore = gScore[current] + distance;

                if (!gScore.ContainsKey(neighbor) || tentativeGScore < gScore[neighbor])
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeGScore;
                    fScore[neighbor] = gScore[neighbor] + Heuristic(neighbor, target);

                    if (!openSet.Contains(neighbor))
                    {
                        openSet.Add(neighbor);
                    }
                }
            }
        }

        return null;
    }

    private float Heuristic(RoadSegment a, RoadSegment b)
    {
        return Vector3.Distance(a.transform.position, b.transform.position);
    }

    private RoadSegment GetNodeWithLowestFScore(List<RoadSegment> openSet, Dictionary<RoadSegment, float> fScore)
    {
        RoadSegment lowestNode = openSet[0];
        float lowestScore = fScore.ContainsKey(lowestNode) ? fScore[lowestNode] : float.MaxValue;

        for (int i = 1; i < openSet.Count; i++)
        {
            float score = fScore.ContainsKey(openSet[i]) ? fScore[openSet[i]] : float.MaxValue;
            if (score < lowestScore)
            {
                lowestScore = score;
                lowestNode = openSet[i];
            }
        }
        return lowestNode;
    }


    private Queue<RoadSegment> ConstructPath(Dictionary<RoadSegment, RoadSegment> cameFrom, RoadSegment end)
    {
        var path = new List<RoadSegment>();
        var curr = end;

        while (curr != null && cameFrom.ContainsKey(curr))
        {
            path.Add(curr);
            curr = cameFrom[curr];
        }

        if (curr != null && !path.Contains(curr))
        {
            path.Add(curr);
        }

        path.Reverse();

        if (path.Count > 0) path.RemoveAt(0);

        return new Queue<RoadSegment>(path);
    }
}