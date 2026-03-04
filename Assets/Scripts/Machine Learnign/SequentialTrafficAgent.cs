using UnityEngine;

public class SequentialTrafficAgent : MonoBehaviour
{
    [Header("References")]
    public GridMapGenerator mapGenerator;

    [Header("Settings")]
    [Tooltip("Co ile sekund nastêpuje zmiana na kolejnym skrzy¿owaniu")]
    public float switchInterval = 0.5f;

    private float timer = 0f;
    private int currentIntersectionIndex = 0;

    void Start()
    {
        if (mapGenerator == null)
        {
            mapGenerator = FindFirstObjectByType<GridMapGenerator>();
        }
    }

    void Update()
    {
        if (mapGenerator == null || mapGenerator.AllIntersections.Count == 0) return;

        timer += Time.deltaTime;

        if (timer >= switchInterval)
        {
            timer = 0f;
            PerformStep();
        }
    }

    void PerformStep()
    {
        var allIntersections = mapGenerator.AllIntersections;

        Intersection targetIntersection = allIntersections[currentIntersectionIndex];

        int totalPhases = targetIntersection.phases.Count;

        if (totalPhases > 1)
        {
            int nextPhase = (targetIntersection.currentPhaseIndex + 1) % totalPhases;
            targetIntersection.SetIntersectionState(nextPhase);

        }

        currentIntersectionIndex++;

        if (currentIntersectionIndex >= allIntersections.Count)
        {
            currentIntersectionIndex = 0;
        }
    }
}