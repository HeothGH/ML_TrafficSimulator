using UnityEngine;

public class TrafficLightController : MonoBehaviour
{
    public bool IsGreen { get; private set; } = true;
    private Renderer cubeRenderer;

    void Awake()
    {
        cubeRenderer = GetComponent<Renderer>();
        UpdateVisuals();
    }

    // TA metoda jest interfejsem dla ML oraz dla ManualInput
    public void SetState(int state) // 0 = Red, 1 = Green
    {
        IsGreen = (state == 1);
        UpdateVisuals();
    }

    void UpdateVisuals()
    {
        cubeRenderer.material.color = IsGreen ? Color.green : Color.red;
    }
}