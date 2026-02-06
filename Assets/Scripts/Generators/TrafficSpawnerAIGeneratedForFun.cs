using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TrafficSpawnerAIGeneratedForFun : MonoBehaviour
{
    [Header("Referencje")]
    public GridMapGenerator mapGenerator;
    public CarAgent carPrefab;

    [Header("Ustawienia Symulacji (Taktowanie)")]
    [Tooltip("Co ile sekund przesuwaæ logikê aut o 1 slot? (np. 0.5s - 1.0s)")]
    public float simulationStep = 0.5f;
    public bool runSimulationLogic = true;

    [Header("Ustawienia Spawnowania")]
    public int seed = 42;
    public float spawnInterval = 2.0f; // Co ile sekund próbowaæ dodaæ nowe auto do kolejki
    public int maxCars = 20;

    // Prywatne zmienne
    private float simTimer;
    private float spawnTimer;
    private System.Random rng;
    private List<CarAgent> activeCars = new List<CarAgent>();

    // Struktura pomocnicza dla kolejki oczekuj¹cych
    private class PendingCarRequest
    {
        public RoadSegment startRoad;
        public Queue<RoadSegment> path;
    }

    // Kolejka aut oczekuj¹cych na wjazd (FIFO)
    private Queue<PendingCarRequest> spawnQueue = new Queue<PendingCarRequest>();

    void Start()
    {
        rng = new System.Random(seed);

        // Upewnij siê, ¿e mapa jest wygenerowana
        if (mapGenerator.AllRoadSegments.Count == 0)
        {
            mapGenerator.GenerateMap();
        }
    }

    void Update()
    {
        // 1. Obs³uga Logiki Symulacji (Tick)
        // Wykonujemy to rzadziej (co simulationStep), ¿eby auta mia³y czas przejechaæ wizualnie
        if (runSimulationLogic)
        {
            simTimer += Time.deltaTime;
            if (simTimer >= simulationStep)
            {
                simTimer = 0;
                AdvanceSimulation();
            }
        }

        // 2. Czyszczenie listy aktywnych aut (usuniêcie zniszczonych)
        activeCars.RemoveAll(c => c == null);

        // 3. Dodawanie nowych zleceñ do kolejki (Generator ruchu)
        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnInterval && activeCars.Count + spawnQueue.Count < maxCars)
        {
            spawnTimer = 0;
            QueueNewCar();
        }

        // 4. Próba wypuszczenia aut z kolejki na mapê
        ProcessSpawnQueue();
    }

    // To jest "serce" ruchu - przesuwa auta w slotach
    private void AdvanceSimulation()
    {
        foreach (var road in mapGenerator.AllRoadSegments)
        {
            if (road != null) road.Tick();
        }
    }

    // Tworzy logiczne zlecenie przejazdu i dodaje do kolejki
    private void QueueNewCar()
    {
        var allRoads = mapGenerator.AllRoadSegments;
        if (allRoads.Count < 2) return;

        // Losujemy start i koniec
        // Nie sprawdzamy tu CanEnter(), bo auto mo¿e poczekaæ w kolejce!
        RoadSegment startRoad = allRoads[rng.Next(allRoads.Count)];
        RoadSegment endRoad = allRoads[rng.Next(allRoads.Count)];

        // Zabezpieczenie: Start musi byæ ró¿ny od koñca
        int attempts = 0;
        while (startRoad == endRoad && attempts < 10)
        {
            endRoad = allRoads[rng.Next(allRoads.Count)];
            attempts++;
        }
        if (startRoad == endRoad) return; // Poddajemy siê

        // Wyznaczamy trasê
        Queue<RoadSegment> route = FindPathBFS(startRoad, endRoad);

        if (route != null && route.Count > 0)
        {
            // Dodajemy do kolejki oczekuj¹cych
            spawnQueue.Enqueue(new PendingCarRequest
            {
                startRoad = startRoad,
                path = route
            });
        }
    }

    // Sprawdza czy pierwsze auto w kolejce mo¿e wjechaæ. Jeœli tak - spawnuje.
    private void ProcessSpawnQueue()
    {
        if (spawnQueue.Count == 0) return;

        // Patrzymy na pierwsze auto w kolejce
        PendingCarRequest request = spawnQueue.Peek();

        // Czy droga startowa ma wolne miejsce na wjeŸdzie?
        if (request.startRoad.CanEnter())
        {
            // SPAWN!
            CarAgent car = Instantiate(carPrefab, Vector3.zero, Quaternion.identity);

            // Inicjalizacja przeniesie auto na w³aœciw¹ pozycjê
            car.Initialize(request.startRoad, request.path);

            activeCars.Add(car);

            // Usuwamy obs³u¿one auto z kolejki
            spawnQueue.Dequeue();
        }
        // Jeœli nie CanEnter(), to nic nie robimy. 
        // Auto zostaje na pocz¹tku kolejki (Queue.Peek) i spróbuje w nastêpnej klatce.
        // Blokuje to kolejne auta w kolejce (jak w prawdziwym korku przy wyjeŸdzie z gara¿u).
    }

    // --- BFS (Bez zmian) ---
    private Queue<RoadSegment> FindPathBFS(RoadSegment start, RoadSegment target)
    {
        var frontier = new Queue<RoadSegment>();
        frontier.Enqueue(start);

        var cameFrom = new Dictionary<RoadSegment, RoadSegment>();
        cameFrom[start] = null;

        while (frontier.Count > 0)
        {
            var current = frontier.Dequeue();

            if (current == target) return ConstructPath(cameFrom, target);

            // Deterministyczne losowanie s¹siadów
            var neighbors = current.connectedRoads
                .Select(c => c.targetRoad)
                .OrderBy(x => rng.Next())
                .ToList();

            foreach (var next in neighbors)
            {
                if (!cameFrom.ContainsKey(next))
                {
                    frontier.Enqueue(next);
                    cameFrom[next] = current;
                }
            }
        }
        return null;
    }

    private Queue<RoadSegment> ConstructPath(Dictionary<RoadSegment, RoadSegment> cameFrom, RoadSegment end)
    {
        var path = new List<RoadSegment>();
        var curr = end;
        while (curr != null)
        {
            path.Add(curr);
            curr = cameFrom[curr];
        }
        path.Reverse();

        // Usuwamy pierwszy element (startRoad), bo auto ju¿ na nim stoi.
        // CarAgent potrzebuje listy KOLEJNYCH dróg.
        if (path.Count > 0) path.RemoveAt(0);

        return new Queue<RoadSegment>(path);
    }
}