using UnityEngine;

public class LeaderboardItem : MonoBehaviour
{
    [SerializeField]
    private TMPro.TMP_Text playerNameText;
    [SerializeField]
    private TMPro.TMP_Text scoreText;
    [SerializeField]
    private TMPro.TMP_Text dateText;
    public void SetLeaderboardInfo(string name, string score, string date)
    {
        playerNameText.text = name;
        scoreText.text = "Score: " + score;
        dateText.text = date;
    }
}
