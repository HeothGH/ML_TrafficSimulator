using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class SimulationManager : MonoBehaviour
{
    [Header("Config")]
    public float tickRate = 2f;
    public int simulationSeed = 12345; // Do generowania mapy (todo)
    public int trafficSeed = 67890;    // Do generowania ruchu (todo)

    [Header("References")]
    public GameObject carPrefab;
    public List<RoadSegment> allRoads; // opcjonalne do przypisania rêcznego

    private float timer;
    private System.Random trafficRng;

    List<RoadSegment> testPath = new List<RoadSegment>();
    //List<RoadSegment> testPath1 = new List<RoadSegment>();
    //List<RoadSegment> testPath2 = new List<RoadSegment>();
    //List<RoadSegment> testPath3 = new List<RoadSegment>();
    [Header("Generator (Opcjonalne)")]
    public GridMapGenerator mapGenerator;

    void Start()
    {

        //0 -> 1 -> 2 -> 3 -> 0
        testPath.Add(allRoads[1]);  testPath.Add(allRoads[2]); testPath.Add(allRoads[3]);
        //testPath1.Enqueue(allRoads[3]); testPath1.Enqueue(allRoads[0]); testPath1.Enqueue(allRoads[1]);
        //testPath1.Add(allRoads[0]); testPath1.Add(allRoads[1]); testPath1.Add(allRoads[2]); testPath1.Add(allRoads[3]); testPath1.Add(allRoads[0]); testPath1.Add(allRoads[1]);
        //testPath2.Enqueue(allRoads[2]); testPath2.Enqueue(allRoads[3]); testPath2.Enqueue(allRoads[0]); testPath2.Enqueue(allRoads[1]);
        //testPath2.Add(allRoads[3]); testPath2.Add(allRoads[0]); testPath2.Add(allRoads[1]);  testPath2.Add(allRoads[2]); testPath2.Add(allRoads[3]); testPath2.Add(allRoads[0]); testPath2.Add(allRoads[1]);
        //testPath3.Enqueue(allRoads[1]); testPath3.Enqueue(allRoads[2]); testPath3.Enqueue(allRoads[3]); testPath3.Enqueue(allRoads[0]); testPath3.Enqueue(allRoads[1]);
        //testPath3.Add(allRoads[2]); testPath3.Add(allRoads[3]); testPath3.Add(allRoads[0]); testPath3.Add(allRoads[1]); testPath3.Add(allRoads[2]); testPath3.Add(allRoads[3]); testPath3.Add(allRoads[0]); testPath3.Add(allRoads[1]);
        
        if(mapGenerator == null) 
        { 
            SpawnTestCar(allRoads[0], testPath);
        }
        // TEST: Spawn aut testowych na sztywno
        //SpawnTestCarsSecondWith1SecDelay();
        //SpawnTestCar(allRoads[3], testPath1);
        ////SpawnTestCar(testPath2);
        //SpawnTestCar(allRoads[2], testPath2);
        ////SpawnTestCar(testPath3);
        //SpawnTestCar(allRoads[1], testPath3);
    }
    void Awake()
    {
        if (mapGenerator != null)
        {
            mapGenerator.GenerateMap();

            if (mapGenerator.AllRoadSegments != null && mapGenerator.AllRoadSegments.Count > 0)
            {
                allRoads.AddRange(mapGenerator.AllRoadSegments);
            }
        }
    }

    void SpawnTestCarsSecondWith1SecDelay()
    {
        SpawnTestCar(allRoads[0], testPath);
        Invoke("SpawnTestCarsSecondWith1SecDelay", 1f);
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= tickRate)
        {
            Step();
            timer = 0;
        }
        if(Keyboard.current.rKey.wasPressedThisFrame)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    public void Step()
    {
        foreach (var road in allRoads)
        {
            road.Tick();
        }
    }

    void SpawnTestCar(RoadSegment startRoad, List<RoadSegment> path)
    {
        if (path.Count < 1) return;

        GameObject carObj = Instantiate(carPrefab);
        CarAgent agent = carObj.GetComponent<CarAgent>();


        agent.Initialize(startRoad, path);
    }
}