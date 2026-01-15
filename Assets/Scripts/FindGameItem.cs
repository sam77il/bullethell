using UnityEngine;

public class FindGameItem : MonoBehaviour
{
    [SerializeField]
    private TMPro.TMP_Text gameNameText;
    [SerializeField]
    private TMPro.TMP_Text portText;
    [SerializeField]
    private TMPro.TMP_Text playersText;

    public void SetGameInfo(string name, int port, int players)
    {
        gameNameText.text = "Name: " + name;
        portText.text = "Port: " + port.ToString();
        playersText.text = "Players: " + players.ToString() + "/2";
    }
}
