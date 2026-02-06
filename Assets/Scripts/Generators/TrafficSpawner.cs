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

            // ZMIANA: Wywo³anie A* zamiast BFS
            Queue<RoadSegment> route = FindPathAStar(startRoad, endRoad);

            if (route != null && route.Count > 0)
            {
                spawnQueue.Enqueue(new PendingCarRequest
                {
                    startRoad = startRoad,
                    path = route
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

        if (nextCar.startRoad.CanEnter())
        {
            CarAgent car = Instantiate(carPrefab, Vector3.zero, Quaternion.identity);
            car.Initialize(nextCar.startRoad, nextCar.path);

            activeCars.Add(car);
            spawnQueue.Dequeue();
        }
    }

    // --- IMPLEMENTACJA A* START ---

    private Queue<RoadSegment> FindPathAStar(RoadSegment start, RoadSegment target)
    {
        // Zbiór wêz³ów do rozpatrzenia (Open Set)
        List<RoadSegment> openSet = new List<RoadSegment> { start };

        // Mapa sk¹d przyszliœmy, aby odtworzyæ œcie¿kê
        Dictionary<RoadSegment, RoadSegment> cameFrom = new Dictionary<RoadSegment, RoadSegment>();

        // gScore: Koszt dotarcia od startu do danego wêz³a
        // Jeœli nie ma w s³owniku, traktujemy jako nieskoñczonoœæ
        Dictionary<RoadSegment, float> gScore = new Dictionary<RoadSegment, float>();
        gScore[start] = 0;

        // fScore: gScore + heurystyka (szacowany koszt do celu)
        Dictionary<RoadSegment, float> fScore = new Dictionary<RoadSegment, float>();
        fScore[start] = Heuristic(start, target);

        while (openSet.Count > 0)
        {
            // ZnajdŸ wêze³ z najni¿szym fScore w openSet
            RoadSegment current = GetNodeWithLowestFScore(openSet, fScore);

            // Czy dotarliœmy do celu?
            if (current == target)
            {
                return ConstructPath(cameFrom, target);
            }

            openSet.Remove(current);

            // Przejrzyj s¹siadów
            foreach (var connection in current.connectedRoads)
            {
                RoadSegment neighbor = connection.targetRoad;

                // Obliczamy tymczasowy gScore (koszt dotarcia do s¹siada przez 'current')
                float distance = Vector3.Distance(current.transform.position, neighbor.transform.position);
                float tentativeGScore = gScore[current] + distance;

                // Jeœli s¹siada nie ma w gScore (nieskoñczonoœæ) lub znaleŸliœmy lepsz¹ œcie¿kê
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

        // Nie znaleziono œcie¿ki
        return null;
    }

    // Funkcja heurystyczna: Dystans w linii prostej (Euclidean Distance)
    private float Heuristic(RoadSegment a, RoadSegment b)
    {
        return Vector3.Distance(a.transform.position, b.transform.position);
    }

    // Pomocnicza metoda do znalezienia wêz³a z najni¿szym F
    // (W wiêkszych projektach warto u¿yæ PriorityQueue, ale List wystarczy tutaj)
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

    // --- IMPLEMENTACJA A* KONIEC ---

    private Queue<RoadSegment> ConstructPath(Dictionary<RoadSegment, RoadSegment> cameFrom, RoadSegment end)
    {
        var path = new List<RoadSegment>();
        var curr = end;

        // Zabezpieczenie pêtli while, sprawdzamy czy klucz istnieje
        while (curr != null && cameFrom.ContainsKey(curr))
        {
            path.Add(curr);
            curr = cameFrom[curr]; // To zwróci nulla na starcie, jeœli start nie ma rodzica
        }

        // Dodajemy start rêcznie, bo pêtla mog³a go pomin¹æ (zale¿y jak cameFrom jest zainicjowane)
        // W BFS start mia³ null w cameFrom, tutaj w A* pêtla while(cameFrom.ContainsKey) to obs³u¿y
        // ale musimy upewniæ siê, ¿e dodajemy ostatni element (start), jeœli pêtla przerwie.
        if (curr != null && !path.Contains(curr))
        {
            path.Add(curr);
        }

        path.Reverse();

        // Usuwamy pierwszy element (startRoad), poniewa¿ auto ju¿ na nim stoi
        // Zgodnie z logik¹ Twojego poprzedniego BFS
        if (path.Count > 0) path.RemoveAt(0);

        return new Queue<RoadSegment>(path);
    }
}