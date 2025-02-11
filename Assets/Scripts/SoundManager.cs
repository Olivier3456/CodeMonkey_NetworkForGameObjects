using System;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    [SerializeField] private Transform placeSfxPrefab;
    [SerializeField] private Transform winSfxPrefab;
    [SerializeField] private Transform loseSfxPrefab;


    private void Start()
    {
        GameManager.Instance.OnPlacedObject += GameManager_OnPlacedObject;
        GameManager.Instance.OnGameWin += GameManager_OnGameWin;
    }


    private void GameManager_OnPlacedObject(object sender, EventArgs e)
    {
        Transform placeSfxTransform = Instantiate(placeSfxPrefab);
        Destroy(placeSfxTransform.gameObject, 2f);
    }


    private void GameManager_OnGameWin(object sender, GameManager.OnGameWinEventArgs e)
    {
        if (GameManager.Instance.GetLocalPlayerType() == e.winPlayerType)
        {
            Transform winSfxTransform = Instantiate(winSfxPrefab);
            Destroy(winSfxTransform.gameObject, 10f);
        }
        else
        {
            Transform loseSfxTransform = Instantiate(loseSfxPrefab);
            Destroy(loseSfxTransform.gameObject, 10f);
        }
    }
}
