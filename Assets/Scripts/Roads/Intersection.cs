using System.Collections.Generic;
using System.Linq; // Potrzebne do sortowania
using UnityEngine;

public class Intersection : MonoBehaviour
{
    public Transform centerPoint;

    [System.Serializable]
    public class TrafficPhase
    {
        public string phaseName;
        public List<RoadSegment> greenRoads = new List<RoadSegment>();

        public void SetState(bool isGreen)
        {
            foreach (var road in greenRoads)
            {
                if (road.trafficLight != null)
                {
                    road.trafficLight.SetState(isGreen ? 1 : 0);
                }
            }
        }
    }

    [Header("RL Configuration")]
    public List<TrafficPhase> phases = new List<TrafficPhase>();
    public int currentPhaseIndex = 0;

    private void Awake()
    {
        if (centerPoint == null) centerPoint = transform;
    }

    public void AutoConfigurePhases(List<RoadSegment> incomingRoads)
    {
        phases.Clear();

        if (incomingRoads.Count == 0) return;

        // Sortujemy drogi malej¹co po priorytecie (2 -> 1 -> 0)
        // Jeœli priorytety s¹ równe, decyduje nazwa/ID (dla determinizmu)
        var sortedRoads = incomingRoads
            .OrderByDescending(r => r.priority)
            .ThenBy(r => r.name)
            .ToList();

        // LOGIKA GRUPOWANIA:
        // Faza 0: Dwie najwa¿niejsze drogi (nawet jeœli to zakrêt L)
        // Faza 1: Wszystkie pozosta³e drogi

        TrafficPhase phaseMain = new TrafficPhase { phaseName = "Priority_Main" };
        TrafficPhase phaseSub = new TrafficPhase { phaseName = "Priority_Sub" };

        // Bierzemy max 2 pierwsze drogi do g³ównej fazy
        int mainCount = Mathf.Min(2, sortedRoads.Count);

        for (int i = 0; i < mainCount; i++)
        {
            phaseMain.greenRoads.Add(sortedRoads[i]);
        }

        // Reszta do podrzêdnej
        for (int i = mainCount; i < sortedRoads.Count; i++)
        {
            phaseSub.greenRoads.Add(sortedRoads[i]);
        }

        phases.Add(phaseMain);

        // Dodajemy drug¹ fazê tylko jeœli s¹ jakieœ drogi podrzêdne
        if (phaseSub.greenRoads.Count > 0)
        {
            phases.Add(phaseSub);
        }
        else
        {
            phases.Add(phaseSub);
        }

        SetIntersectionState(0);
    }

    public void SetIntersectionState(int phaseIndex)
    {
        if (phaseIndex < 0 || phaseIndex >= phases.Count) return;

        currentPhaseIndex = phaseIndex;

        for (int i = 0; i < phases.Count; i++)
        {
            bool isActive = (i == currentPhaseIndex);
            phases[i].SetState(isActive);
        }
    }

    public List<Vector3> GetPathThroughIntersection(Vector3 startPos, Vector3 endPos)
    {
        List<Vector3> path = new List<Vector3>();
        path.Add(startPos);

        int segments = 10;
        Vector3 p0 = startPos;
        Vector3 p1 = centerPoint.position;
        Vector3 p2 = endPos;

        for (int i = 1; i <= segments; i++)
        {
            float t = i / (float)segments;
            // Bezier quadratic
            Vector3 point =
                (1 - t) * (1 - t) * p0 +
                2 * (1 - t) * t * p1 +
                t * t * p2;
            path.Add(point);
        }
        return path;
    }
}