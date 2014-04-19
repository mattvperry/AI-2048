using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AI_2048
{
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
                // Keep track of maximum
                if (line[i] > line[maxIndex]) maxIndex = i;
                if(i > 0)
                {
                    // Look for line neighbors that are close to each other
                    if (line[i] == line[i - 1] + 1 || line[i] == line[i - 1] - 1) score += 1000.0f;
                }
            }
            // Maximum is at an end
            if (maxIndex == 0 || maxIndex == 3) score += 20000.0f;
            // Check if values are ordered
            if (line[0] < line[1] && line[1] < line[2] && line[2] < line[3]) score += 10000.0f;
            if (line[0] > line[1] && line[1] > line[2] && line[2] > line[3]) score += 10000.0f;
            return score;
        }
        #endregion
    }
}
