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
        // Jeœli nie przypisano generatora rêcznie, spróbuj znaleŸæ go na scenie
        if (mapGenerator == null)
        {
            mapGenerator = FindObjectOfType<GridMapGenerator>();
        }
    }

    void Update()
    {
        // Czekamy, a¿ mapa siê wygeneruje
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

        // Pobierz aktualnie obs³ugiwane skrzy¿owanie
        Intersection targetIntersection = allIntersections[currentIntersectionIndex];

        // Zmieñ fazê na nastêpn¹ (modulo iloœæ faz)
        int totalPhases = targetIntersection.phases.Count;

        // Zmieniamy œwiat³o tylko, jeœli skrzy¿owanie ma wiêcej ni¿ 1 fazê
        if (totalPhases > 1)
        {
            int nextPhase = (targetIntersection.currentPhaseIndex + 1) % totalPhases;
            targetIntersection.SetIntersectionState(nextPhase);

            // Opcjonalny debug, ¿ebyœ widzia³ w konsoli co siê dzieje
            // Debug.Log($"[Agent] Zmiana na {targetIntersection.name} -> Faza {nextPhase}");
        }

        // Przesuñ indeks na kolejne skrzy¿owanie
        currentIntersectionIndex++;

        // Jeœli doszliœmy do koñca listy, wracamy do pocz¹tku (Index 0)
        if (currentIntersectionIndex >= allIntersections.Count)
        {
            currentIntersectionIndex = 0;
        }
    }
}