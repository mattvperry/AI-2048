using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using System.ServiceModel.Web;

namespace AI_2048.Server
{
    [ServiceContract]
    public interface IGameService
    {

        /// <summary>
        /// Read the current game state
        /// </summary>
        [OperationContract]
        [WebGet(RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, UriTemplate = "/state")]
        long[][] GetGameState();

        /// <summary>
        /// Make a move in the game
        /// </summary>
        /// <param name="key">Key to press</param>
        [OperationContract]
        [WebGet(RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, UriTemplate = "/move/{move}")]
        void MakeMove(string move);

        /// <summary>
        /// Gets the current game score
        /// </summary>
        /// <returns>Score</returns>
        [OperationContract]
        [WebGet(RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, UriTemplate = "/score")]
        long CurrentScore();

        /// <summary>
        /// Restarts the game
        /// </summary>
        [OperationContract]
        [WebGet(RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, UriTemplate = "/restart")]
        void RestartGame();

        /// <summary>
        /// Is the game over?
        /// </summary>
        /// <returns>Game terminated state</returns>
        [OperationContract]
        [WebGet(RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, UriTemplate = "/game_over")]
        bool IsGameOver();

        /// <summary>
        /// Controls whether or not to go past 2048
        /// </summary>
        /// <param name="keep">Continue past 2048?</param>
        [OperationContract]
        [WebGet(RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, UriTemplate = "/keep_playing/{keep}")]
        void KeepPlaying(string keep);
    }
}
