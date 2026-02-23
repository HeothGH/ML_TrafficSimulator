using System.Collections.Generic;
using UnityEngine;

public class StatsManager : MonoBehaviour
{
    public static StatsManager Instance { get; private set; }

    [Header("Metrics for ML")]
    public float totalTravelTime = 0f;
    public int carsFinished = 0;
    public int currentCarsOnMap = 0;
    public int carsDestroyedBeforeFinish = 0;

    [Tooltip("Suma kar (Waiting Time * Multiplier) w obecnym kroku symulacji")]
    public float currentStepPenalty = 0f;

    [Tooltip("Suma kar od pocz¹tku epizodu")]
    public float accumulatedEpisodePenalty = 0f;

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

    public void RegisterCarWronglyDestroyed()
    {
        carsDestroyedBeforeFinish++;
        currentCarsOnMap--;
    }

    public void RegisterCarFinish(float travelTime)
    {
        currentCarsOnMap--;
        carsFinished++;
        totalTravelTime += travelTime;
    }

    // Wywo³ywane przez SimulationManager w ka¿dym Step()
    public void CalculateStepPenalty(List<RoadSegment> allRoads, float dt)
    {
        float stepSum = 0f;
        foreach (var road in allRoads)
        {
            stepSum += road.GetCalculatedPenalty(dt);
        }

        currentStepPenalty = stepSum;
        accumulatedEpisodePenalty += stepSum;

        // W logach ML-Agents tutaj wyœlesz AddReward(-currentStepPenalty);
    }

    public void ResetMetrics()
    {
        totalTravelTime = 0f;
        carsFinished = 0;
        currentCarsOnMap = 0;
        currentStepPenalty = 0f;
        accumulatedEpisodePenalty = 0f;
    }
}