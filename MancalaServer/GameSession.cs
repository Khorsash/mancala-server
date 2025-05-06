using System;
using System.Linq;

namespace MancalaServer
{
    class Game
    {
        public static int[] MakeMove(int[] board, int pitIndx)
        {
            int turn = board[board.Length-1];
            int startIndx = turn == 1 ? pitIndx : pitIndx + 7;

            if(startIndx > 13 || board.Length != 15 || board[startIndx] == 0) 
            {return board;}

            int[] finalBoard = board.ToArray();
            int opponentsBar = turn == 1 ? 13 : 6;
            int ownBar = turn == 1 ? 6 : 13;
            int piecesCount = board[startIndx] == 1 ? 1 : board[startIndx]-1;
            finalBoard[startIndx] = board[startIndx] == 1 ? 0 : 1;
            int currentIndex = startIndx;
            while(piecesCount>0)
            {
                currentIndex = (currentIndex+1)%(board.Length-1);
                if(currentIndex == opponentsBar) continue;
                finalBoard[currentIndex]++;
                piecesCount--;
            }

            if((turn == 1 ? (currentIndex<6) : (currentIndex>6&&currentIndex<13)) && finalBoard[currentIndex] == 1 && finalBoard[12-currentIndex] != 0)
            {
                finalBoard[ownBar] += finalBoard[12-currentIndex] + 1;
                finalBoard[12-currentIndex] = 0;
                finalBoard[currentIndex] = 0;
            }

            if((turn == 1 ? (currentIndex>6&&currentIndex<13) : (currentIndex<6)) && finalBoard[currentIndex] % 2 == 0)
            {
                finalBoard[ownBar] += finalBoard[currentIndex];
                finalBoard[currentIndex] = 0;
            }
            
            if(finalBoard.AsSpan(0, 6).ToArray().Sum() == 0)
            {
                finalBoard[6] += finalBoard.AsSpan(7, 6).ToArray().Sum();
                for(int i=7; i<13; i++) finalBoard[i] = 0;
            }
            else 
            {
                if(finalBoard.AsSpan(7, 6).ToArray().Sum() == 0)
                {
                    finalBoard[13] += finalBoard.AsSpan(0, 6).ToArray().Sum();
                    for(int i=0; i<6; i++) finalBoard[i] = 0;
                }
            }
            
            if(currentIndex == ownBar)
            {return finalBoard;}
            
            finalBoard[board.Length-1] = turn == 1 ? 2 : 1;

            return finalBoard;
        }
    }
    public class GameSession
    {
        public string SessionId { get; }
        public string? Player1 { get; private set; }
        public string? Player2 { get; private set; }
        public string? Player1Nickname {get; private set;}
        public string? Player2Nickname {get; private set;}
        public int[] GameState { get; private set; }
        public bool IsGameOver { get; private set; }
        public bool isPublic {get; private set;}

        public GameSession(string sessionId, bool publicity)
        {
            isPublic = publicity;
            GameState = new int[15] {4, 4, 4, 4, 4, 4, 0, 4, 4, 4, 4, 4, 4, 0, 1};
            SessionId = sessionId;
            IsGameOver = false;
        }

        public bool AddPlayer(string connectionId, string nickname="Anonymous")
        {
            if (Player1 == null)
            {
                Player1 = connectionId;
                Player1Nickname = nickname;
                return true;
            }
            else if (Player2 == null)
            {
                Player2 = connectionId;
                Player2Nickname = nickname;
                return true;
            }
            return false;
        }

        public bool IsGameFull()
        {
            return Player1 != null && Player2 != null;
        }

        public bool CanMakeMove(string connectionId)
        {
            return (GameState[GameState.Length-1] == 1 && connectionId == Player1) ||
                   (GameState[GameState.Length-1] == 2 && connectionId == Player2);
        }

        public void MakeMove(string move)
        {
            int pitIndx = Convert.ToInt16(move);
            
            GameState = Game.MakeMove(GameState, pitIndx);
            if(GetWinner() != "0") IsGameOver = true;
        }

        public string? GetPlayerRole(string connectionId)
        {
            if (connectionId == Player1) return "1";
            if (connectionId == Player2) return "2";
            return null;
        }

        public string GetCurrentPlayerConnectionId()
        {
            return GameState[GameState.Length-1] == 1 ? Player1! : Player2!;
        }

        public string GetWinner()
        {
            string winner;
            if(GameState.AsSpan(0, 6).ToArray().Sum()+GameState.AsSpan(7, 6).ToArray().Sum() == 0)
            {
                winner = GameState[6] > GameState[13] ? "1" : "2";
                if (GameState[6] == GameState[13]) winner = "3";
                return winner;
            }
            return "0";
        }
    }
}