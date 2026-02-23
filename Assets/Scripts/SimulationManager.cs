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

    [Header("References")]
    public GameObject carPrefab;
    public List<RoadSegment> allRoads = new List<RoadSegment>();
    public GridMapGenerator mapGenerator;

    // Licznik klatek fizycznych
    private int fixedFrameCount = 0;

    // Obliczona sta³a delta czasu dla logiki
    private float logicDeltaTime;

    void Awake()
    {
        // 1. Obliczamy sztywny czas trwania jednego kroku logicznego
        // Jeœli FixedUpdate jest co 0.02s, a interwa³ to 5, to logicDeltaTime = 0.1s
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
        }
    }

    void FixedUpdate()
    {
        fixedFrameCount++;

        // Wykonaj logikê tylko w co N-tej klatce fizyki
        if (fixedFrameCount % logicFrameInterval == 0)
        {
            // Przekazujemy STA£¥ wartoœæ czasu.
            // Dziêki temu Twoje obliczenia kar w StatsManager s¹ idealnie stabilne.
            Step(logicDeltaTime);
        }
    }

    void Update()
    {
        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    // Metoda Step mo¿e byæ wywo³ywana rêcznie przez Agenta ML (Academy)
    public void Step(float deltaTime)
    {
        // 1. Fizyka i logika aut
        foreach (var road in allRoads)
        {
            road.Tick();
        }

        // 2. Obliczenie kar (Rewards)
        if (StatsManager.Instance != null)
        {
            StatsManager.Instance.CalculateStepPenalty(allRoads, deltaTime);
        }
    }
}