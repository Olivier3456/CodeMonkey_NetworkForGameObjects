using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GameVisualManager : NetworkBehaviour
{
    private const float GRID_SIZE = 3.1f;


    [SerializeField] private Transform crossPrefab;
    [SerializeField] private Transform circlePrefab;
    [SerializeField] private Transform lineCompletePrefab;


    // for Relay: enabling games visuals when both players connected to the relay
    [SerializeField] private GameObject gameVisualMainObject;
    [SerializeField] private GameObject relayConnectionVisualObject;


    private List<GameObject> visualGameObjectList;


    private void Awake()
    {
        visualGameObjectList = new List<GameObject>();
    }


    private void Start()
    {
        GameManager.Instance.OnClickedOnGridPosition += GameManagerOnClickedOnGridPosition;
        GameManager.Instance.OnGameWin += GameManager_OnGameWin;
        GameManager.Instance.OnRematch += GameManager_OnRematch;

        // for Relay: enabling games visuals when both players connected to the relay
        GameManager.Instance.OnGameStarted += GameManager_OnGameStarted;
    }


    // for Relay: enabling games visuals when both players connected to the relay
    private void GameManager_OnGameStarted(object sender, EventArgs e)
    {
        gameVisualMainObject.SetActive(true);
        relayConnectionVisualObject.SetActive(false);
    }


    private void GameManagerOnClickedOnGridPosition(object sender, GameManager.OnClickedOnGridPositionEventArgs e)
    {
        SpawnObjectRpc(e.x, e.y, e.playerType);
    }


    private void GameManager_OnGameWin(object sender, GameManager.OnGameWinEventArgs e)
    {
        if (!NetworkManager.Singleton.IsServer)
        {
            return;
        }

        float eulerZ = 0f;
        switch (e.line.orientation)
        {
            case GameManager.Orientation.Horizontal: eulerZ = 0f; break;
            case GameManager.Orientation.Vertical: eulerZ = 90f; break;
            case GameManager.Orientation.DiagonalDownLeftToUpRight: eulerZ = 45f; break;
            case GameManager.Orientation.DiagonalUpLeftToDownRight: eulerZ = -45f; break;
        }

        Transform lineCompleteTransform = Instantiate(lineCompletePrefab,
                                                      GetGridWorldPosition(e.line.centerGridPosition.x, e.line.centerGridPosition.y),
                                                      Quaternion.Euler(0, 0, eulerZ));

        lineCompleteTransform.GetComponent<NetworkObject>().Spawn(true); // pour que ça spawne aussi chez les clients
        visualGameObjectList.Add(lineCompleteTransform.gameObject);
    }


    private void GameManager_OnRematch(object sender, EventArgs e)
    {
        // This event is sent for both Host and Clients, but we only want to destroy gameObject in Host side:
        // Theses gameObjects are NetworkObjects, so they will be destroyed for both sides if Server destroys it.
        // However, we don't need this check, because Client side, visualGameObjectList is empty, but we add it just for the logic.
        if (!NetworkManager.Singleton.IsServer)
        {
            return;
        }

        foreach (GameObject go in visualGameObjectList)
        {
            Destroy(go);
        }
    }


    //[Rpc(SendTo.Server)] // not needed here, because the function which call this one is only exec by server
    private void SpawnObjectRpc(int x, int y, GameManager.PlayerType playerType)
    {
        Transform prefab = null;
        switch (playerType)
        {
            case GameManager.PlayerType.Cross:
                prefab = crossPrefab;
                break;
            case GameManager.PlayerType.Circle:
                prefab = circlePrefab;
                break;
        }

        Transform spawnedTransform = Instantiate(prefab, GetGridWorldPosition(x, y), Quaternion.identity);
        spawnedTransform.GetComponent<NetworkObject>().Spawn(true); // pour que ça spawne aussi chez les clients
        visualGameObjectList.Add(spawnedTransform.gameObject);
    }


    private Vector2 GetGridWorldPosition(int x, int y)
    {
        return new Vector2(-GRID_SIZE + x * GRID_SIZE, -GRID_SIZE + y * GRID_SIZE);
    }
}
