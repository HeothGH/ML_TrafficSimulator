using System.Collections.Generic;
using UnityEngine;

public class GridMapGenerator : MonoBehaviour
{
    [Header("Wymiary")]
    public float roadLength = 50f;
    public float intersectionSize = 15f;

    [Header("Ustawienia Mapy")]
    public int width = 3;
    public int height = 3;
    public int seed = 12345; // todo

    [Header("Prefaby")]
    public Intersection intersectionPrefab;
    public GameObject roadPrefab;

    [Header("Kontener")]
    public Transform mapParent;

    public List<RoadSegment> AllRoadSegments { get; private set; } = new List<RoadSegment>();

    private Intersection[,] nodes;
    private Dictionary<Intersection, List<RoadSegment>> outRoadsFromNode = new Dictionary<Intersection, List<RoadSegment>>();
    private Dictionary<RoadSegment, Intersection> segmentDestination = new Dictionary<RoadSegment, Intersection>();

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
        outRoadsFromNode.Clear();
        segmentDestination.Clear();
        Random.InitState(seed);

        nodes = new Intersection[width, height];
        float gridStep = intersectionSize + roadLength;
        Debug.Log($"Generowanie mapy... Krok siatki: {gridStep}m");

        // Generowanie Skrzy¿owañ
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                Vector3 pos = new Vector3(x * gridStep, 0, z * gridStep);

                Intersection node = Instantiate(intersectionPrefab, pos, Quaternion.identity, mapParent);
                node.name = $"Intersection_{x}_{z}";
                nodes[x, z] = node;

                outRoadsFromNode[node] = new List<RoadSegment>();
            }
        }

        // Wstawianie Dróg
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                Intersection current = nodes[x, z];

                if (x < width - 1)
                {
                    Intersection right = nodes[x + 1, z];
                    SpawnRoadBetween(current, right);
                }

                if (z < height - 1)
                {
                    Intersection up = nodes[x, z + 1];
                    SpawnRoadBetween(current, up);
                }
            }
        }

        ConnectLogic();
    }

    private void SpawnRoadBetween(Intersection nodeA, Intersection nodeB)
    {
        Vector3 posA = nodeA.transform.position;
        Vector3 posB = nodeB.transform.position;

        Vector3 midPoint = (posA + posB) * 0.5f;

        Vector3 direction = (posB - posA).normalized;
        Quaternion rotation = Quaternion.LookRotation(direction);

        GameObject roadObj = Instantiate(roadPrefab, midPoint, rotation, mapParent);
        roadObj.name = $"Road_{nodeA.name}_to_{nodeB.name}"; // Profesjonalnie wygl¹daj¹ce ³adne nazwy

        RoadSegment[] segments = roadObj.GetComponentsInChildren<RoadSegment>();

        if (segments.Length == 0)
        {
            Debug.LogError($"B£¥D: Prefab '{roadObj.name}' nie ma komponentów RoadSegment w dzieciach!");
            return;
        }

        foreach (var seg in segments)
        {
            float angle = Vector3.Angle(seg.transform.forward, direction);

            if (angle < 90f)
            {
                // A -> B
                outRoadsFromNode[nodeA].Add(seg);
                segmentDestination[seg] = nodeB;
                seg.name = "Lane_Forward";
            }
            else
            {
                // B -> A
                outRoadsFromNode[nodeB].Add(seg);
                segmentDestination[seg] = nodeA;
                seg.name = "Lane_Backward";
            }

            AllRoadSegments.Add(seg);
        }
    }

    private void ConnectLogic()
    {
        foreach (var incomingRoad in AllRoadSegments)
        {
            if (!segmentDestination.ContainsKey(incomingRoad)) continue;

            Intersection targetInt = segmentDestination[incomingRoad];
            List<RoadSegment> exits = outRoadsFromNode[targetInt];

            incomingRoad.connectedRoads = new List<RoadSegment.Connection>();

            foreach (var exit in exits)
            {
                // Blokada zawracania
                if (segmentDestination.ContainsKey(exit))
                {
                    Intersection exitTarget = segmentDestination[exit];
                    Intersection incomingSource = GetSourceNode(incomingRoad);

                    if (exitTarget == incomingSource) continue; // To jest zawracanie
                }

                // Dodatkowe zabezpieczenie geometryczne przed zawracaniem (k¹t > 170 stopni)
                if (Vector3.Angle(incomingRoad.transform.forward, exit.transform.forward) > 170f)
                    continue;

                RoadSegment.Connection conn = new RoadSegment.Connection
                {
                    targetRoad = exit,
                    intersection = targetInt
                };
                incomingRoad.connectedRoads.Add(conn);
            }
        }
    }

    private Intersection GetSourceNode(RoadSegment road)
    {
        foreach (var kvp in outRoadsFromNode)
        {
            if (kvp.Value.Contains(road)) return kvp.Key;
        }
        return null;
    }
}