using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class Intersection : MonoBehaviour
{
    public Transform centerPoint;
    private void Awake()
    {
        if (centerPoint == null) centerPoint = transform;
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
            // Wzór Beziera: B(t) = (1-t)^2 * P0 + 2(1-t)t * P1 + t^2 * P2
            Vector3 point =
                (1 - t) * (1 - t) * p0 +
                2 * (1 - t) * t * p1 +
                t * t * p2;
            path.Add(point);
        }

        return path;
    }

}
