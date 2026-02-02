using UnityEngine;
using UnityEngine.InputSystem;

// Ten skrypt symuluje bycie "Agentem ML"
public class ManualTrafficInput : MonoBehaviour
{
    public TrafficLightController targetLight;

    void Update()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            // Przełączamy stan
            int newState = targetLight.IsGreen ? 0 : 1;
            targetLight.SetState(newState);
        }
    }
}