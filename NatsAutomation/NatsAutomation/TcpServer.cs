using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NatsAutomation
{
    class TcpServer
    {
        public static int DEFAULT_PORT = 5006;

        private EventHandler Listener;

        private Thread listenThread;

        public TcpServer(EventHandler Listener) {
            this.Listener = Listener;
        }

        public void run()
        {
            TcpListener listener = new TcpListener(IPAddress.Any, DEFAULT_PORT);

            listener.Start();

            listenThread = new Thread(() =>
            {
                while (true)
                {
                    TcpClient client = listener.AcceptTcpClient();

                    new Thread(() =>
                    {
                        new TcpThread(client, Listener).run();
                    }).Start();
                }
            });
            listenThread.Start();
        }
    }
}
