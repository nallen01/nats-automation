﻿using System;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace NatsAutomation
{
    public class LightingService
    {
        private static int PORT = 3332;
        private static int SOCKET_CONNECTION_TIMEOUT_MS = 1000;

        private TcpClient Client;
        private Stream ClientStream;

        public LightingService(String ip)
        {
            if (!MainForm.IGNORE_VISION)
            {
                try
                {
                    Client = new TcpClient();
                    if (!Client.ConnectAsync(ip, PORT).Wait(SOCKET_CONNECTION_TIMEOUT_MS))
                    {
                        throw new Exception("Connection timeout");
                    }

                    ClientStream = Client.GetStream();
                }
                catch (Exception)
                {
                    CleanUp();
                    throw new Exception("Unable to connect to server at " + ip + ":" + PORT);
                }
            }
        }

        public void CleanUp()
        {
            if (ClientStream != null)
                ClientStream.Close();
            if (Client != null)
                Client.Close();
        }

        public void RunSequence(int index)
        {
            if (Client != null)
            {
                FreeStylerPacket data = new FreeStylerPacket()
                {
                    Code = 504 + index,
                    TCPIPArgument = 255,
                    Argument = 0
                };

                SendFreeStylerPacket(data);
            }
        }

        public Boolean SendFreeStylerPacket(FreeStylerPacket packet)
        {
            if (packet.Code >= 0 && packet.Code < 1000 && packet.TCPIPArgument >= 0 && packet.TCPIPArgument < 1000 && packet.Argument >= 0 && packet.Argument < 1000)
            {
                String command = String.Format("FSOC{0:D3}{1:D3}{2:D3}", packet.Code, packet.TCPIPArgument, packet.Argument);

                ClientStream.Write(ASCIIEncoding.ASCII.GetBytes(command), 0, command.Length);

                return true;
            }

            return false;
        }

        public class FreeStylerPacket
        {
            public int Code;
            public int TCPIPArgument;
            public int Argument;
        }
    }
}
