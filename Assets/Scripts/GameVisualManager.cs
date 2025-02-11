using System;
using Unity.Netcode;
using UnityEngine;

public class GameVisualManager : NetworkBehaviour
{
    private const float GRID_SIZE = 3.1f;


    [SerializeField] private Transform crossPrefab;
    [SerializeField] private Transform circlePrefab;
    [SerializeField] private Transform lineCompletePrefab;

    void Start()
    {
        GameManager.Instance.OnClickedOnGridPosition += GameManagerOnClickedOnGridPosition;
        GameManager.Instance.OnGameWin += GameManager_OnGameWin;
    }


    private void GameManagerOnClickedOnGridPosition(object sender, GameManager.OnClickedOnGridPositionEventArgs e)
    {
        SpawnObjectRpc(e.x, e.y, e.playerType);
    }


    private void GameManager_OnGameWin(object sender, GameManager.OnGameWinEventArgs e)
    {
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
    }


    private Vector2 GetGridWorldPosition(int x, int y)
    {
        return new Vector2(-GRID_SIZE + x * GRID_SIZE, -GRID_SIZE + y * GRID_SIZE);
    }
}
