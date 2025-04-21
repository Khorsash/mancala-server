using System.Collections.Generic;

namespace MancalaServer
{
    public static class GameManager
    {
        public static Dictionary<string, GameSession> Sessions = new Dictionary<string, GameSession>();
    }
}