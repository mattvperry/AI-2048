using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.ServiceModel.Description;

using AI_2048.Server;

namespace AI_2048
{
    /// <summary>
    /// WCF Client wrapper for the AI-2048 WCF Service
    /// </summary>
    public class GameClient : IDisposable
    {
        #region Properties
        private GameServer server = null;
        private GameServer Server
        {
            get
            {
                if(server == null)
                {
                    server = new GameServer();
                }
                return server;
            }
        }

        private Uri baseAddress = null;
        private Uri BaseAddress
        {
            get
            {
                if(baseAddress == null)
                {
                    baseAddress = new Uri(string.Format(GameServer.ADDRESS_FMT, "localhost", Server.Port));
                }
                return baseAddress;
            }
        }

        private ChannelFactory<IGameService> channelFactory = null;
        private ChannelFactory<IGameService> ChannelFactory
        {
            get
            {
                if(channelFactory == null)
                {
                    channelFactory = new ChannelFactory<IGameService>(new WebHttpEndpoint(
                        ContractDescription.GetContract(typeof(IGameService)),
                        new EndpointAddress(BaseAddress)
                    ));
                }
                return channelFactory;
            }
        }

        private IGameService client = null;
        private IGameService Client
        {
            get
            {
                if(client == null)
                {
                    client = ChannelFactory.CreateChannel();
                }
                return client;
            }
        }
        #endregion

        #region Game Methods
        /// <summary>
        /// Read the current game state
        /// </summary>
        public long[][] GetGameState()
        {
            return Client.GetGameState();
        }

        /// <summary>
        /// Make a move in the game
        /// </summary>
        /// <param name="key">Key to press</param>
        public void MakeMove(Moves move)
        {
            Client.MakeMove(((int)move).ToString());
        }

        /// <summary>
        /// Gets the current game score
        /// </summary>
        /// <returns>Score</returns>
        public long CurrentScore()
        {
            return Client.CurrentScore();
        }

        /// <summary>
        /// Restarts the game
        /// </summary>
        public void RestartGame()
        {
            Client.RestartGame();
        }

        /// <summary>
        /// Is the game over?
        /// </summary>
        /// <returns>Game terminated state</returns>
        public bool IsGameOver()
        {
            return Client.IsGameOver();
        }

        /// <summary>
        /// Controls whether or not to go past 2048
        /// </summary>
        /// <param name="keep">Continue past 2048?</param>
        public void KeepPlaying(bool keep)
        {
            Client.KeepPlaying(keep.ToString());
        }
        #endregion

        #region IDisposable
        public void Dispose()
        {
            if(server != null)
            {
                Server.Dispose();
            }
        }
        #endregion
    }
}
