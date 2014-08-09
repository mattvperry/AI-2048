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
            GameAI ai = new GameAI();
            using (GamePage page = new GamePage())
            {
                while (true)
                {
                    GameState gs = new GameState(page.GetGameState());
                    Moves move = ai.FindBestMove(gs);
                    page.MakeMove(move);

                    if(page.IsGameOver())
                    {
                        Console.WriteLine("Final game score: {0}", page.CurrentScore());
                        Console.Write("Restart game? [Y/n]: ");
                        string input = Console.ReadLine().ToLower();
                        if (input == "n") break;

                        page.RestartGame();
                    }
                }
            }
        }
    }
}
