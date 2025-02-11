using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameOverUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI gameOverText;
    [SerializeField] private Color winColor;
    [SerializeField] private Color loseColor;

    [SerializeField] private Button rematchButton;


    private void Awake()
    {
        rematchButton.onClick.AddListener(() =>
        {
            GameManager.Instance.RematchRpc();
        });
    }


    private void Start()
    {
        GameManager.Instance.OnGameWin += GameManager_OnGameWin;
        GameManager.Instance.OnRematch += GameManager_OnRematch;
        Hide();
    }



    private void GameManager_OnGameWin(object sender, GameManager.OnGameWinEventArgs e)
    {
        if (e.winPlayerType == GameManager.Instance.GetLocalPlayerType())
        {
            gameOverText.text = "YOU WIN";
            gameOverText.color = winColor;
        }
        else
        {
            gameOverText.text = "YOU LOSE";
            gameOverText.color = loseColor;
        }

        Show();
    }


    private void GameManager_OnRematch(object sender, EventArgs e)
    {
        Hide();
    }


    private void Show() => gameObject.SetActive(true);
    private void Hide() => gameObject.SetActive(false);
}
