using UnityEngine;

public class StatsManager : MonoBehaviour
{
    public static StatsManager Instance { get; private set; }

    [Header("Metrics for ML")]
    public float totalTravelTime = 0f;
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

    public void RegisterCarSpawn()
    {
        currentCarsOnMap++;
    }

    public void RegisterCarFinish(float travelTime)
    {
        currentCarsOnMap--;
        carsFinished++;
        totalTravelTime += travelTime;

        Debug.Log($"Auto ukonczylo trase w: {travelTime:F2}s. Total Score: {totalTravelTime}");
    }

    public void ResetMetrics()
    {
        totalTravelTime = 0f;
        carsFinished = 0;
        currentCarsOnMap = 0;
    }
}