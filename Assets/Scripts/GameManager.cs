using System;
using Unity.Netcode;
using UnityEngine;


public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    public event EventHandler<OnClickedOnGridPositionEventArgs> OnClickedOnGridPosition;
    public event EventHandler OnGameStarted;
    public event EventHandler OnCurrentPlayablePlayerTypeChanged;

    public class OnClickedOnGridPositionEventArgs : EventArgs
    {
        public int x;
        public int y;
        public PlayerType playerType;
    }

    public enum PlayerType { None, Cross, Circle }
    private PlayerType localPlayerType;

    private NetworkVariable<PlayerType> currentPlayablePlayerType = new NetworkVariable<PlayerType>();


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogError($"Only one instance of {typeof(GameManager)} is allowed!");
            Destroy(gameObject);
            return;
        }
    }


    public override void OnNetworkSpawn()
    {
        Debug.Log($"OnNetworkSpawn: {NetworkManager.Singleton.LocalClientId}.");

        if (NetworkManager.Singleton.LocalClientId == 0)
        {
            localPlayerType = PlayerType.Cross;
        }
        else
        {
            localPlayerType = PlayerType.Circle;
        }

        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += NetworkManager_OnClientConnectedCallback;
        }

        currentPlayablePlayerType.OnValueChanged += currentPlayablePlayerType_OnValueChanged;
    }

    private void currentPlayablePlayerType_OnValueChanged(PlayerType oldPlayerType, PlayerType newPlayerType)
    {
        OnCurrentPlayablePlayerTypeChanged?.Invoke(this, EventArgs.Empty);
    }


    private void NetworkManager_OnClientConnectedCallback(ulong obj)
    {
        if (NetworkManager.Singleton.ConnectedClientsList.Count == 2)
        {
            currentPlayablePlayerType.Value = PlayerType.Cross;
            TriggerOnGameStartedRpc();
        }
    }


    [Rpc(SendTo.ClientsAndHost)]
    private void TriggerOnGameStartedRpc()
    {
        OnGameStarted?.Invoke(this, EventArgs.Empty);
    }


    [Rpc(SendTo.Server)]
    public void ClickedOnGridPositionRpc(int x, int y, PlayerType playerType)
    {
        Debug.Log($"Clicked on grid position {x}, {y}. Is it server? {IsServer}.");

        if (playerType != currentPlayablePlayerType.Value)
        {
            return;
        }

        OnClickedOnGridPosition?.Invoke(this, new OnClickedOnGridPositionEventArgs() { x = x, y = y, playerType = playerType });

        if (currentPlayablePlayerType.Value == PlayerType.Cross)
        {
            currentPlayablePlayerType.Value = PlayerType.Circle;
        }
        else if (currentPlayablePlayerType.Value == PlayerType.Circle)
        {
            currentPlayablePlayerType.Value = PlayerType.Cross;
        }

        // TriggerOnCurrentPlayablePlayerTypeChangedRpc();
    }


    // Not needed anymore (NetworkVariable used for currentPlayablePlayerType).
    // [Rpc(SendTo.ClientsAndHost)]
    // private void TriggerOnCurrentPlayablePlayerTypeChangedRpc()
    // {
    //     OnCurrentPlayablePlayerTypeChanged?.Invoke(this, EventArgs.Empty);
    // }


    public PlayerType GetLocalPlayerType() => localPlayerType;
    public PlayerType GetCurrentPlayablePlayerType() => currentPlayablePlayerType.Value;
}
