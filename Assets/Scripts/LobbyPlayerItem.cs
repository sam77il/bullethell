using System;
using UnityEngine;
using UnityEngine.UI;

public class LobbyPlayerItem : MonoBehaviour
{
    [SerializeField]
    private TMPro.TMP_Text playerNameText;

    [SerializeField]
    private Button kickButton;

    public void SetPlayerInfo(string playerName, bool isHost, Action callback)
    {
        playerNameText.text = playerName;
        kickButton.gameObject.SetActive(isHost);
        kickButton.onClick.RemoveAllListeners();

        kickButton.onClick.AddListener(() => callback());
    }
}
