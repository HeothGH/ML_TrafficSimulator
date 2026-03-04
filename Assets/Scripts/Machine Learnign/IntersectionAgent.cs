using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Intersection))]
public class IntersectionAgent : Agent
{
    private Intersection intersection;

    [Header("Agent Settings")]
    [Tooltip("Minimalny czas (w sekundach) miêdzy zmianami œwiate³")]
    public float decisionInterval = 5.0f;

    private float cooldownTimer = 0f;

    public override void Initialize()
    {
        intersection = GetComponent<Intersection>();
    }

    public void FixedUpdate()
    {
        if (intersection == null || intersection.phases == null) return;

        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.fixedDeltaTime;
        }

        float localStepPenalty = 0f;
        foreach (var phase in intersection.phases)
        {
            if (phase != null && phase.greenRoads != null)
            {
                foreach (var road in phase.greenRoads)
                {
                    if (road != null)
                    {
                        localStepPenalty += road.GetCalculatedPenalty(Time.fixedDeltaTime);
                    }
                }
            }
        }

        if (localStepPenalty > 0f)
        {
            AddReward(-localStepPenalty);
        }

    }

    public override void CollectObservations(VectorSensor sensor)
    {
        if (intersection == null || intersection.phases == null) return;

        List<RoadSegment> currentIncomingRoads = new List<RoadSegment>();
        foreach (var phase in intersection.phases)
        {
            if (phase != null && phase.greenRoads != null)
            {
                currentIncomingRoads.AddRange(phase.greenRoads);
            }
        }

        int maxRoads = 4;
        for (int i = 0; i < maxRoads; i++)
        {
            if (i < currentIncomingRoads.Count && currentIncomingRoads[i] != null)
            {
                RoadSegment road = currentIncomingRoads[i];
                sensor.AddObservation(GetCarCountOnRoad(road) / 20f);
                sensor.AddObservation((float)road.priority / 2f);
            }
            else
            {
                sensor.AddObservation(0f);
                sensor.AddObservation(0f);
            }
        }

        int phaseCount = Mathf.Max(1, intersection.phases.Count);
        sensor.AddObservation((float)intersection.currentPhaseIndex / phaseCount);

        sensor.AddObservation(cooldownTimer > 0f ? 1f : 0f);
    }

    private int GetCarCountOnRoad(RoadSegment road)
    {
        if (road.slots == null) return 0;
        int count = 0;
        for (int i = 0; i < road.capacity; i++)
        {
            if (road.slots[i] != null) count++;
        }
        return count;
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        if (intersection == null || intersection.phases.Count <= 1) return;

        int actionTaken = actions.DiscreteActions[0];

        if (actionTaken == 1 && cooldownTimer <= 0f)
        {
            int totalPhases = intersection.phases.Count;
            int nextPhase = (intersection.currentPhaseIndex + 1) % totalPhases;
            intersection.SetIntersectionState(nextPhase);

            cooldownTimer = decisionInterval;
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        discreteActionsOut[0] = 0; 

        if (Keyboard.current != null && Keyboard.current.spaceKey.isPressed)
        {
            discreteActionsOut[0] = 1;
        }
    }
}