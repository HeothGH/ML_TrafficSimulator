using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.Rendering.HableCurve;

public class CameraController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 20f;
    public float sprintMultiplier = 2.0f;

    [Header("Zoom Settings")]
    public float scrollSensitivity = 0.5f;
    public float minHeight = 5f;
    public float maxHeight = 1000f;

    [Header("Interaction")]
    public LayerMask carLayer;

    [Header("Visuals (Selection Ring)")]
    public float ringRadius = 2f;
    public float lineWidth = 0.5f;
    public Color ringColor = Color.white;
    private int segments = 64;

    private InputAction moveAction;
    private InputAction sprintAction;
    private InputAction zoomAction;
    private InputAction clickAction;
    private InputAction mousePosAction;

    private Transform targetToFollow;
    private Camera cam;

    private GameObject selectionRingObject;
    private LineRenderer ringLineRenderer;
    void Awake()
    {
        cam = GetComponent<Camera>();
        SetupInputActions();
    }

    void OnEnable()
    {
        moveAction.Enable();
        sprintAction.Enable();
        zoomAction.Enable();
        clickAction.Enable();
        mousePosAction.Enable();
    }

    void OnDisable()
    {
        moveAction.Disable();
        sprintAction.Disable();
        zoomAction.Disable();
        clickAction.Disable();
        mousePosAction.Disable();
    }

    void Start()
    {
        CreateSelectionRing();
    }

    void Update()
    {
        HandleSelection();
        HandleMovementAndFollow();
        HandleZoom();
        UpdateSelectionRing();
    }

    private void SetupInputActions()
    {
        moveAction = new InputAction("Move", binding: "<Gamepad>/leftStick");
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");

        sprintAction = new InputAction("Sprint", binding: "<Keyboard>/shift");

        zoomAction = new InputAction("Zoom", binding: "<Mouse>/scroll");

        clickAction = new InputAction("Click", binding: "<Mouse>/leftButton");

        mousePosAction = new InputAction("MousePos", binding: "<Mouse>/position");
    }

    void HandleSelection()
    {
        if (clickAction.WasPerformedThisFrame())
        {
            Vector2 mouseScreenPos = mousePosAction.ReadValue<Vector2>();

            Ray ray = cam.ScreenPointToRay(mouseScreenPos);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, Mathf.Infinity, carLayer))
            {
                CarAgent car = hit.collider.GetComponentInParent<CarAgent>();
                if (car != null)
                {
                    targetToFollow = car.transform;
                    Debug.Log($"[InputSystem] Œledzenie: {car.name}");
                }
            }
            else
            {
                targetToFollow = null;
            }
        }
    }

    void HandleMovementAndFollow()
    {
        Vector2 moveInput = moveAction.ReadValue<Vector2>();

        bool isMovingInput = moveInput.sqrMagnitude > 0.001f;

        if (isMovingInput)
        {
            targetToFollow = null;

            float currentSpeed = moveSpeed;
            if (sprintAction.IsPressed())
            {
                currentSpeed *= sprintMultiplier;
            }

            Vector3 moveDir = new Vector3(moveInput.x * (transform.position.y * 0.01f), 0, moveInput.y * (transform.position.y * 0.01f)).normalized;

            transform.position += moveDir * currentSpeed * Time.deltaTime;
        }
        else if (targetToFollow != null)
        {
            if (targetToFollow == null) return;

            Vector3 newPos = targetToFollow.position;
            newPos.y = transform.position.y;

            transform.position = newPos;
        }
    }

    void HandleZoom()
    {
        Vector2 scrollVal = zoomAction.ReadValue<Vector2>();

        if (Mathf.Abs(scrollVal.y) > 0.1f)
        {
            float zoomChange = -scrollVal.y * scrollSensitivity * Time.deltaTime;

            float currentHeight = transform.position.y;
            float newHeight = Mathf.Clamp(currentHeight + zoomChange, minHeight, maxHeight);

            Vector3 pos = transform.position;
            pos.y = newHeight;
            transform.position = pos;
        }
    }
    void CreateSelectionRing()
    {
        selectionRingObject = new GameObject("SelectionRing");
        selectionRingObject.transform.SetParent(this.transform);

        ringLineRenderer = selectionRingObject.AddComponent<LineRenderer>();

        ringLineRenderer.useWorldSpace = false;
        ringLineRenderer.loop = true;
        ringLineRenderer.positionCount = segments;
        ringLineRenderer.startWidth = lineWidth;
        ringLineRenderer.endWidth = lineWidth;

        ringLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        ringLineRenderer.startColor = ringColor;
        ringLineRenderer.endColor = ringColor;

        float angleStep = 360f / segments;
        for (int i = 0; i < segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            float x = Mathf.Cos(angle) * ringRadius;
            float z = Mathf.Sin(angle) * ringRadius;

            ringLineRenderer.SetPosition(i, new Vector3(x, 0.2f, z));
        }

        selectionRingObject.SetActive(false);
    }

    void UpdateSelectionRing()
    {
        if (targetToFollow != null)
        {
            if (!selectionRingObject.activeSelf) selectionRingObject.SetActive(true);

            selectionRingObject.transform.position = targetToFollow.position;
            selectionRingObject.transform.rotation = Quaternion.identity;
        }
        else
        {
            if (selectionRingObject.activeSelf) selectionRingObject.SetActive(false);
        }
    }
}