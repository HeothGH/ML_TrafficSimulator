using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class SimulationManager : MonoBehaviour
{
    [Header("Config")]
    public float tickRate = 0.1f; // Zmniejszy³em domyœlnie dla p³ynniejszego debugowania
    public int simulationSeed = 12345;

    [Header("References")]
    public GameObject carPrefab;
    public List<RoadSegment> allRoads = new List<RoadSegment>();
    public GridMapGenerator mapGenerator;

    private float timer;

    void Awake()
    {
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

    void Update()
    {
        // Tryb ci¹g³y (dla testów wizualnych)
        timer += Time.deltaTime;
        if (timer >= tickRate)
        {
            Step(tickRate); // Przekazujemy deltê
            timer = 0;
        }

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