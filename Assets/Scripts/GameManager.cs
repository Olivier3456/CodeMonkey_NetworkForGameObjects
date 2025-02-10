using System;
using Unity.Netcode;
using UnityEngine;


public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    public event EventHandler<OnClickedOnGridPositionEventArgs> OnClickedOnGridPosition;

    public class OnClickedOnGridPositionEventArgs : EventArgs
    {
        public int x;
        public int y;
        public PlayerType playerType;
    }

    public enum PlayerType { None, Cross, Circle }
    private PlayerType localPlayerType;


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
    }


    public void ClickedOnGridPosition(int x, int y)
    {
        Debug.Log($"Clicked on grid position {x}, {y}.");
        OnClickedOnGridPosition?.Invoke(this, new OnClickedOnGridPositionEventArgs() { x = x, y = y, playerType = GetLocalPlayertType() });
    }

    public PlayerType GetLocalPlayertType() => localPlayerType;
}
