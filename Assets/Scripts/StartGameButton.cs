using System;
using UnityEngine;

public class StartGameButton : MonoBehaviour
{
    [SerializeField] private GameObject startGameButtonGameObject;
    [SerializeField] private TestLobby testLobby;


    void Start()
    {
        startGameButtonGameObject.SetActive(false);
        testLobby.OnGameReadyToStart += TestLobby_OnGameReadyToStart;
    }


    private void TestLobby_OnGameReadyToStart(object sender, EventArgs e)
    {
        startGameButtonGameObject.SetActive(true);
    }


    public void StartGame_Button()
    {
        testLobby.StartGame();
    }
}
