using System.Collections.Generic;
using UnityEngine;

public class GridMapGenerator : MonoBehaviour
{
    [Header("Wymiary")]
    public float roadLength = 50f;
    public float intersectionSize = 15f;
    public int width = 5;
    public int height = 5;
    public int seed = 123;

    [Header("Snake Config")]
    [Range(0f, 1f)]
    public float snakeInertia = 0.7f; // Szansa na utrzymanie kierunku (0-1)

    [Header("Prefabs")]
    public Intersection intersectionPrefab;
    public GameObject roadPrefab;
    public Transform mapParent;

    // Publiczny dostêp do wygenerowanych dróg
    public List<RoadSegment> AllRoadSegments { get; private set; } = new List<RoadSegment>();
    public List<Intersection> AllIntersections { get; private set; } = new List<Intersection>();

    // Struktury pomocnicze do generowania
    private Intersection[,] intersectionGrid;

    // Dane o po³¹czeniach logicznych przed instancjacj¹
    // horizontalRoads[x, y] to droga na prawo od wêz³a (x,y)
    // verticalRoads[x, y] to droga w górê od wêz³a (x,y)
    private int[,] hRoadPriority; // -1 brak drogi, 0-2 priorytet
    private int[,] vRoadPriority;

    public void GenerateMap()
    {
        if (mapParent == null)
        {
            GameObject parentObj = new GameObject("GeneratedMap");
            mapParent = parentObj.transform;
        }

        // Czyszczenie
        foreach (Transform child in mapParent)
        {
            if (Application.isPlaying) Destroy(child.gameObject);
            else DestroyImmediate(child.gameObject);
        }
        AllRoadSegments.Clear();
        AllIntersections.Clear();

        Random.InitState(seed);

        // Inicjalizacja siatek danych
        hRoadPriority = new int[width, height]; // x od 0 do width-1 (ostatnia kolumna nie ma drogi w prawo)
        vRoadPriority = new int[width, height];

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                hRoadPriority[x, y] = -1;
                vRoadPriority[x, y] = -1;
            }

        // 1. Generowanie G³ównej Drogi (Snake)
        GenerateSnakeMainRoad();

        // 2. Wype³nianie reszty dróg (Priority 1 i 0)
        FillRemainingRoads();

        // 3. Instancjacja Obiektów
        InstantiateGrid();

        // 4. Logika Po³¹czeñ (Connect Logic)
        ConnectRoadsAndIntersections();

        Debug.Log("Mapa wygenerowana pomyœlnie.");
    }

    private void GenerateSnakeMainRoad()
    {
        // Losuj punkt startowy na krawêdzi
        int sx = 0, sy = 0;
        int edge = Random.Range(0, 4); // 0:Left, 1:Right, 2:Bottom, 3:Top

        if (edge == 0) { sx = 0; sy = Random.Range(0, height); }
        else if (edge == 1) { sx = width - 1; sy = Random.Range(0, height); }
        else if (edge == 2) { sx = Random.Range(0, width); sy = 0; }
        else { sx = Random.Range(0, width); sy = height - 1; }

        int cx = sx;
        int cy = sy;

        // Kierunek pocz¹tkowy - do œrodka mapy
        Vector2Int dir = Vector2Int.zero;
        if (edge == 0) dir = Vector2Int.right;
        else if (edge == 1) dir = Vector2Int.left;
        else if (edge == 2) dir = Vector2Int.up;
        else dir = Vector2Int.down;

        int safetyLimit = width * height * 2;
        int step = 0;

        while (step < safetyLimit)
        {
            // Zapisz drogê któr¹ w³aœnie "stawiamy" (jeœli nie wychodzimy poza mapê)
            // Jeœli idziemy w prawo: stawiamy hRoad w (cx, cy)
            // Jeœli idziemy w lewo: stawiamy hRoad w (cx-1, cy)
            // itd.

            // Oblicz nastêpny wêze³
            int nx = cx + dir.x;
            int ny = cy + dir.y;

            // SprawdŸ czy wyszliœmy z mapy (koniec wê¿a)
            if (nx < 0 || nx >= width || ny < 0 || ny >= height)
            {
                break; // W¹¿ dotar³ do krawêdzi
            }

            // Oznacz drogê miêdzy (cx,cy) a (nx,ny) jako Priority 2
            MarkRoadPriority(cx, cy, dir, 2);

            // Przesuñ siê
            cx = nx;
            cy = ny;

            // Decyzja o zmianie kierunku (Inercja)
            if (Random.value > snakeInertia)
            {
                // Zmieñ kierunek na prostopad³y
                if (dir.x != 0) // idziemy poziomo -> zmieñ na pion
                {
                    dir = (Random.value > 0.5f) ? Vector2Int.up : Vector2Int.down;
                }
                else // idziemy pionowo -> zmieñ na poziom
                {
                    dir = (Random.value > 0.5f) ? Vector2Int.right : Vector2Int.left;
                }
            }

            // SprawdŸ czy nowy kierunek nie wraca w to samo miejsce (backtracking) - uproszczone
            // W prostym Random Walk z inercj¹ pêtle s¹ mo¿liwe.

            step++;
        }
    }

    private void FillRemainingRoads()
    {
        // Iterujemy po wszystkich mo¿liwych po³¹czeniach
        // Jeœli droga nie jest ustawiona (-1), losujemy czy j¹ stworzyæ i jaki priorytet

        // Poziome
        for (int x = 0; x < width - 1; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (hRoadPriority[x, y] == -1) // Nie jest czêœci¹ wê¿a
                {
                    // Czy ta droga ³¹czy siê z wê¿em?
                    // Sprawdzamy czy wêze³ (x,y) lub (x+1,y) jest na trasie wê¿a.
                    // Dla uproszczenia: Losujemy Priority 1 (Wa¿na) lub 0 (Boczna)
                    // Mo¿na dodaæ szansê na brak drogi (dziury w mapie)

                    int p = (Random.value > 0.6f) ? 1 : 0;
                    hRoadPriority[x, y] = p;
                }
            }
        }

        // Pionowe
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height - 1; y++)
            {
                if (vRoadPriority[x, y] == -1)
                {
                    int p = (Random.value > 0.6f) ? 1 : 0;
                    vRoadPriority[x, y] = p;
                }
            }
        }
    }

    private void MarkRoadPriority(int x, int y, Vector2Int dir, int priority)
    {
        if (dir == Vector2Int.right)
        {
            if (x < width - 1) hRoadPriority[x, y] = priority;
        }
        else if (dir == Vector2Int.left)
        {
            if (x > 0) hRoadPriority[x - 1, y] = priority;
        }
        else if (dir == Vector2Int.up)
        {
            if (y < height - 1) vRoadPriority[x, y] = priority;
        }
        else if (dir == Vector2Int.down)
        {
            if (y > 0) vRoadPriority[x, y - 1] = priority;
        }
    }

    private void InstantiateGrid()
    {
        float gridStep = intersectionSize + roadLength;
        intersectionGrid = new Intersection[width, height];

        // 1. Skrzy¿owania
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                Vector3 pos = new Vector3(x * gridStep, 0, z * gridStep);
                Intersection node = Instantiate(intersectionPrefab, pos, Quaternion.identity, mapParent);
                node.name = $"Intersection_{x}_{z}";
                intersectionGrid[x, z] = node;
                AllIntersections.Add(node);
            }
        }

        // 2. Drogi
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                // Droga w prawo (Horizontal)
                if (x < width - 1)
                {
                    int p = hRoadPriority[x, z];
                    if (p != -1) // Jeœli droga istnieje
                    {
                        SpawnRoad(intersectionGrid[x, z], intersectionGrid[x + 1, z], p);
                    }
                }

                // Droga w górê (Vertical)
                if (z < height - 1)
                {
                    int p = vRoadPriority[x, z];
                    if (p != -1)
                    {
                        SpawnRoad(intersectionGrid[x, z], intersectionGrid[x, z + 1], p);
                    }
                }
            }
        }
    }

    private void SpawnRoad(Intersection nodeA, Intersection nodeB, int priority)
    {
        Vector3 posA = nodeA.transform.position;
        Vector3 posB = nodeB.transform.position;
        Vector3 midPoint = (posA + posB) * 0.5f;
        Vector3 direction = (posB - posA).normalized;
        Quaternion rotation = Quaternion.LookRotation(direction);

        GameObject roadObj = Instantiate(roadPrefab, midPoint, rotation, mapParent);
        roadObj.name = $"Road_P{priority}_{nodeA.name}-{nodeB.name}";

        RoadSegment[] segments = roadObj.GetComponentsInChildren<RoadSegment>();

        foreach (var seg in segments)
        {
            seg.priority = priority;
            seg.SetupPriorityVisuals(); // W³¹czamy wizualizacjê
            AllRoadSegments.Add(seg);
        }
    }

    private void ConnectRoadsAndIntersections()
    {
        // Budujemy mapy pomocnicze do szybkiego wyszukiwania
        // Dictionary: Segment -> Do którego skrzy¿owania zmierza
        Dictionary<RoadSegment, Intersection> segmentDestination = new Dictionary<RoadSegment, Intersection>();
        // Dictionary: Skrzy¿owanie -> Lista segmentów wychodz¹cych z niego
        Dictionary<Intersection, List<RoadSegment>> outRoadsFromNode = new Dictionary<Intersection, List<RoadSegment>>();
        // Dictionary: Skrzy¿owanie -> Lista segmentów wchodz¹cych do niego (do konfiguracji œwiate³)
        Dictionary<Intersection, List<RoadSegment>> incomingRoadsToNode = new Dictionary<Intersection, List<RoadSegment>>();

        foreach (var node in AllIntersections)
        {
            outRoadsFromNode[node] = new List<RoadSegment>();
            incomingRoadsToNode[node] = new List<RoadSegment>();
        }

        // Analiza geometryczna ka¿dego segmentu
        foreach (var seg in AllRoadSegments)
        {
            // ZnajdŸ najbli¿sze skrzy¿owania
            Intersection bestDest = null;
            Intersection bestSource = null;
            float minDestDist = float.MaxValue;
            float minSourceDist = float.MaxValue;

            // Pocz¹tek i koniec segmentu w œwiecie
            Vector3 segForward = seg.transform.forward;
            // Dla pewnoœci pobieramy pozycjê
            Vector3 segCenter = seg.transform.position;
            // Szukamy skrzy¿owania przed "dziobem" (Destination) i za "ogonem" (Source)

            foreach (var node in AllIntersections)
            {
                Vector3 vecToNode = node.transform.position - segCenter;
                float dist = vecToNode.magnitude;
                float angle = Vector3.Angle(segForward, vecToNode);

                // Destination: node jest "przed" autem (k¹t < 90) i blisko
                if (angle < 45f) // zawê¿amy k¹t
                {
                    if (dist < minDestDist) { minDestDist = dist; bestDest = node; }
                }
                // Source: node jest "za" autem (k¹t > 135)
                else if (angle > 135f)
                {
                    if (dist < minSourceDist) { minSourceDist = dist; bestSource = node; }
                }
            }

            if (bestDest != null)
            {
                segmentDestination[seg] = bestDest;
                incomingRoadsToNode[bestDest].Add(seg);
            }
            if (bestSource != null)
            {
                outRoadsFromNode[bestSource].Add(seg);
            }
        }

        // 1. £¹czenie logiczne (Next Road logic)
        foreach (var incoming in AllRoadSegments)
        {
            if (!segmentDestination.ContainsKey(incoming)) continue;

            Intersection targetInt = segmentDestination[incoming];
            List<RoadSegment> exits = outRoadsFromNode[targetInt];

            incoming.connectedRoads = new List<RoadSegment.Connection>();

            foreach (var exit in exits)
            {
                // Blokada zawracania (U-turn)
                // Sprawdzamy czy exit prowadzi z powrotem do source incoming
                // Ale nie mamy source w zmiennej. 
                // U¿yjmy k¹ta: jeœli exit jest skierowany przeciwnie do incoming (> 170 deg ró¿nicy forwardów)
                if (Vector3.Angle(incoming.transform.forward, exit.transform.forward) > 170f)
                    continue;

                RoadSegment.Connection conn = new RoadSegment.Connection
                {
                    targetRoad = exit,
                    intersection = targetInt
                };
                incoming.connectedRoads.Add(conn);
            }
        }

        // 2. Konfiguracja Œwiate³ na Skrzy¿owaniach (Grouping)
        foreach (var node in AllIntersections)
        {
            node.AutoConfigurePhases(incomingRoadsToNode[node]);
        }
    }
}