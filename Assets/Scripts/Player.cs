using UnityEngine;
using UnityEngine.InputSystem;
using FishNet.Object;
using FishNet.Connection;

public class Player : NetworkBehaviour
{
    [Header("Movement")]
    public float maxSpeed = 5f;
    public float acceleration = 20f;
    public float deceleration = 25f;
    private Vector2 moveInput;
    private Vector2 currentVelocity;

    public void Move(InputAction.CallbackContext context)
    {
        // Nur der lokale Spieler darf Input setzen
        if (!IsOwner)
            return;

        moveInput = context.ReadValue<Vector2>();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        PlayerInput input = GetComponent<PlayerInput>();

        if (input != null)
            input.enabled = IsOwner;
    }

    [TargetRpc]
    public void OnRegisteredTargetRpc(NetworkConnection conn)
    {
        Debug.Log("Player " + Owner.ClientId + " is active. Registered!!!" + MyServerManager.Instance.ConnectedPlayers.Value);
    }

    void Update()
    {
        // Nur der Owner bewegt sich selbst
        if (!IsOwner)
            return;

        // Zielgeschwindigkeit
        Vector2 targetVelocity = moveInput * maxSpeed;

        // Beschleunigung oder Abbremsen
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
