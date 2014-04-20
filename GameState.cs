using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AI_2048
{
    /// <summary>
    /// Class representing a state of the game
    /// 
    /// We are representing the entire game board as a 64 bit integer.
    /// There are 16 cells in the game grid which implies that we have 4 bits
    /// to represent the value of the cell. Since this game uses powers of 2
    /// we can say that these 4 bits are the power of two that lies in the cell.
    /// This gives us the limitation that we can only have cells up to 2^15.
    /// This is ok because achieving 2^16+ in this game is very unlikely.
    /// 
    /// The other interesting result following from this representation is that
    /// any row can be represented by 16 bits. It follows that there are exactly
    /// 65536 row combinations possible. With such a small space, we can enumerate
    /// every possible row and all possible moves on that row and the resulting score.
    /// 
    /// The space and computation advantage of these pre computed look up tables far
    /// out weighs the limitations imposed by them.
    /// </summary>
    public class GameState
    {
        #region Static Data
        /// <summary>
        /// Look up table for right moves
        /// </summary>
        private static ushort[] RightLookup = new ushort[ushort.MaxValue];

        /// <summary>
        /// Look up table for left moves
        /// </summary>
        private static ushort[] LeftLookup = new ushort[ushort.MaxValue];

        /// <summary>
        /// Static constructor for populating row move lookup data
        /// </summary>
        static GameState()
        {
            // Iterate over all possible rows
            for(ushort row = 0; row < ushort.MaxValue; ++row)
            {
                ushort revRow = ReverseRow(row);
                ushort moveResult = MoveRight(row);
                ushort revMoveResult = ReverseRow(moveResult);

                RightLookup[row] = (ushort)(row ^ moveResult);
                LeftLookup[revRow] = (ushort)(revRow ^ revMoveResult);
            }
        }

        /// <summary>
        /// Execute a right move on a given row
        /// </summary>
        /// <param name="row">Row to move</param>
        /// <returns>New row after move</returns>
        private static ushort MoveRight(ushort row)
        {
            // Split row into individual values before making move
            byte[] line = SplitRow(row);

            // Iterate over row from the right side
            for(int i = 3; i > 0; --i)
            {
                // Find the first non zero element to the left
                // of the rightmost value
                int j;
                for(j = i - 1; j >= 0; --j)
                {
                    if (line[j] != 0) break;
                }
                if (j == -1) break;

                // If the rightmost element is zero
                if(line[i] == 0)
                {
                    // Move element at position j to position i
                    line[i] = line[j];
                    line[j] = 0;
                    i++; // Retry this element
                }
                // If the rightmost element equals its closest neighbor
                // and it is not already the maximum value
                else if(line[i] == line[j] && line[i] != 0xF)
                {
                    // Bump position i up one
                    line[i]++;
                    // Remove position j
                    line[j] = 0;
                }
            }

            // Reconstruct row from parts
            return PackRow(line);
        }
        #endregion

        #region Static Helpers
        /// <summary>
        /// Split ushort representation of row into byte array
        /// </summary>
        /// <param name="row">Row as ushort</param>
        /// <returns>Row as byte array</returns>
        public static byte[] SplitRow(ushort row)
        {
            return new byte[]
            {
                (byte)((row >> 12) & 0xF),
                (byte)((row >> 8) & 0xF),
                (byte)((row >> 4) & 0xF),
                (byte)(row & 0xF),
            };
        }

        /// <summary>
        /// Combine byte array row into ushort
        /// </summary>
        /// <param name="row">Row as byte array</param>
        /// <returns>Row as ushort</returns>
        public static ushort PackRow(byte[] row)
        {
            return (ushort)((row[0] << 12) | (row[1] << 8) | (row[2] << 4) | row[3]);
        }

        /// <summary>
        /// Reverse a given row
        /// </summary>
        /// <param name="row">Row to reverse</param>
        /// <returns>Reversed row</returns>
        public static ushort ReverseRow(ushort row)
        {
            return (ushort)((row >> 12) | ((row >> 4) & 0x00F0) | ((row << 4) & 0x0F00) | (row << 12));
        }

        /// <summary>
        /// Since we are storing each element as a power of 2, this method
        /// will convert the element to the actual value in the grid
        /// </summary>
        /// <param name="power">Power of 2</param>
        /// <returns>2 ^ power</returns>
        public static int RealValue(byte power)
        {
            if (power == 0) return 0;
            return (int)Math.Pow(2.0, (double)power);
        }

        /// <summary>
        /// Transpose the board so columns become rows and vice versa
        ///   0123       048c
        ///   4567  -->  159d
        ///   89ab       26ae
        ///   cdef       37bf
        /// Bitwise operator magic...
        /// </summary>
        /// <param name="board">Original board</param>
        /// <returns>Transposed board</returns>
        public static ulong TransposeBoard(ulong board)
        {
            ulong a1 = board & 0xF0F00F0FF0F00F0F;
            ulong a2 = board & 0x0000F0F00000F0F0;
            ulong a3 = board & 0x0F0F00000F0F0000;
            ulong a = a1 | (a2 << 12) | (a3 >> 12);
            ulong b1 = a & 0xFF00FF0000FF00FF;
            ulong b2 = a & 0x00FF00FF00000000;
            ulong b3 = a & 0x00000000FF00FF00;
            return b1 | (b2 >> 24) | (b3 << 24);
        }

        /// <summary>
        /// Function which will yield each row to a delegate which will return
        /// that row after a move. This function will apply that move to the board
        /// </summary>
        /// <param name="board">Board to start with</param>
        /// <param name="getMove">Delegate which will transform each row</param>
        /// <returns>New board after move</returns>
        private static ulong Move(ulong board, ushort[] lookup)
        {
            for (int i = 0; i <= 48; i += 16)
            {
                ushort row = (ushort)((board >> i) & 0xFFFF);
                board ^= ((ulong)lookup[row]) << i;
            }
            return board;
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Constructor which takes a 2d array of a board
        /// </summary>
        /// <param name="grid">2d array of board</param>
        public GameState(long[,] grid)
        {
            for (int i = 0; i < 4; ++i)
            {
                for (int j = 0; j < 4; ++j)
                {
                    board |= grid[i,j] == 0 ? 0 : (ulong)Math.Log(grid[i,j], 2);
                    if (i == 3 && j == 3) break;
                    board <<= 4;
                }
            }
        }

        /// <summary>
        /// Constructor which takes a 64 bit int representation of a board
        /// </summary>
        /// <param name="board">64-bit int board</param>
        private GameState(ulong board)
        {
            this.board = board;
        }
        #endregion

        #region Properties
        /// <summary>
        /// Representation of the board as a 64-bit int
        /// </summary>
        private ulong board = 0;

        /// <summary>
        /// Overloaded indexer for retrieving grid element
        /// </summary>
        /// <param name="x">X pos</param>
        /// <param name="y">Y pos</param>
        /// <returns>Element at x, y</returns>
        public int this[int x, int y]
        {
            get
            {
                if (x < 0 || x > 3 || y < 0 || y > 3)
                {
                    throw new IndexOutOfRangeException();
                }
                int shift = 60 - (4 * (4 * x + y));
                return RealValue((byte)((this.board >> shift) & 0xF));
            }
        }

        /// <summary>
        /// List generator for all elements on the board
        /// </summary>
        public IEnumerable<int> Elements
        {
            get
            {
                for (int i = 0; i < 16; ++i)
                {
                    yield return RealValue((byte)((this.board >> (4 * (15 - i))) & 0xF));
                }
            }
        }

        /// <summary>
        /// List generator for all rows on the board
        /// </summary>
        public IEnumerable<ushort> Rows
        {
            get
            {
                for (int i = 0; i <= 48; i += 16)
                {
                    yield return (ushort)((this.board >> i) & 0xFFFF);
                }
            }
        }

        /// <summary>
        /// Enumerate all possible random choices on the current board for
        /// the next new tile
        /// </summary>
        public IEnumerable<RandomChoice> PossibleRandomChoices
        {
            get
            {
                ulong tile = 1;
                foreach(var element in Elements.Reverse())
                {
                    if(element == 0)
                    {
                        yield return new RandomChoice()
                        {
                            Place2 = new GameState(this.board | tile),
                            Place4 = new GameState(this.board | (tile << 1))
                        };
                    }

                    tile <<= 4;
                }
            }
        }

        /// <summary>
        /// Returns the number empty elements on the board
        /// </summary>
        public int EmptyCount
        {
            get
            {
                ulong x = this.board | ((this.board >> 2) & 0x3333333333333333);
                x |= (x >> 1);
                x = ~x & 0x1111111111111111;
                // At this point each nibble is:
                //  0 if the original was non-zero
                //  1 if the original was zero
                for(int i = 32; i >= 4; i /= 2)
                {
                    x += x >> i;
                }
                return (int)(x & 0xF);
            }
        }
        #endregion

        #region Public Interface
        /// <summary>
        /// Perform the given move on the board
        /// </summary>
        /// <param name="move">Move direction</param>
        /// <returns>Resulting game state</returns>
        public GameState MakeMove(Moves move)
        {
            ulong newBoard;
            switch(move)
            {
                case Moves.Right:
                    newBoard = MoveRight();
                    break;
                case Moves.Left:
                    newBoard = MoveLeft();
                    break;
                case Moves.Up:
                    newBoard = MoveUp();
                    break;
                case Moves.Down:
                    newBoard = MoveDown();
                    break;
                default:
                    throw new Exception("Unknown move direction");
            }
            return new GameState(newBoard);
        }

        /// <summary>
        /// Return the transposition of this game state
        /// </summary>
        /// <returns>Transposed game state</returns>
        public GameState Transpose()
        {
            return new GameState(TransposeBoard(this.board));
        }

        /// <summary>
        /// Print the current game state
        /// </summary>
        /// <returns>String representation of the current board</returns>
        public override string ToString()
        {
            // Find element with longest digit count
            int maxDigits = (int)Elements.Max(element => { return (double)element.ToString().Length; });

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < 4; ++i)
            {
                for (int j = 0; j < 4; ++j)
                {
                    sb.AppendFormat("{0} ", this[i, j].ToString().PadLeft(maxDigits));
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }

        /// <summary>
        /// Override equals method
        /// </summary>
        /// <param name="obj">Object to compare to</param>
        /// <returns>True of obj and this game state represent the same state</returns>
        public override bool Equals(object obj)
        {
            GameState gs = obj as GameState;
            if ((object)gs == null) return false;
            return this == gs;
        }

        /// <summary>
        /// Overload equals operator
        /// </summary>
        /// <param name="a">Left hand game state</param>
        /// <param name="b">Right hand game state</param>
        /// <returns>True if a and b represent the same state</returns>
        public static bool operator ==(GameState a, GameState b)
        {
            if(ReferenceEquals(a, b))
            {
                return true;
            }

            if((object)a == null || (object)b == null)
            {
                return false;
            }

            return a.board == b.board;
        }

        /// <summary>
        /// Overload not equals operator
        /// </summary>
        /// <param name="a">Left hand game state</param>
        /// <param name="b">Right hand game state</param>
        /// <returns>True if a and b represent different states</returns>
        public static bool operator !=(GameState a, GameState b)
        {
            return !(a == b);
        }

        /// <summary>
        /// Hash the game state
        /// </summary>
        /// <returns>Game State hash</returns>
        public override int GetHashCode()
        {
            return board.GetHashCode();
        }
        #endregion

        #region Game Move Helpers
        /// <summary>
        /// Execute a right move on the game
        /// </summary>
        /// <returns>Resulting game state</returns>
        private ulong MoveRight()
        {
            return Move(this.board, RightLookup);
        }

        /// <summary>
        /// Execute a right move on the game
        /// </summary>
        /// <returns>Resulting game state</returns>
        private ulong MoveLeft()
        {
            return Move(this.board, LeftLookup);
        }

        /// <summary>
        /// Execute a right move on the game
        /// </summary>
        /// <returns>Resulting game state</returns>
        private ulong MoveUp()
        {
            ulong tBoard = TransposeBoard(this.board);
            tBoard = Move(tBoard, LeftLookup);
            return TransposeBoard(tBoard);
        }

        /// <summary>
        /// Execute a right move on the game
        /// </summary>
        /// <returns>Resulting game state</returns>
        private ulong MoveDown()
        {
            ulong tBoard = TransposeBoard(this.board);
            tBoard = Move(tBoard, RightLookup);
            return TransposeBoard(tBoard);
        }
        #endregion
    }

    /// <summary>
    /// Enumeration of possible moves
    /// </summary>
    public enum Moves
    {
        Up,
        Right,
        Down,
        Left,
    }

    /// <summary>
    /// Class representing the random choice on placing a new tile
    /// </summary>
    public class RandomChoice
    {
        /// <summary>
        /// GameState after placing a random 2
        /// </summary>
        public GameState Place2 { get; set; }

        /// <summary>
        /// GameState after placing a random 4
        /// </summary>
        public GameState Place4 { get; set; }
    }
}
