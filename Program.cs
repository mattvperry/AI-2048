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
            int[,] test = 
            {
                { 2, 0, 0, 0 },
                { 4, 0, 2, 2 },
                { 0, 0, 0, 2 },
                { 0, 8, 4, 16 },
            };
            GameState gs = new GameState(test);
            Console.WriteLine(gs);
            Console.WriteLine(gs.EmptyCount);
            gs = gs.MakeMove(Moves.Left);
            Console.WriteLine(gs);
            Console.WriteLine(gs.EmptyCount);
            gs = gs.MakeMove(Moves.Right);
            Console.WriteLine(gs);
            Console.WriteLine(gs.EmptyCount);
            gs = gs.MakeMove(Moves.Up);
            Console.WriteLine(gs);
            Console.WriteLine(gs.EmptyCount);
            gs = gs.MakeMove(Moves.Down);
            Console.WriteLine(gs);
            Console.WriteLine(gs.EmptyCount);
        }
    }
}
