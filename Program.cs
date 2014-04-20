using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AI_2048
{
    class Program
    {
        static void Main(string[] args)
        {
            long[,] grid = new long[4, 4]
            {
                { 4, 2, 0, 0 },
                { 8, 2, 0, 0 },
                { 2, 0, 0, 0 },
                { 4, 2, 0, 0 }
            };

            GameAI ai = new GameAI();
            using (GamePage page = new GamePage())
            {
                while (true)
                {
                    GameState gs = new GameState(page.GetGameState());
                    Moves move = ai.FindBestMove(gs);
                    page.MakeMove(move);
                }
            }
        }
    }
}
