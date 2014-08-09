using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace AI_2048
{
    /// <summary>
    /// Game AI constants which affect the algorithms efficiency and accuracy
    /// </summary>
    public static class GameAIConstants
    {
        // Heuristic weight constants
        public const double SCORE_LOST_PENALTY = 200000.0;
        public const double SCORE_MONOTONICITY_WEIGHT = -47.0;
        public const double SCORE_SUM_WEIGHT = -11.0;
        public const double SCORE_MERGES_WEIGHT = 700.0;
        public const double SCORE_EMPTY_WEIGHT = 270.0;

        // Heuristic scaling constants
        public const double SCORE_SUM_POWER = 3.5;
        public const double SCORE_MONOTONICITY_POWER = 4.0;

        /// <summary>
        /// Dont recurse into a node with a probability less than this
        /// </summary>
        public const double CPROB_THRESHOLD = 0.0001;

        /// <summary>
        /// Maximum depth to cache nodes
        /// </summary>
        public const int CACHE_DEPTH = 6;
    }

    /// <summary>
    /// AI code to decide which moves to make in the game
    /// </summary>
    public class GameAI
    {
        #region Static Data
        /// <summary>
        /// Look up table for row score data
        /// </summary>
        private static double[] RowScores;

        /// <summary>
        /// Static constructor for populating row score lookup data
        /// </summary>
        static GameAI()
        {
            RowScores = Enumerable.Range(0, ushort.MaxValue).Select(r => ScoreRow((ushort)r)).ToArray();
        }

        /// <summary>
        /// Score the given row based on the following heuristic:
        /// 
        /// 10000 Points for every empty space
        /// 20000 Points if the maximum value in the row is at an end
        /// 1000 Points for every value that is next to a value of 1 less exponent
        /// 10000 Points if the row is sorted
        /// </summary>
        /// <param name="row">Row to score</param>
        /// <returns>Heuristic score of the row</returns>
        private static double ScoreRow(ushort row)
        {
           // Split row into individual values
            byte[] line = GameState.SplitRow(row);

            // Count empty tiles
            int empties = line.Count(rank => rank == 0);

            // Sum all tile ranks
            double sum = line.Select(rank => Math.Pow((double)rank, GameAIConstants.SCORE_SUM_POWER)).Sum();

            // Calculate monotonicity from left to right and from right to left
            Func<double, double> monoPower = (x) => Math.Pow(x, GameAIConstants.SCORE_MONOTONICITY_POWER);
            var monoValues = line.Zip(line.Skip(1), (first, second) => monoPower(first) - monoPower(second));
            var monoLeft = monoValues.Where(v => v > 0).Sum();
            var monoRight = monoValues.Where(v => v < 0).Sum() * -1;
            var monotonicity = Math.Min(monoLeft, monoRight);

            // Count available merges
            var rowValues = line.Where(tile => tile != 0);
            var equalPairs = rowValues.Zip(rowValues.Skip(1), (first, second) => Tuple.Create(first, second)).Where(t => t.Item1 == t.Item2);
            var merges = equalPairs.Count() + equalPairs.Distinct().Count();

            var weights = new List<double> 
            { 
                GameAIConstants.SCORE_EMPTY_WEIGHT, 
                GameAIConstants.SCORE_MERGES_WEIGHT, 
                GameAIConstants.SCORE_MONOTONICITY_WEIGHT, 
                GameAIConstants.SCORE_SUM_WEIGHT 
            };
            var heuristics = new List<double> { empties, merges, monotonicity, sum };

            return weights.Zip(heuristics, (w, h) => w * h).Sum() + GameAIConstants.SCORE_LOST_PENALTY;
        }
        #endregion

        #region Public Interface
        /// <summary>
        /// Find the next best move
        /// </summary>
        /// <returns>Next move to make</returns>
        public Moves FindBestMove(GameState gs)
        {
            Console.WriteLine(gs);
            Console.WriteLine("Current Score: {0}", ScoreGameState(gs));

            var scores = new Dictionary<Moves, double>();
            Parallel.ForEach(AllMoves(), move =>
            {
                scores[move] = ScoreTopLevelMove(gs, move);
            });
            return scores.Aggregate((acc, score) => score.Value > acc.Value ? score : acc).Key;
        }
        #endregion

        #region ExpectiMiniMax Implementation
        /// <summary>
        /// Score the entire board using the heuristic
        /// </summary>
        /// <param name="gs">Game State</param>
        /// <returns>Score of game state</returns>
        private double ScoreGameState(GameState gs)
        {
            return  gs.Rows.Sum(row => RowScores[row]) +
                    gs.Transpose().Rows.Sum(col => RowScores[col]);
        }

        /// <summary>
        /// Start the recursive expectiminimax algorithm. This will score the first move
        /// of a game.
        /// </summary>
        /// <param name="board">Current board state</param>
        /// <param name="move">Move to score</param>
        /// <returns>Score of move on board</returns>
        private double ScoreTopLevelMove(GameState board, Moves move)
        {
            AlgorithmState state = new AlgorithmState(board);

            Stopwatch stopwatch = Stopwatch.StartNew();
            GameState newBoard = board.MakeMove(move);
            double score = newBoard != board ? ScoreRandomNode(state, newBoard, 0, 1.0) : 0.0;
            stopwatch.Stop();

            PrintStatus(move, score, state, stopwatch.Elapsed);

            return score;
        }

        /// <summary>
        /// This will add the random element of the game to the algorithm.
        ///  A 2 is placed 90% of the time
        ///  A 4 is placed 10% of the time
        /// </summary>
        /// <param name="state">Algorithm status and memory</param>
        /// <param name="board">Current board state</param>
        /// <param name="cprob">Cumulative probability of this node occuring</param>
        /// <returns>Score of a random node at this board state</returns>
        private double ScoreRandomNode(AlgorithmState state, GameState board, int depth, double cprob)
        {
            // Halt search on the following conditions:
            //  1) Probability of this state is below threshold
            //  2) We have passed the maximum search depth
            //  3) We have already seen this game state and know its score
            if(cprob < GameAIConstants.CPROB_THRESHOLD || depth >= state.DepthLimit)
            {
                state.MaxDepth = Math.Max(state.MaxDepth, depth);
                return ScoreGameState(board);
            }

            if(depth < GameAIConstants.CACHE_DEPTH && state.TransTable.ContainsKey(board))
            {
                state.CacheHits++;
                return state.TransTable[board];
            }

            cprob /= board.EmptyCount;
            double score = board.PossibleRandomChoices.AsParallel().Aggregate(0.0, (acc, choice) =>
            {
                acc += ScoreMoveNode(state, choice.Place2, depth, cprob * 0.9) * 0.9;
                acc += ScoreMoveNode(state, choice.Place4, depth, cprob * 0.1) * 0.1;
                return acc;
            }) / board.EmptyCount;

            if(depth < GameAIConstants.CACHE_DEPTH)
            {
                state.TransTable[board] = score;
            }

            return score;
        }

        /// <summary>
        /// This will sum the score of the user's move choices
        /// </summary>
        /// <param name="state">Algorithm status and memory</param>
        /// <param name="board">Current board state</param>
        /// <param name="cprob">Cumulative probability of this node occuring</param>
        /// <returns>Score of a move node at this board state</returns>
        private double ScoreMoveNode(AlgorithmState state, GameState board, int depth, double cprob)
        {
            // Recurse deeper for every possible move
            return AllMoves().Max(move =>
            {
                GameState newBoard = board.MakeMove(move);
                state.MovesEvaled++;
                return newBoard != board ? ScoreRandomNode(state, newBoard, depth + 1, cprob) : 0.0;
            });
        }
        #endregion

        #region Helpers
        private IEnumerable<Moves> AllMoves()
        {
            return Enum.GetValues(typeof(Moves)).Cast<Moves>();
        }

        private void PrintStatus(Moves move, double score, AlgorithmState state, TimeSpan elapsedTime)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(string.Format("Move {0} evaluated:", move));
            sb.AppendLine(string.Format("  Move score: {0}", score));
            sb.AppendLine(string.Format("  Moves evaluated: {0}", state.MovesEvaled));
            sb.AppendLine(string.Format("  Cache: {0} hits, {1} size", state.CacheHits, state.TransTable.Count));
            sb.AppendLine(string.Format("  Search: {0} deep, {1} max depth", state.MaxDepth, state.DepthLimit));
            sb.AppendLine(string.Format("  Time elapsed: {0}", elapsedTime));

            Console.WriteLine(sb);
            Console.WriteLine();
        }
        #endregion
    }

    /// <summary>
    /// Class representing the current state of the algorithm
    /// </summary>
    public class AlgorithmState
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="board">Starting board</param>
        public AlgorithmState(GameState board)
        {
            DepthLimit = Math.Max(3, board.DistinctTiles - 2);
        }

        /// <summary>
        /// Transposition table which matches a gamestate to its score. Basically a cache
        /// </summary>
        public ConcurrentDictionary<GameState, double> TransTable = new ConcurrentDictionary<GameState,double>();

        /// <summary>
        /// The maximum depth that the algorithm has reached in any path
        /// </summary>
        public int MaxDepth = 0;

        /// <summary>
        /// Number of times a state has been found in the trans table
        /// </summary>
        public int CacheHits = 0;

        /// <summary>
        /// Total number of possible moves evaluated
        /// </summary>
        public int MovesEvaled = 0;

        /// <summary>
        /// Search depth limit
        /// </summary>
        public int DepthLimit = 0;
    }
}
