using UnityEngine;
using UnityEngine.InputSystem;

public class ManualTrafficInput : MonoBehaviour
{
    public TrafficLightController targetLight;

    void Update()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            int newState = targetLight.IsGreen ? 0 : 1;
            targetLight.SetState(newState);
        }
    }
}