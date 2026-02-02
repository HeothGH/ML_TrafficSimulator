using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class RoadSegment : MonoBehaviour
{
    [Header("Settings")]
    public float carLengthWithGap = 6.0f; // 4.5m auto + 1.5m odstêpu
    public TrafficLightController trafficLight; // Opcjonalne

    [Header("Debug Info")]
    public int capacity;
    public List<CarAgent> slots; // Lista aut (0 = przód, capacity-1 = ty³)

    // S¹siedzi - graf dróg. Kluczowe dla routingu.
    // Mo¿emy automatycznie wykrywaæ lub przypisywaæ rêcznie.
    public List<RoadSegment> connectedRoads = new List<RoadSegment>();

    private BoxCollider roadCollider;
    private float segmentLength;

    void Awake()
    {
        roadCollider = GetComponent<BoxCollider>();

        // 1. Obliczanie pojemnoœci na podstawie d³ugoœci Collidera
        // Bounds.size.z zwraca rozmiar w œwiecie (uwzglêdnia skalê)
        segmentLength = roadCollider.bounds.size.z;

        // Zabezpieczenie przed dzieleniem przez zero
        if (carLengthWithGap <= 0) carLengthWithGap = 6.0f;

        capacity = Mathf.FloorToInt(segmentLength / carLengthWithGap);
        if (capacity < 1) capacity = 1;

        // Inicjalizacja slotów pustymi wartoœciami
        slots = new List<CarAgent>(new CarAgent[capacity]);
    }

    public bool CanEnter()
    {
        // Sprawdzamy czy ostatni slot (wjazd) jest pusty
        return slots[capacity - 1] == null;
    }

    public void EnterRoad(CarAgent car)
    {
        if (CanEnter())
        {
            int entryIndex = capacity - 1;
            slots[entryIndex] = car;
            car.currentRoad = this;
            car.currentSlotIndex = entryIndex;

            // Zaktualizuj wizualny cel auta
            car.UpdateVisualTarget(GetWorldPositionOfSlot(entryIndex));
        }
        else
        {
            Debug.LogError($"Auto próbowa³o wjechaæ na pe³n¹ drogê: {name}");
        }
    }

    // G³ówna pêtla logiki (wywo³ywana przez Managera)
    public void Tick()
    {
        // WA¯NE: Iterujemy od 0 (przód) do ty³u.
        // Dziêki temu auto z 0 ucieka, 1 wchodzi na 0, 2 wchodzi na 1 itd. w jednej klatce.
        for (int i = 0; i < capacity; i++)
        {
            CarAgent car = slots[i];
            if (car == null) continue;

            // --- Przypadek 1: Auto jest na koñcu drogi (Slot 0) ---
            if (i == 0)
            {
                // SprawdŸ œwiat³a
                bool isGreen = (trafficLight == null || trafficLight.IsGreen);

                if (isGreen)
                {
                    // SprawdŸ gdzie auto chce jechaæ
                    RoadSegment nextRoad = car.GetNextRoadFromRoute();

                    if (nextRoad != null)
                    {
                        // Czy nastêpna droga fizycznie ³¹czy siê z t¹? (Walidacja)
                        if (connectedRoads.Contains(nextRoad))
                        {
                            // Czy jest miejsce na nastêpnej drodze?
                            if (nextRoad.CanEnter())
                            {
                                // PRZEPROWADZKA
                                slots[i] = null; // Usuñ st¹d
                                nextRoad.EnterRoad(car); // Dodaj tam
                                car.PopRoute(); // Zaliczony odcinek
                            }
                            // Else: Czekamy na miejsce (korek za skrzy¿owaniem)
                        }
                        else
                        {
                            // Auto chce jechaæ drog¹, która nie jest po³¹czona! 
                            // Tutaj mo¿na dodaæ logikê "rerouting" albo b³¹d.
                            Debug.LogWarning($"Auto {car.name} chce teleportowaæ siê do niepo³¹czonej drogi!");
                        }
                    }
                    else
                    {
                        // Koniec trasy - usuwamy auto z symulacji (lub respawn)
                        slots[i] = null;
                        Destroy(car.gameObject);
                    }
                }
            }
            // --- Przypadek 2: Ruch wewn¹trz drogi ---
            else
            {
                // Jeœli slot przed nami (i-1) jest pusty, podje¿d¿amy
                if (slots[i - 1] == null)
                {
                    slots[i - 1] = car;
                    slots[i] = null;
                    car.currentSlotIndex = i - 1;

                    // Aktualizacja wizualna
                    car.UpdateVisualTarget(GetWorldPositionOfSlot(i - 1));
                }
            }
        }
    }

    // Zamienia index slotu na pozycjê w œwiecie 3D
    public Vector3 GetWorldPositionOfSlot(int slotIndex)
    {
        // Slot 0 = Przód (Forward), Slot Max = Ty³ (-Forward)
        // Musimy zmapowaæ index na pozycjê w obrêbie BoxCollidera

        float stepSize = segmentLength / capacity;

        // Œrodek pierwszego slotu (tego przy œwiat³ach)
        // Collider Center + (Forward * Extents) - (Half Step)
        Vector3 roadEnd = transform.position + (transform.forward * (segmentLength * 0.5f));

        // Cofamy siê o (index * rozmiar slotu) i jeszcze o pó³ slotu, ¿eby byæ w œrodku
        float offset = (slotIndex * stepSize) + (stepSize * 0.5f);

        return roadEnd - (transform.forward * offset);
    }

    // Helper do rysowania slotów w edytorze
    void OnDrawGizmosSelected()
    {
        if (roadCollider == null) roadCollider = GetComponent<BoxCollider>();
        Gizmos.color = Color.cyan;
        // Rysuj proste kropki gdzie s¹ sloty
        if (capacity > 0)
        {
            for (int i = 0; i < capacity; i++)
            {
                // U¿ywamy przybli¿onej logiki, bo Awake mog³o nie pójœæ w edytorze
                float len = transform.lossyScale.z; // Zak³adamy cube
                float step = len / capacity;
                Vector3 start = transform.position + transform.forward * (len * 0.5f);
                Gizmos.DrawWireSphere(start - transform.forward * (i * step + step * 0.5f), 0.5f);
            }
        }
    }
}