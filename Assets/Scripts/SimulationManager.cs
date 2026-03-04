using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class SimulationManager : MonoBehaviour
{
    [Header("Config")]
    [Tooltip("Co ile klatek fizycznych ma siê wykonywaæ logika symulacji? Np. 5 oznacza co 0.1s (przy fixedTime 0.02)")]
    public int logicFrameInterval = 5;
    public int simulationSeed = 12345;
    // public float timeScale = 1f;

    [Header("References")]
    public GameObject carPrefab;
    public List<RoadSegment> allRoads = new List<RoadSegment>();
    public GridMapGenerator mapGenerator;

    private int fixedFrameCount = 0;

    private float logicDeltaTime;

    void Awake()
    {
        logicDeltaTime = logicFrameInterval * Time.fixedDeltaTime;

        Debug.Log($"[SimManager] Logika dzia³a co {logicDeltaTime.ToString("F1")}s ({logicFrameInterval} klatek fizyki).");

        if (mapGenerator != null)
        {
            mapGenerator.seed = simulationSeed;
            mapGenerator.GenerateMap();

            if (mapGenerator.AllRoadSegments != null)
            {
                allRoads.AddRange(mapGenerator.AllRoadSegments);
            }
            CenterCameraOnMap();
        }

        foreach (var road in allRoads)
        {
            road.InitializeCamera();
        }

        // Time.timeScale = Mathf.Max(0.1f, this.timeScale);
    }

    void FixedUpdate()
    {
        fixedFrameCount++;

        if (fixedFrameCount % logicFrameInterval == 0)
        {
            Step(logicDeltaTime);
        }
        CheckEpisodeEnd();
    }

    void Update()
    {
        if (Keyboard.current.rKey.wasPressedThisFrame)
        {   
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    public void Step(float deltaTime)
    {
        foreach (var road in allRoads)
        {
            road.Tick();
        }

        if (StatsManager.Instance != null)
        {
            StatsManager.Instance.CalculateStepPenalty(allRoads, deltaTime);
        }
    }
    private void CheckEpisodeEnd()
    {
        TrafficSpawner spawner = FindFirstObjectByType<TrafficSpawner>();

        if (spawner != null && StatsManager.Instance != null)
        {
            int totalExpected = spawner.totalCarsInScenario;
            int handledCars = StatsManager.Instance.carsFinished + StatsManager.Instance.carsDestroyedBeforeFinish;

            if (totalExpected > 0 && handledCars >= totalExpected)
            {
                Debug.Log($"[ML-Agents] Epizod zakoñczony! Zakoñczono aut: {handledCars}/{totalExpected}");

                IntersectionAgent[] agents = FindObjectsByType<IntersectionAgent>(FindObjectsSortMode.None);
                foreach (var agent in agents)
                {
                    agent.EndEpisode();
                }

                StatsManager.Instance.ResetMetrics();

                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
        }
    }

    private void CenterCameraOnMap()
    {
        if (Camera.main == null || mapGenerator == null || mapGenerator.AllIntersections.Count == 0)
            return;

        Vector3 centerPosition = Vector3.zero;

        foreach (var intersection in mapGenerator.AllIntersections)
        {
            centerPosition += intersection.transform.position;
        }

        centerPosition /= mapGenerator.AllIntersections.Count;

        Camera.main.transform.position = new Vector3(centerPosition.x, 250f, centerPosition.z);

        Camera.main.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        Debug.Log($"[SimManager] Kamera wyœrodkowana na pozycji: {Camera.main.transform.position}");
    }
}