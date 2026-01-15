[System.Serializable]
public class Game
{
    public string ip;
    public int port;
    public string password;
    public int players;
}

[System.Serializable]
public class GameList
{
    public Game[] games;
}