[System.Serializable]
public class Leaderboard
{
    public string playerName;
    public string score;
    public string date;
}

[System.Serializable]
public class LeaderboardList
{
    public Leaderboard[] entries;
}