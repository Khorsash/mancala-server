using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace MancalaServer
{
    public class GameHub : Hub
    {
        public async Task JoinGame(string sessionId)
        {
            if (!GameManager.Sessions.TryGetValue(sessionId, out var session))
            {
                session = new GameSession(sessionId);
                GameManager.Sessions[sessionId] = session;
            }

            if (!session.AddPlayer(Context.ConnectionId))
            {
                await Clients.Caller.SendAsync("Error", "Game full");
                return;
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);

            var role = session.GetPlayerRole(Context.ConnectionId);
            await Clients.Caller.SendAsync("Role", role);

            if (session.Player1 != null && session.Player2 != null)
            {
                await Clients.Group(sessionId).SendAsync("GameState", string.Join(",", session.GameState));
                await Clients.Client(session.GetCurrentPlayerConnectionId()).SendAsync("CanMove");
            }
            else
            {
                await Clients.Caller.SendAsync("WaitingForOpponent");
            }
        }

        public async Task MakeMove(string sessionId, string move)
        {
            if (!GameManager.Sessions.TryGetValue(sessionId, out var session)) return;

            if (!session.CanMakeMove(Context.ConnectionId))
            {
                await Clients.Caller.SendAsync("Error", "Not your turn");
                return;
            }

            session.MakeMove(move);

            await Clients.Group(sessionId).SendAsync("GameState", string.Join(",", session.GameState));

            if (session.IsGameOver)
            {
                await Clients.Group(sessionId).SendAsync("GameOver", session.GetWinner());
                GameManager.Sessions.Remove(sessionId);
            }
            else
            {
                await Clients.Client(session.GetCurrentPlayerConnectionId()).SendAsync("CanMove");
                await Clients.Group(sessionId).SendAsync("MadeMove", move);
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            foreach (var session in GameManager.Sessions.Values)
            {
                if (session.Player1 == Context.ConnectionId || session.Player2 == Context.ConnectionId)
                {
                    await Clients.Group(session.SessionId).SendAsync("GameOver", "Opponent disconnected.");
                }
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}