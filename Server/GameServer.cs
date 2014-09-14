using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ServiceModel;
using System.ServiceModel.Description;

namespace AI_2048.Server
{
    public class GameServer : IDisposable
    {
        public static string ADDRESS_FMT = "http://{0}:{1}/2048";

        public Uri BaseAddress { get; private set; }

        public int Port { get; private set; }

        private GameService service = null;
        private GameService Service
        {
            get
            {
                if (service == null)
                {
                    service = new GameService();
                }
                return service;
            }
        }

        private ServiceHost host = null;
        private ServiceHost Host
        {
            get
            {
                if (host == null)
                {
                    host = new ServiceHost(Service, BaseAddress);
                    host.AddServiceEndpoint(new WebHttpEndpoint(
                        ContractDescription.GetContract(typeof(IGameService), Service),
                        new EndpointAddress(BaseAddress)
                    ));
                }
                return host;
            }
        }

        public GameServer(int port = 8001)
        {
            Port = port;
            BaseAddress = new Uri(string.Format(ADDRESS_FMT, "0.0.0.0", Port));
            Start();
        }

        public void Dispose()
        {
            if (host != null && Host.State != CommunicationState.Closed)
            {
                Stop();
            }
            if (service != null)
            {
                Service.Dispose();
            }
        }

        private void Start()
        {
            Host.Open();
        }

        private void Stop()
        {
            Host.Close();
        }
    }
}
