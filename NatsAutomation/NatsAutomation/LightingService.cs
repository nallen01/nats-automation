using System;
using System.IO;
using System.Net.Sockets;
namespace NatsAutomation
{
    public class LightingService
    {
        private static int PORT = 3332;
        private static int SOCKET_CONNECTION_TIMEOUT_MS = 1000;

        private TcpClient Client;
        private StreamReader ClientIn;
        private StreamWriter ClientOut;

        public LightingService(String ip)
        {
            try
            {
                Client = new TcpClient();
                if (!Client.ConnectAsync(ip, PORT).Wait(SOCKET_CONNECTION_TIMEOUT_MS))
                {
                    throw new Exception("Connection timeout");
                }

                Stream ClientStream = Client.GetStream();
                ClientIn = new StreamReader(ClientStream);
                ClientOut = new StreamWriter(ClientStream);
            }
            catch (Exception)
            {
                CleanUp();
                throw new Exception("Unable to connect to server at " + ip + ":" + PORT);
            }
        }

        public void CleanUp()
        {
            if (Client != null)
                Client.Close();
            if (ClientIn != null)
                ClientIn.Close();
            if (ClientOut != null)
                ClientOut.Close();
        }
    }
}
