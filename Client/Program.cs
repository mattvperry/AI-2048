using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AI_2048.Server;
using CommandLine;

namespace AI_2048
{
    class Program
    {
        static void Main(string[] args)
        {
            var options = new Options();
            if (Parser.Default.ParseArguments(args, options))
            {
                switch (options.AIMode)
                {
                    case AIMode.Client:
                        ClientMode(options.KeepPlaying);
                        break;
                    case AIMode.Server:
                        ServerMode(options.Port);
                        break;
                }
            }
        }

        static void ClientMode(bool keepPlaying)
        {
            var ai = new GameAI();
            using (var client = new GameClient())
            {
                client.KeepPlaying(keepPlaying);
                while (true)
                {
                    GameState gs = new GameState(client.GetGameState());
                    Moves move = ai.FindBestMove(gs);
                    client.MakeMove(move);

                    if (client.IsGameOver())
                    {
                        Console.WriteLine("Final game score: {0}", client.CurrentScore());
                        Console.Write("Restart game? [Y/n]: ");
                        string input = Console.ReadLine().ToLower();
                        if (input == "n") break;

                        client.RestartGame();
                    }
                }
            }
        }

        static void ServerMode(int port)
        {
            using (var server = new GameServer(port))
            {
                Console.WriteLine("Game server is running at {0}.", server.BaseAddress);
                Console.WriteLine("Press <Enter> to stop server.");
                Console.ReadLine();
            }
        }
    }
}
