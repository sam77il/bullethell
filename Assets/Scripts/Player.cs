using UnityEngine;
using UnityEngine.InputSystem;
using FishNet.Object;
using FishNet.Connection;
using FishNet.Object.Synchronizing;
using System.Threading.Tasks;
using System.Collections.Generic;

public class Player : NetworkBehaviour
{
    [Header("Movement")]
    public float maxSpeed = 5f;
    public float acceleration = 20f;
    public float deceleration = 25f;
    private Vector2 moveInput;
    private Vector2 currentVelocity;
    private LobbyManager lobbyManager;
    public string PlayerName;

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
        if (IsOwner)
        {
            lobbyManager = FindFirstObjectByType<LobbyManager>();
            if (lobbyManager != null)
            {    
                Debug.Log("Registering player on server: " + Owner.ClientId);
                lobbyManager.lobbyPanel.SetActive(true);
                lobbyManager.createGamePanel.SetActive(false);
                lobbyManager.findGamesPanel.SetActive(false);

                lobbyManager.ClientId = Owner.ClientId;
                PlayerName = lobbyManager.enteredName;
                RegisterPlayerServerRpc(lobbyManager.enteredName);
            }
        }

        SpawnPlayer();
    }

    public void SpawnPlayer()
    {
        PlayerInput input = GetComponent<PlayerInput>();

        if (input != null)
        {   
            input.enabled = IsOwner;
        }
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        
        if (IsOwner && lobbyManager != null)
        {
            lobbyManager.GoToLobby();
        }
    }

    public void CheckPassword(string password)
    {
        Debug.Log("Testing password: " + password);
        MyServerManager.Instance.CheckPassword(this, password);
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestServerClose()
    {
        Debug.Log("Requesting Close");
        MyServerManager.Instance.CloseGameServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestStartGame()
    {
        MyServerManager.Instance.StartGame();
    }

    [TargetRpc]
    public void Disconnect(NetworkConnection conn)
    {
        if (lobbyManager != null)
        {
            lobbyManager.GoToLobby();
            ClientManager.StopConnection();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void LeaveLobby()
    {
        MyServerManager.Instance.LeaveLobby(this);
    }

    [ServerRpc]
    private void RegisterPlayerServerRpc(string playerName)
    {
        MyServerManager.Instance.AddPlayer(this, playerName);
    }

    [TargetRpc]
    public void UpdateLobby(NetworkConnection conn, List<PlayerData> playersData)
    {
        lobbyManager.RefreshLobbyUI(playersData);
    }

    void Update()
    {
        if (!IsOwner)
            return;

        Vector2 targetVelocity = moveInput * maxSpeed;

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
