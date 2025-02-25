using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;


public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    public event EventHandler<OnClickedOnGridPositionEventArgs> OnClickedOnGridPosition;
    public event EventHandler OnGameStarted;
    public event EventHandler OnCurrentPlayablePlayerTypeChanged;
    public event EventHandler<OnGameWinEventArgs> OnGameWin;
    public event EventHandler OnRematch;
    public event EventHandler OnGameTied;
    public event EventHandler OnScoreChanged;
    public event EventHandler OnPlacedObject;

    public class OnClickedOnGridPositionEventArgs : EventArgs
    {
        public int x;
        public int y;
        public PlayerType playerType;
    }

    public class OnGameWinEventArgs : EventArgs
    {
        public Line line;
        public PlayerType winPlayerType;
    }

    public enum PlayerType { None, Cross, Circle }
    private PlayerType localPlayerType;

    private NetworkVariable<PlayerType> currentPlayablePlayerType = new NetworkVariable<PlayerType>();  // don't forget to initialize NetworkVariables at declaration

    private PlayerType[,] playerTypeArray;

    private List<Line> lineList;

    public enum Orientation { Horizontal, Vertical, DiagonalDownLeftToUpRight, DiagonalUpLeftToDownRight }


    public struct Line
    {
        public List<Vector2Int> gridVector2IntList;
        public Vector2Int centerGridPosition;
        public Orientation orientation;
    }


    private NetworkVariable<int> playerCrossScore = new NetworkVariable<int>(); // don't forget to initialize NetworkVariables at declaration
    private NetworkVariable<int> playerCircleScore = new NetworkVariable<int>();


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

        playerTypeArray = new PlayerType[3, 3];

        lineList = new List<Line>()
        {
                 // horizontals
                 new Line {
                gridVector2IntList = new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0)},
                centerGridPosition = new Vector2Int(1, 0),
                orientation = Orientation.Horizontal
                 },
                 new Line {
                gridVector2IntList = new List<Vector2Int> { new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(2, 1)},
                centerGridPosition = new Vector2Int(1, 1),
                orientation = Orientation.Horizontal
                 },
                 new Line {
                gridVector2IntList = new List<Vector2Int> { new Vector2Int(0, 2), new Vector2Int(1, 2), new Vector2Int(2, 2)},
                centerGridPosition = new Vector2Int(1, 2),
                orientation = Orientation.Horizontal

                 },


                 // verticals
                  new Line {
                gridVector2IntList = new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, 2)},
                centerGridPosition = new Vector2Int(0, 1),
                orientation = Orientation.Vertical
                 },
                  new Line {
                gridVector2IntList = new List<Vector2Int> { new Vector2Int(1, 0), new Vector2Int(1, 1), new Vector2Int(1, 2)},
                centerGridPosition = new Vector2Int(1, 1),
                orientation = Orientation.Vertical
                 },
                  new Line {
                gridVector2IntList = new List<Vector2Int> { new Vector2Int(2, 0), new Vector2Int(2, 1), new Vector2Int(2, 2)},
                centerGridPosition = new Vector2Int(2, 1),
                orientation = Orientation.Vertical
                 },


                 // diagonals
                  new Line {
                gridVector2IntList = new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(1, 1), new Vector2Int(2, 2)},
                centerGridPosition = new Vector2Int(1, 1),
                orientation = Orientation.DiagonalDownLeftToUpRight
                 },
                   new Line {
                gridVector2IntList = new List<Vector2Int> { new Vector2Int(0, 2), new Vector2Int(1, 1), new Vector2Int(2, 0)},
                centerGridPosition = new Vector2Int(1, 1),
                orientation = Orientation.DiagonalUpLeftToDownRight
                 }
        };
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

        currentPlayablePlayerType.OnValueChanged += CurrentPlayablePlayerType_OnValueChanged;

        // we want to the value of the NetworkVariale to be effectively changed before using it
        playerCrossScore.OnValueChanged += (int previousScore, int newScore) =>
        {
            OnScoreChanged?.Invoke(this, EventArgs.Empty);
        };

        playerCircleScore.OnValueChanged += (int previousScore, int newScore) =>
        {
            OnScoreChanged?.Invoke(this, EventArgs.Empty);
        };
    }

    private void CurrentPlayablePlayerType_OnValueChanged(PlayerType oldPlayerType, PlayerType newPlayerType)
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


    [Rpc(SendTo.ClientsAndHost)]    // this function will be executed by client instances too
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

        if (playerTypeArray[x, y] != PlayerType.None)
        {
            return;
        }

        playerTypeArray[x, y] = playerType;
        TriggerOnPlacedObjectRpc();

        OnClickedOnGridPosition?.Invoke(this, new OnClickedOnGridPositionEventArgs() { x = x, y = y, playerType = playerType });

        if (currentPlayablePlayerType.Value == PlayerType.Cross)
        {
            currentPlayablePlayerType.Value = PlayerType.Circle;
        }
        else if (currentPlayablePlayerType.Value == PlayerType.Circle)
        {
            currentPlayablePlayerType.Value = PlayerType.Cross;
        }

        TestWinner();

        // TriggerOnCurrentPlayablePlayerTypeChangedRpc();
    }


    [Rpc(SendTo.ClientsAndHost)]
    private void TriggerOnPlacedObjectRpc()
    {
        OnPlacedObject?.Invoke(this, EventArgs.Empty);
    }


    // For the event to be triggered on all instances, Server and Clients.
    // But it is not needed anymore: a NetworkVariable is used for currentPlayablePlayerType.
    // [Rpc(SendTo.ClientsAndHost)]
    // private void TriggerOnCurrentPlayablePlayerTypeChangedRpc()
    // {
    //     OnCurrentPlayablePlayerTypeChanged?.Invoke(this, EventArgs.Empty);
    // }


    private void TestWinner()
    {
        for (int i = 0; i < lineList.Count; i++)
        {
            Line line = lineList[i];
            {
                if (TestWinnerLine(line))
                {
                    Debug.Log("Winner!");
                    currentPlayablePlayerType.Value = PlayerType.None;

                    PlayerType winPlayerType = playerTypeArray[line.centerGridPosition.x, line.centerGridPosition.y];

                    switch (winPlayerType)
                    {
                        case PlayerType.Cross:
                            playerCrossScore.Value++;
                            break;
                        case PlayerType.Circle:
                            playerCircleScore.Value++;
                            break;
                    }

                    TriggerOnGameWinRpc(i, winPlayerType);
                    return;
                }
            }
        }

        // verify tie condition
        bool hasTie = true;
        for (int x = 0; x < playerTypeArray.GetLength(0); x++)
        {
            for (int y = 0; y < playerTypeArray.GetLength(1); y++)
            {
                if (playerTypeArray[x, y] == PlayerType.None)
                {
                    hasTie = false;
                    break;
                }
            }
        }

        if (hasTie)
        {
            TriggerOnGameTiedRpc();
        }
    }


    private bool TestWinnerLine(PlayerType aPlayerType, PlayerType bPlayerType, PlayerType cPlayerType)
    {
        return aPlayerType != PlayerType.None &&
               aPlayerType == bPlayerType &&
               aPlayerType == cPlayerType;
    }


    private bool TestWinnerLine(Line line)
    {
        return TestWinnerLine(playerTypeArray[line.gridVector2IntList[0].x, line.gridVector2IntList[0].y],
                              playerTypeArray[line.gridVector2IntList[1].x, line.gridVector2IntList[1].y],
                              playerTypeArray[line.gridVector2IntList[2].x, line.gridVector2IntList[2].y]);
    }


    [Rpc(SendTo.ClientsAndHost)]    // for the event to be sent for both Host and Client sides
    public void TriggerOnGameWinRpc(int lineIndex, PlayerType winPlayerType)
    {
        Line line = lineList[lineIndex];

        OnGameWin?.Invoke(this, new OnGameWinEventArgs
        {
            line = line,
            winPlayerType = winPlayerType
        });
    }


    [Rpc(SendTo.ClientsAndHost)]    // same here
    public void TriggerOnGameTiedRpc()
    {
        OnGameTied?.Invoke(this, EventArgs.Empty);
    }


    [Rpc(SendTo.Server)]
    public void RematchRpc()
    {
        for (int x = 0; x < playerTypeArray.GetLength(0); x++)
        {
            for (int y = 0; y < playerTypeArray.GetLength(1); y++)
            {
                playerTypeArray[x, y] = PlayerType.None;
            }
        }

        currentPlayablePlayerType.Value = PlayerType.Cross;

        TriggerOnRematchRpc();
    }


    [Rpc(SendTo.ClientsAndHost)] // this event need to be sent both sides Host and Clients => see GameOverUI
    private void TriggerOnRematchRpc()
    {
        OnRematch?.Invoke(this, EventArgs.Empty);
    }




    public PlayerType GetLocalPlayerType() => localPlayerType;
    public PlayerType GetCurrentPlayablePlayerType() => currentPlayablePlayerType.Value;
    public void GetScores(out int playerCrossScore, out int playerCircleScore)
    {
        playerCrossScore = this.playerCrossScore.Value;
        playerCircleScore = this.playerCircleScore.Value;
    }
}
