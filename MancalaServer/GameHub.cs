using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace MancalaServer
{
    public class GameHub : Hub
    {
        public async Task ListPublicGames()
        {
            List<string> avaliableGames = new List<string>();
            foreach(var sessionId in GameManager.Sessions.Keys)
            {
                if(GameManager.Sessions[sessionId].isPublic && !GameManager.Sessions[sessionId].IsGameFull()) 
                {
                    avaliableGames.Add(sessionId);
                }
            }
            await Clients.Caller.SendAsync("AvaliableGames", string.Join(",", avaliableGames));
        }
        public async Task QuickGame()
        {
            GameSession session;
            string? role;
            foreach(var sessionId in GameManager.Sessions.Keys)
            {
                session = GameManager.Sessions[sessionId];

                if(!session.isPublic) 
                {continue;}
                if(GameManager.Sessions[sessionId].AddPlayer(Context.ConnectionId))
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);
                    GameManager.Sessions[sessionId].AddPlayer(Context.ConnectionId);
                    role = session.GetPlayerRole(Context.ConnectionId);
                    await Clients.Caller.SendAsync("sessionID", sessionId);
                    await Clients.Caller.SendAsync("Role", role);

                    if (session.Player1 != null && session.Player2 != null)
                    {
                        await Clients.Group(sessionId).SendAsync("GameState", string.Join(",", session.GameState)+",");
                    }
                    return;
                }
            }
            string sessionId2 = Guid.NewGuid().ToString("N").Substring(0, 32);
            while(GameManager.Sessions.ContainsKey(sessionId2)) sessionId2 = Guid.NewGuid().ToString("N").Substring(0, 32);
            session = new GameSession(sessionId2, true);
            GameManager.Sessions[sessionId2] = session;
            await Groups.AddToGroupAsync(Context.ConnectionId, sessionId2);
            session.AddPlayer(Context.ConnectionId);
            role = session.GetPlayerRole(Context.ConnectionId);
            await Clients.Caller.SendAsync("sessionID", sessionId2);
            await Clients.Caller.SendAsync("Role", role);
            await Clients.Caller.SendAsync("WaitingForOpponent", sessionId2);
        }
        public async Task JoinGame(string sessionId)
        {
            if (!GameManager.Sessions.TryGetValue(sessionId, out var session))
            {
                await Clients.Caller.SendAsync("Error", "Game doesn't exist");
                return;
            }
            if (!session.AddPlayer(Context.ConnectionId))
            {
                await Clients.Caller.SendAsync("Error", "Game full");
                return;
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);

            session.AddPlayer(Context.ConnectionId);
            var role = session.GetPlayerRole(Context.ConnectionId);
            await Clients.Caller.SendAsync("Role", role);

            if (session.Player1 != null && session.Player2 != null)
            {
                await Clients.Group(sessionId).SendAsync("GameState", string.Join(",", session.GameState)+",");
            }
        }
        public async Task CreateGame(string sessionID, string publicity)
        {
            string sessionId = sessionID == "" ? Guid.NewGuid().ToString("N").Substring(0, 32) : sessionID;
            if(GameManager.Sessions.ContainsKey(sessionId))
            {await Clients.Caller.SendAsync("Error", "Game with that id already exists");}
            while(GameManager.Sessions.ContainsKey(sessionId)) sessionId = Guid.NewGuid().ToString("N").Substring(0, 32);
            GameSession session = new GameSession(sessionId, publicity == "True");
            GameManager.Sessions[sessionId] = session;
            session.AddPlayer(Context.ConnectionId);
            await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);
            var role = session.GetPlayerRole(Context.ConnectionId);
            await Clients.Caller.SendAsync("sessionID", sessionId);
            await Clients.Caller.SendAsync("Role", role);
            await Clients.Caller.SendAsync("WaitingForOpponent", sessionId);
        }

        public async Task MakeMove(string sessionId, string move)
        {
            if (!GameManager.Sessions.TryGetValue(sessionId, out var session)) return;

            if (!session.CanMakeMove(Context.ConnectionId))
            {
                await Clients.Caller.SendAsync("Error", "Not your turn");
                return;
            }
            string moveForHistory = session.GetPlayerRole(session.GetCurrentPlayerConnectionId()) == "1" 
                                    ? move 
                                    : Convert.ToString(Convert.ToInt16(move)+7);
            session.MakeMove(move);
            await Clients.Group(sessionId).SendAsync("GameState", string.Join(",", session.GameState)+","+moveForHistory);

            if (session.IsGameOver)
            {
                await Clients.Group(sessionId).SendAsync("GameOver", session.GetWinner());
                GameManager.Sessions.Remove(sessionId);
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            foreach (var session in GameManager.Sessions.Values)
            {
                if (session.Player1 == Context.ConnectionId || session.Player2 == Context.ConnectionId)
                {
                    await Clients.Group(session.SessionId).SendAsync("GameOver", "Opponent disconnected.");
                    GameManager.Sessions.Remove(session.SessionId);
                }
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}