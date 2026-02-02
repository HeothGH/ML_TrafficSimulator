using System.Collections.Generic;
using UnityEngine;

public class SimulationManager : MonoBehaviour
{
    [Header("Config")]
    public float tickRate = 2f;
    public int simulationSeed = 12345; // Do generowania mapy (przysz³oœæ)
    public int trafficSeed = 67890;    // Do generowania ruchu

    [Header("References")]
    public GameObject carPrefab;
    public List<RoadSegment> allRoads; // Przypisz w Inspectorze wszystkie drogi

    private float timer;
    private System.Random trafficRng;

    List<RoadSegment> testPath = new List<RoadSegment>();
    List<RoadSegment> testPath1 = new List<RoadSegment>();
    List<RoadSegment> testPath2 = new List<RoadSegment>();
    List<RoadSegment> testPath3 = new List<RoadSegment>();

    void Start()
    {
        // Inicjalizacja RNG
        trafficRng = new System.Random(trafficSeed);

        //testPath.Enqueue(allRoads[0]); testPath.Enqueue(allRoads[1]);
        testPath.Add(allRoads[1]);
        //testPath1.Enqueue(allRoads[3]); testPath1.Enqueue(allRoads[0]); testPath1.Enqueue(allRoads[1]);
        testPath1.Add(allRoads[0]); testPath1.Add(allRoads[1]);
        //testPath2.Enqueue(allRoads[2]); testPath2.Enqueue(allRoads[3]); testPath2.Enqueue(allRoads[0]); testPath2.Enqueue(allRoads[1]);
        testPath2.Add(allRoads[3]); testPath2.Add(allRoads[0]); testPath2.Add(allRoads[1]);
        //testPath3.Enqueue(allRoads[1]); testPath3.Enqueue(allRoads[2]); testPath3.Enqueue(allRoads[3]); testPath3.Enqueue(allRoads[0]); testPath3.Enqueue(allRoads[1]);
        testPath3.Add(allRoads[2]); testPath3.Add(allRoads[3]); testPath3.Add(allRoads[0]); testPath3.Add(allRoads[1]);

        // TEST: Spawn aut testowych na sztywno
        SpawnTestCar(allRoads[0], testPath);
        //SpawnTestCar(testPath1);
        SpawnTestCar(allRoads[3], testPath1);
        //SpawnTestCar(testPath2);
        SpawnTestCar(allRoads[2], testPath2);
        //SpawnTestCar(testPath3);
        SpawnTestCar(allRoads[1], testPath3);
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= tickRate)
        {
            Step();
            timer = 0;
        }
    }

    public void Step()
    {
        // Aktualizujemy wszystkie drogi
        foreach (var road in allRoads)
        {
            road.Tick();
        }
    }

    void SpawnTestCar(RoadSegment startRoad, List<RoadSegment> path)
    {
        if (path.Count < 1) return;

        // Upewnij siê, ¿e s¹ po³¹czone w Inspectorze (connectedRoads)!

        GameObject carObj = Instantiate(carPrefab);
        CarAgent agent = carObj.GetComponent<CarAgent>();


        agent.Initialize(startRoad, path);
    }
}