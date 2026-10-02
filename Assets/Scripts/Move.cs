using UnityEngine;
using UnityEngine.InputSystem;

public class FollowCursor : MonoBehaviour
{
    Rigidbody2D rb;
    Camera cam;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        cam = Camera.main;
    }

    void FixedUpdate()
    {
        if (Mouse.current == null) return; // no mouse connected

        Vector2 mouseScreen = Mouse.current.position.ReadValue();
        Vector2 mouseWorld = cam.ScreenToWorldPoint(mouseScreen);
        rb.MovePosition(mouseWorld);
    }
}