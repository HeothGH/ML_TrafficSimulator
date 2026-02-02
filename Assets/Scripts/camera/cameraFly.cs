using UnityEngine;
using UnityEngine.InputSystem;

public class cameraFly : MonoBehaviour
{
    public float moveSpeed = 5f;       // podstawowa prêdkoœæ
    public float fastMultiplier = 2f;  // mno¿nik przyspieszenia (Shift)
    public float mouseSensitivity = 2f; // czu³oœæ myszy

    private float rotationX;
    private float rotationY;

    void Update()
    {
        HandleMouseLook();
        HandleMovement();
    }

    void HandleMouseLook()
    {
        if(Mouse.current.rightButton.isPressed)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            Vector2 mouseDelta = Mouse.current.delta.ReadValue();

            rotationX += mouseDelta.x * mouseSensitivity * Time.deltaTime;
            rotationY -= mouseDelta.y * mouseSensitivity * Time.deltaTime;
            rotationY = Mathf.Clamp(rotationY, -90f, 90f);

            transform.rotation = Quaternion.Euler(rotationY, rotationX, 0);
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    void HandleMovement()
    {
        float speed = moveSpeed;

        if (Keyboard.current.leftShiftKey.isPressed)
            speed *= fastMultiplier;

        Vector3 move = Vector3.zero;

        if (Keyboard.current.wKey.isPressed) move += transform.forward;
        if (Keyboard.current.sKey.isPressed) move -= transform.forward;
        if (Keyboard.current.aKey.isPressed) move -= transform.right;
        if (Keyboard.current.dKey.isPressed) move += transform.right;
        if (Keyboard.current.shiftKey.isPressed) move += transform.up;
        if (Keyboard.current.leftCtrlKey.isPressed) move -= transform.up;

        transform.position += move.normalized * speed * Time.deltaTime;
    }
}
