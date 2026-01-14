using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    [Header("Movement")]
    public float maxSpeed = 5f;
    public float acceleration = 20f;
    public float deceleration = 25f;

    private Vector2 moveInput;
    private Vector2 currentVelocity;

    // PlayerInput → Invoke Unity Events → Player/Move
    public void Move(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    void Update()
    {
        // Zielgeschwindigkeit
        Vector2 targetVelocity = moveInput * maxSpeed;

        // Beschleunigen oder Abbremsen
        float accelRate = (moveInput.sqrMagnitude > 0.01f)
            ? acceleration
            : deceleration;

        currentVelocity = Vector2.MoveTowards(
            currentVelocity,
            targetVelocity,
            accelRate * Time.deltaTime
        );

        transform.Translate(currentVelocity * Time.deltaTime);
    }
}
