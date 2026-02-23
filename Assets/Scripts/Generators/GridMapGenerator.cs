using System.Collections.Generic;
using UnityEngine;

public class GridMapGenerator : MonoBehaviour
{
    public enum GenerationMode
    {
        Snake,
        MiddleStraight
    }

    [Header("Ustawienia Generalne")]
    public GenerationMode generationMode = GenerationMode.MiddleStraight;
    public int width = 10;
    public int height = 10;
    public int seed = 123;

    [Header("Wymiary Obiektów")]
    public float roadLength = 50f;
    public float intersectionSize = 15f;

    [Header("Konfiguracja: Middle Straight")]
    [Range(0f, 1f)]
    public float secondaryRoadProbability = 0.3f;
    [Range(0f, 1f)]
    public float fillGapsProbability = 0.5f;

    [Header("Konfiguracja: Snake")]
    [Range(0f, 1f)]
    public float snakeInertia = 0.7f;

    [Header("Prefabs")]
    public Intersection intersectionPrefab;
    public GameObject roadPrefab;
    public Transform mapParent;

    public List<RoadSegment> AllRoadSegments { get; private set; } = new List<RoadSegment>();
    public List<Intersection> AllIntersections { get; private set; } = new List<Intersection>();

    private Intersection[,] intersectionGrid;
    private int[,] hRoadPriority;
    private int[,] vRoadPriority;

    public void GenerateMap()
    {
        if (mapParent == null)
        {
            GameObject parentObj = new GameObject("GeneratedMap");
            mapParent = parentObj.transform;
        }

        foreach (Transform child in mapParent)
        {
            if (Application.isPlaying) Destroy(child.gameObject);
            else DestroyImmediate(child.gameObject);
        }
        AllRoadSegments.Clear();
        AllIntersections.Clear();

        Random.InitState(seed);

        hRoadPriority = new int[width, height];
        vRoadPriority = new int[width, height];

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                hRoadPriority[x, y] = -1;
                vRoadPriority[x, y] = -1;
            }

        // 1. Generowanie Logiki Dróg
        if (generationMode == GenerationMode.Snake)
        {
            GenerateSnakeMainRoad();
            FillRemainingRoads(randomHighPriority: true, fillChance: 0.6f);
        }
        else if (generationMode == GenerationMode.MiddleStraight)
        {
            GenerateMiddleStraightStrategy();
            FillRemainingRoads(randomHighPriority: false, fillChance: fillGapsProbability);
        }

        // --- Usuwanie niepo³¹czonych fragmentów ---
        PruneDisconnectedRoads();

        // 2. Instancjacja
        InstantiateGrid();

        // 3. Logika Po³¹czeñ
        ConnectRoadsAndIntersections();

        Debug.Log($"Mapa wygenerowana. Tryb: {generationMode}, Seed: {seed}");
    }

    // --- ALGORYTM CZYSZCZENIA WYSP (BFS) ---
    private void PruneDisconnectedRoads()
    {
        bool[,] visited = new bool[width, height];
        Queue<Vector2Int> queue = new Queue<Vector2Int>();

        // 1. ZnajdŸ punkt startowy na drodze g³ównej (Priority 2)
        // Szukamy dowolnego wêz³a, który jest czêœci¹ g³ównej drogi
        bool startFound = false;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // Sprawdzamy czy z tego wêz³a wychodzi jakakolwiek droga priorytetu 2
                bool connectedToMain = false;
                if (x < width - 1 && hRoadPriority[x, y] == 2) connectedToMain = true;
                if (x > 0 && hRoadPriority[x - 1, y] == 2) connectedToMain = true;
                if (y < height - 1 && vRoadPriority[x, y] == 2) connectedToMain = true;
                if (y > 0 && vRoadPriority[x, y - 1] == 2) connectedToMain = true;

                if (connectedToMain)
                {
                    queue.Enqueue(new Vector2Int(x, y));
                    visited[x, y] = true;
                    startFound = true;
                    break;
                }
            }
            if (startFound) break;
        }

        if (!startFound) return; // Jeœli nie ma g³ównej drogi, nic nie robimy (rzadki przypadek)

        // 2. Przejœcie BFS po wszystkich po³¹czonych drogach
        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            int cx = current.x;
            int cy = current.y;

            // SprawdŸ s¹siada z prawej (jeœli jest droga)
            if (cx < width - 1 && hRoadPriority[cx, cy] != -1)
            {
                if (!visited[cx + 1, cy])
                {
                    visited[cx + 1, cy] = true;
                    queue.Enqueue(new Vector2Int(cx + 1, cy));
                }
            }
            // SprawdŸ s¹siada z lewej (jeœli jest droga wchodz¹ca do nas z lewej)
            if (cx > 0 && hRoadPriority[cx - 1, cy] != -1)
            {
                if (!visited[cx - 1, cy])
                {
                    visited[cx - 1, cy] = true;
                    queue.Enqueue(new Vector2Int(cx - 1, cy));
                }
            }
            // SprawdŸ s¹siada z góry
            if (cy < height - 1 && vRoadPriority[cx, cy] != -1)
            {
                if (!visited[cx, cy + 1])
                {
                    visited[cx, cy + 1] = true;
                    queue.Enqueue(new Vector2Int(cx, cy + 1));
                }
            }
            // SprawdŸ s¹siada z do³u
            if (cy > 0 && vRoadPriority[cx, cy - 1] != -1)
            {
                if (!visited[cx, cy - 1])
                {
                    visited[cx, cy - 1] = true;
                    queue.Enqueue(new Vector2Int(cx, cy - 1));
                }
            }
        }

        // 3. Usuwanie dróg, które prowadz¹ do nieodwiedzonych wêz³ów lub s¹ poza grafem
        // Jeœli droga ³¹czy wêze³ odwiedzony z nieodwiedzonym (niemo¿liwe w BFS grafu nieskierowanego, 
        // ale sprawdzamy czy OBA koñce s¹ w visited lub czy droga w ogóle jest dostêpna)

        // Czyœcimy H Roads
        for (int x = 0; x < width - 1; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // Jeœli wêze³ startowy drogi nie zosta³ odwiedzony, to droga jest odciêta
                if (!visited[x, y])
                {
                    hRoadPriority[x, y] = -1;
                }
            }
        }

        // Czyœcimy V Roads
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height - 1; y++)
            {
                if (!visited[x, y])
                {
                    vRoadPriority[x, y] = -1;
                }
            }
        }
    }

    private void GenerateMiddleStraightStrategy()
    {
        bool isHorizontalMain = width >= height;

        if (isHorizontalMain)
        {
            int midY = height / 2;
            for (int x = 0; x < width - 1; x++) hRoadPriority[x, midY] = 2;

            for (int x = 0; x < width; x++)
            {
                if (Random.value < secondaryRoadProbability)
                {
                    for (int y = 0; y < height - 1; y++) vRoadPriority[x, y] = 1;
                }
            }
        }
        else
        {
            int midX = width / 2;
            for (int y = 0; y < height - 1; y++) vRoadPriority[midX, y] = 2;

            for (int y = 0; y < height; y++)
            {
                if (Random.value < secondaryRoadProbability)
                {
                    for (int x = 0; x < width - 1; x++) hRoadPriority[x, y] = 1;
                }
            }
        }
    }

    private void GenerateSnakeMainRoad()
    {
        int sx = 0, sy = 0;
        int edge = Random.Range(0, 4);

        if (edge == 0) { sx = 0; sy = Random.Range(0, height); }
        else if (edge == 1) { sx = width - 1; sy = Random.Range(0, height); }
        else if (edge == 2) { sx = Random.Range(0, width); sy = 0; }
        else { sx = Random.Range(0, width); sy = height - 1; }

        int cx = sx;
        int cy = sy;

        Vector2Int dir = Vector2Int.zero;
        if (edge == 0) dir = Vector2Int.right;
        else if (edge == 1) dir = Vector2Int.left;
        else if (edge == 2) dir = Vector2Int.up;
        else dir = Vector2Int.down;

        int safetyLimit = width * height * 2;
        int step = 0;

        while (step < safetyLimit)
        {
            int nx = cx + dir.x;
            int ny = cy + dir.y;

            if (nx < 0 || nx >= width || ny < 0 || ny >= height) break;

            MarkRoadPriority(cx, cy, dir, 2);

            cx = nx;
            cy = ny;

            if (Random.value > snakeInertia)
            {
                if (dir.x != 0) dir = (Random.value > 0.5f) ? Vector2Int.up : Vector2Int.down;
                else dir = (Random.value > 0.5f) ? Vector2Int.right : Vector2Int.left;
            }
            step++;
        }
    }

    private void FillRemainingRoads(bool randomHighPriority, float fillChance)
    {
        for (int x = 0; x < width - 1; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (hRoadPriority[x, y] == -1 && Random.value < fillChance)
                {
                    int p = (!randomHighPriority) ? 0 : (Random.value > 0.6f ? 1 : 0);
                    hRoadPriority[x, y] = p;
                }
            }
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height - 1; y++)
            {
                if (vRoadPriority[x, y] == -1 && Random.value < fillChance)
                {
                    int p = (!randomHighPriority) ? 0 : (Random.value > 0.6f ? 1 : 0);
                    vRoadPriority[x, y] = p;
                }
            }
        }
    }

    private void MarkRoadPriority(int x, int y, Vector2Int dir, int priority)
    {
        if (dir == Vector2Int.right) { if (x < width - 1) hRoadPriority[x, y] = priority; }
        else if (dir == Vector2Int.left) { if (x > 0) hRoadPriority[x - 1, y] = priority; }
        else if (dir == Vector2Int.up) { if (y < height - 1) vRoadPriority[x, y] = priority; }
        else if (dir == Vector2Int.down) { if (y > 0) vRoadPriority[x, y - 1] = priority; }
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
                // Zliczamy liczbê po³¹czeñ
                int connectionsCount = 0;

                // Sprawdzamy 4 kierunki
                if (x < width - 1 && hRoadPriority[x, z] != -1) connectionsCount++;
                if (x > 0 && hRoadPriority[x - 1, z] != -1) connectionsCount++;
                if (z < height - 1 && vRoadPriority[x, z] != -1) connectionsCount++;
                if (z > 0 && vRoadPriority[x, z - 1] != -1) connectionsCount++;

                // LOGIKA: Tworzymy prefab skrzy¿owania TYLKO jeœli ma wiêcej ni¿ 1 drogê.
                // Jeœli ma 1 drogê (œlepa uliczka) lub 0 (b³¹d), nie stawiamy obiektu Intersection.
                if (connectionsCount > 1)
                {
                    Vector3 pos = new Vector3(x * gridStep, 0, z * gridStep);
                    Intersection node = Instantiate(intersectionPrefab, pos, Quaternion.identity, mapParent);
                    node.name = $"Intersection_{x}_{z}";
                    intersectionGrid[x, z] = node;
                    AllIntersections.Add(node);
                }
                else
                {
                    // Œlepa uliczka - brak prefabu skrzy¿owania
                    intersectionGrid[x, z] = null;
                }
            }
        }

        // 2. Drogi
        // UWAGA: Musimy zmieniæ sposób spawnowania, bo intersectionGrid[x,z] mo¿e byæ teraz null!
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                Vector3 nodePosA = new Vector3(x * gridStep, 0, z * gridStep);

                // Horizontal
                if (x < width - 1)
                {
                    int p = hRoadPriority[x, z];
                    if (p != -1)
                    {
                        Vector3 nodePosB = new Vector3((x + 1) * gridStep, 0, z * gridStep);
                        SpawnRoad(nodePosA, nodePosB, intersectionGrid[x, z], intersectionGrid[x + 1, z], p);
                    }
                }

                // Vertical
                if (z < height - 1)
                {
                    int p = vRoadPriority[x, z];
                    if (p != -1)
                    {
                        Vector3 nodePosB = new Vector3(x * gridStep, 0, (z + 1) * gridStep);
                        SpawnRoad(nodePosA, nodePosB, intersectionGrid[x, z], intersectionGrid[x, z + 1], p);
                    }
                }
            }
        }
    }

    private void SpawnRoad(Vector3 posA, Vector3 posB, Intersection nodeA, Intersection nodeB, int priority)
    {
        Vector3 midPoint = (posA + posB) * 0.5f;
        Vector3 direction = (posB - posA).normalized;
        Quaternion rotation = Quaternion.LookRotation(direction);

        GameObject roadObj = Instantiate(roadPrefab, midPoint, rotation, mapParent);

        // Bezpieczne nazewnictwo (gdy nodeA/B jest null)
        string nameA = nodeA != null ? nodeA.name : "DeadEnd";
        string nameB = nodeB != null ? nodeB.name : "DeadEnd";
        roadObj.name = $"Road_P{priority}_{nameA}-{nameB}";

        RoadSegment[] segments = roadObj.GetComponentsInChildren<RoadSegment>();

        foreach (var seg in segments)
        {
            seg.priority = priority;
            seg.SetupPriorityVisuals();
            AllRoadSegments.Add(seg);
        }
    }

    private void ConnectRoadsAndIntersections()
    {
        Dictionary<RoadSegment, Intersection> segmentDestination = new Dictionary<RoadSegment, Intersection>();
        Dictionary<Intersection, List<RoadSegment>> outRoadsFromNode = new Dictionary<Intersection, List<RoadSegment>>();
        Dictionary<Intersection, List<RoadSegment>> incomingRoadsToNode = new Dictionary<Intersection, List<RoadSegment>>();

        foreach (var node in AllIntersections)
        {
            outRoadsFromNode[node] = new List<RoadSegment>();
            incomingRoadsToNode[node] = new List<RoadSegment>();
        }

        foreach (var seg in AllRoadSegments)
        {
            Intersection bestDest = null;
            Intersection bestSource = null;
            float minDestDist = float.MaxValue;
            float minSourceDist = float.MaxValue;

            Vector3 segForward = seg.transform.forward;
            Vector3 segCenter = seg.transform.position;

            foreach (var node in AllIntersections)
            {
                Vector3 vecToNode = node.transform.position - segCenter;
                float dist = vecToNode.magnitude;
                float angle = Vector3.Angle(segForward, vecToNode);

                if (angle < 45f)
                {
                    if (dist < minDestDist) { minDestDist = dist; bestDest = node; }
                }
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
            else
            {
                // CASUS 1: Droga prowadzi do nik¹d (bo usunêliœmy prefab skrzy¿owania - œlepa uliczka)
                // Usuwamy œwiat³a, bo nie ma skrzy¿owania = nie ma kolizji.
                seg.RemoveTrafficLight();
            }

            if (bestSource != null)
            {
                outRoadsFromNode[bestSource].Add(seg);
            }
        }

        // £¹czenie logiczne
        foreach (var incoming in AllRoadSegments)
        {
            if (!segmentDestination.ContainsKey(incoming)) continue;

            Intersection targetInt = segmentDestination[incoming];
            List<RoadSegment> exits = outRoadsFromNode[targetInt];

            incoming.connectedRoads = new List<RoadSegment.Connection>();

            foreach (var exit in exits)
            {
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

        // Konfiguracja Œwiate³ i Fazy
        foreach (var node in AllIntersections)
        {
            var incoming = incomingRoadsToNode[node];

            // CASUS 2: Skrzy¿owanie istnieje, ale ma ma³o dróg wlotowych.
            // Jeœli dróg wchodz¹cych jest 1 lub 2 (np. zakrêt L lub prosta droga),
            // to nie ma sensu ich blokowaæ œwiat³ami.
            if (incoming.Count <= 2)
            {
                foreach (var road in incoming)
                {
                    road.RemoveTrafficLight();
                }
                // Opcjonalnie: nie konfigurujemy faz, bo nie ma œwiate³
            }
            else
            {
                // Standardowe skrzy¿owanie (T lub X) - konfigurujemy fazy
                node.AutoConfigurePhases(incoming);
            }
        }
    }
}