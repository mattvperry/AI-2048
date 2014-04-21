using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace AI_2048
{
    /// <summary>
    /// AI code to decide which moves to make in the game
    /// </summary>
    public class GameAI
    {
        #region Static Data
        /// <summary>
        /// Look up table for row score data
        /// </summary>
        private static float[] RowScores = new float[ushort.MaxValue];

        /// <summary>
        /// Static constructor for populating row score lookup data
        /// </summary>
        static GameAI()
        {
            for(ushort row = 0; row < ushort.MaxValue; ++row)
            {
                RowScores[row] = ScoreRow(row);
            }
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
        private static float ScoreRow(ushort row)
        {
           // Split row into individual values
            byte[] line = GameState.SplitRow(row);

            float score = 0.0f;
            int maxIndex = 0;
            for(int i = 0; i < 4; ++i)
            {
                // Empty space
                if (line[i] == 0) score += 10000.0f;
                if(i > 0)
                {
                    // Keep track of maximum
                    if (line[i] > line[maxIndex]) maxIndex = i;
                    // Look for line neighbors that are close to each other
                    if (Math.Abs(line[i] - line[i - 1]) == 1) score += 1000.0f;
                }
            }
            // Maximum is at an end
            if (maxIndex == 0 || maxIndex == 3) score += 20000.0f;
            // Check if values are ordered
            if ((line[0] < line[1]) && (line[1] < line[2]) && (line[2] < line[3])) score += 10000.0f;
            if ((line[0] > line[1]) && (line[1] > line[2]) && (line[2] > line[3])) score += 10000.0f;
            return score;
        }
        #endregion

        #region Properties
        /// <summary>
        /// Dont recurse into a node with a probability less than this
        /// </summary>
        private float probThreshold;

        /// <summary>
        /// Maximum depth to cache nodes
        /// </summary>
        private int cacheDepth;

        /// <summary>
        /// Maximum depth to search to
        /// </summary>
        private int searchDepth;
        #endregion

        #region Constructor
        /// <summary>
        /// Makes an instance of a Game AI
        /// </summary>
        /// <param name="probThreshhold">Dont recurse into a node with a probability less than this</param>
        /// <param name="cacheDepth">Maximum depth to cache nodes</param>
        /// <param name="searchDepth">Maximum depth to search to</param>
        public GameAI(float probThreshold = .0001f, int cacheDepth = 6, int searchDepth = 8)
        {
            this.probThreshold = probThreshold;
            this.cacheDepth = cacheDepth;
            this.searchDepth = searchDepth;
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

            /*
            float best = 0.0f;
            Moves bestMove = Moves.Up;

            foreach(Moves move in AllMoves())
            {
                float result = ScoreToplevelMove(gs, move);
                if(result > best)
                {
                    best = result;
                    bestMove = move;
                }
            }

            return bestMove;
            */

            return AllMoves().AsParallel()
                .ToDictionary(m => m, m => ScoreTopLevelMove(gs, m))
                .Aggregate((a, m) => a.Value > m.Value ? a : m).Key;
        }
        #endregion

        #region ExpectiMiniMax Implementation
        /// <summary>
        /// Score the entire board using the heuristic
        /// </summary>
        /// <param name="gs">Game State</param>
        /// <returns>Score of game state</returns>
        private float ScoreGameState(GameState gs)
        {
            float rowScore = gs.Rows.Sum(row => { return RowScores[row]; });
            float colScore = gs.Transpose().Rows.Sum(col => { return RowScores[col]; });
            return rowScore + colScore + 100000.0f;
        }

        /// <summary>
        /// Start the recursive expectiminimax algorithm. This will score the first move
        /// of a game.
        /// </summary>
        /// <param name="board">Current board state</param>
        /// <param name="move">Move to score</param>
        /// <returns>Score of move on board</returns>
        private float ScoreTopLevelMove(GameState board, Moves move)
        {
            AlgorithmState state = new AlgorithmState();
            Stopwatch stopwatch = new Stopwatch();

            stopwatch.Start();
            float score = ScoreTopLevelMove(state, board, move);
            stopwatch.Stop();

            Console.WriteLine(string.Format("Move {0}: result {1}: eval'd {2} moves ({3} cache hits, {4} cache size) in {5} milliseconds (maxdepth = {6})",
                move, score, state.MovesEvaled, state.CacheHits, state.TransTable.Count, stopwatch.Elapsed.Milliseconds, state.MaxDepth));

            return score;
        }

        private float ScoreTopLevelMove(AlgorithmState state, GameState board, Moves move)
        {
            GameState newBoard = board.MakeMove(move);
            // This move does nothing
            if (newBoard == board)
            {
                return 0.0f;
            }

            return ScoreRandomNode(state, newBoard, 1.0f) + 1e-6f;
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
        private float ScoreRandomNode(AlgorithmState state, GameState board, float cprob)
        {
            if (board.EmptyCount == 0) return 0.0f;

            cprob /= board.EmptyCount;
            float score = board.PossibleRandomChoices.Aggregate(0.0f, (acc, choice) =>
            {
                acc += ScoreMoveNode(state, choice.Place2, cprob * 0.9f) * 0.9f;
                acc += ScoreMoveNode(state, choice.Place4, cprob * 0.1f) * 0.1f;
                return acc;
            });
            return score / board.EmptyCount;
        }

        /// <summary>
        /// This will sum the score of the user's move choices
        /// </summary>
        /// <param name="state">Algorithm status and memory</param>
        /// <param name="board">Current board state</param>
        /// <param name="cprob">Cumulative probability of this node occuring</param>
        /// <returns>Score of a move node at this board state</returns>
        private float ScoreMoveNode(AlgorithmState state, GameState board, float cprob)
        {
            // Halt search on the following conditions:
            //  1) Probability of this state is below threshold
            //  2) We have passed the maximum search depth
            //  3) We have already seen this game state and know its score
            if(cprob < probThreshold || state.CurrDepth >= searchDepth)
            {
                if (state.CurrDepth > state.MaxDepth)
                    state.MaxDepth = state.CurrDepth;
                return ScoreGameState(board);
            }

            if(state.CurrDepth < cacheDepth && state.TransTable.ContainsKey(board))
            {
                state.CacheHits++;
                return state.TransTable[board];
            }

            // Recurse deeper for every possible move
            state.CurrDepth++;
            float best = AllMoves().Max(move =>
            {
                GameState newBoard = board.MakeMove(move);
                state.MovesEvaled++;
                return newBoard != board ? ScoreRandomNode(state, board, cprob) : 0.0f;
            });
            state.CurrDepth--;

            if(state.CurrDepth < cacheDepth)
            {
                state.TransTable[board] = best;
            }

            return best;
        }
        #endregion

        #region Helpers
        private IEnumerable<Moves> AllMoves()
        {
            return Enum.GetValues(typeof(Moves)).Cast<Moves>();
        }
        #endregion
    }

    /// <summary>
    /// Class representing the current state of the algorithm
    /// </summary>
    public class AlgorithmState
    {
        /// <summary>
        /// Transposition table which matches a gamestate to its score. Basically a cache
        /// </summary>
        public Dictionary<GameState, float> TransTable = new Dictionary<GameState,float>();

        /// <summary>
        /// The maximum depth that the algorithm has reached in any path
        /// </summary>
        public int MaxDepth = 0;

        /// <summary>
        /// The current depth of the algorithm
        /// </summary>
        public int CurrDepth = 0;

        /// <summary>
        /// Number of times a state has been found in the trans table
        /// </summary>
        public int CacheHits = 0;

        /// <summary>
        /// Total number of possible moves evaluated
        /// </summary>
        public int MovesEvaled = 0;
    }
}
