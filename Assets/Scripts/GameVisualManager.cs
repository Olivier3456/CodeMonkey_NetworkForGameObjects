using Unity.Netcode;
using UnityEngine;

public class GameVisualManager : NetworkBehaviour
{
    private const float GRID_SIZE = 3.1f;


    [SerializeField] private Transform crossPrefab;
    [SerializeField] private Transform circlePrefab;

    void Start()
    {
        GameManager.Instance.OnClickedOnGridPosition += GameManagerOnClickedGridPosition;
    }


    private void GameManagerOnClickedGridPosition(object sender, GameManager.OnClickedOnGridPositionEventArgs e)
    {
        SpawnObjectRpc(e.x, e.y, e.playerType);
    }


    [Rpc(SendTo.Server)]
    private void SpawnObjectRpc(int x, int y, GameManager.PlayerType playerType)
    {
        //Debug.Log("SpawnObject");

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
        spawnedTransform.GetComponent<NetworkObject>().Spawn(true);
    }


    private Vector2 GetGridWorldPosition(int x, int y)
    {
        return new Vector2(-GRID_SIZE + x * GRID_SIZE, -GRID_SIZE + y * GRID_SIZE);
    }
}
