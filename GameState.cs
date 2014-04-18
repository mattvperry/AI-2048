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
        /// Maximum value of a 16 bit integer
        /// </summary>
        private const int MAX_SHORT = ushort.MaxValue;

        /// <summary>
        /// Look up table for row data
        /// </summary>
        private static RowData[] RowLookup = new RowData[MAX_SHORT];

        /// <summary>
        /// Static constructor for populating row lookup data
        /// </summary>
        static GameState()
        {
            // Iterate over all possible rows
            for(ushort row = 0; row < MAX_SHORT; ++row)
            {
                // Split row into individual values
                byte[] line = 
                {
                    (byte)((row >> 12) & 0xF),
                    (byte)((row >> 8) & 0xF),
                    (byte)((row >> 4) & 0xF),
                    (byte)(row & 0xF),
                };

                ushort moveRightResult = MoveRight(line);
                RowLookup[row] = new RowData()
                {
                    Score = ScoreRow(line),
                    MoveRight = moveRightResult,
                    MoveLeft = ReverseRow(moveRightResult),
                };
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
        private static float ScoreRow(byte[] row)
        {
            float score = 0.0f;
            int maxIndex = 0;
            for(int i = 0; i < 4; ++i)
            {
                // Empty space
                if (row[i] == 0) score += 10000.0f;
                // Keep track of maximum
                if (row[i] > row[maxIndex]) maxIndex = i;
                if(i > 0)
                {
                    // Look for row neighbors that are close to each other
                    if (row[i] == row[i - 1] + 1 || row[i] == row[i - 1] - 1) score += 1000.0f;
                }
            }
            // Maximum is at an end
            if (maxIndex == 0 || maxIndex == 3) score += 20000.0f;
            // Check if values are ordered
            if (row[0] < row[1] && row[1] < row[2] && row[2] < row[3]) score += 10000.0f;
            if (row[0] > row[1] && row[1] > row[2] && row[2] > row[3]) score += 10000.0f;
            return score;
        }

        /// <summary>
        /// Execute a right move on a given row
        /// </summary>
        /// <param name="row">Row to move</param>
        /// <returns>New row after move</returns>
        private static ushort MoveRight(byte[] row)
        {
            // Iterate over row from the right side
            for(int i = 3; i > 0; --i)
            {
                // Find the first non zero element to the left
                // of the rightmost value
                int j;
                for(j = i - 1; j >= 0; --j)
                {
                    if (row[j] != 0) break;
                }
                if (j == -1) break;

                // If the rightmost element is zero
                if(row[i] == 0)
                {
                    // Move element at position j to position i
                    row[i] = row[j];
                    row[j] = 0;
                    i++; // Retry this element
                }
                // If the rightmost element equals its closest neighbor
                // and it is not already the maximum value
                else if(row[i] == row[j] && row[i] != 0xF)
                {
                    // Bump position i up one
                    row[i]++;
                    // Remove position j
                    row[j] = 0;
                }
            }

            // Reconstruct row from parts
            return (ushort)((row[0] << 12) | (row[1] << 8) | (row[2] << 4) | row[3]);
        }

        /// <summary>
        /// Reverse a given row
        /// </summary>
        /// <param name="row">Row to reverse</param>
        /// <returns>Reversed row</returns>
        private static ushort ReverseRow(ushort row)
        {
            return (ushort)((row >> 12) | ((row >> 4) & 0x00F0) | ((row << 4) & 0x0F00) | (row << 12));
        }
        #endregion
    }

    /// <summary>
    /// Small class to hold all data about a row
    /// </summary>
    public class RowData
    {
        /// <summary>
        /// Result of moving a row right
        /// </summary>
        public ushort MoveRight { get; set; }

        /// <summary>
        /// Result of moving a row left
        /// </summary>
        public ushort MoveLeft { get; set; }

        /// <summary>
        /// The heuristic score of a row
        /// </summary>
        public float Score { get; set; }
    }
}
