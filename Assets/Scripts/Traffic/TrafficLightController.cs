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

    public void SetState(int state)
    {
        IsGreen = (state == 1);
        UpdateVisuals();
    }

    void UpdateVisuals()
    {
        cubeRenderer.material.color = IsGreen ? Color.green : Color.red;
    }
}