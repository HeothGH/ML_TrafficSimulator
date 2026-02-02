using UnityEngine;

public class StatsManager : MonoBehaviour
{
    // Singleton Instance
    public static StatsManager Instance { get; private set; }

    [Header("Metrics for ML")]
    public float totalTravelTime = 0f; // Suma czasu wszystkich aut (Reward = -totalTravelTime)
    public int carsFinished = 0;
    public int currentCarsOnMap = 0;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // Wywo³ywane, gdy auto siê spawnuje
    public void RegisterCarSpawn()
    {
        currentCarsOnMap++;
    }

    // Wywo³ywane, gdy auto dojedzie do celu
    public void RegisterCarFinish(float travelTime)
    {
        currentCarsOnMap--;
        carsFinished++;
        totalTravelTime += travelTime;

        // Logowanie dla Ciebie (debug)
        Debug.Log($"Auto ukonczylo trase w: {travelTime:F2}s. Total Score: {totalTravelTime}");
    }

    // Opcjonalne: Metoda Reset dla ML (nowy epizod treningowy)
    public void ResetMetrics()
    {
        totalTravelTime = 0f;
        carsFinished = 0;
        currentCarsOnMap = 0;
    }
}