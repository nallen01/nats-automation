using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NatsAutomation
{
    class TcpThread
    {
        private TcpClient client;
        private Stream stream;
        private StreamReader reader;
        private StreamWriter writer;
        

        private EventHandler Listener;

        public TcpThread(TcpClient client, EventHandler Listener)
        {
            this.client = client;
            this.Listener = Listener;
        }

        private Boolean sendMessage(String message)
        {
            if (writer != null)
            {
                try
                {
                    writer.Write(message + '\n');
                    writer.Flush();
                    return true;
                }
                catch (Exception) { }
            }
            return false;
        }

        public void run()
        {
            stream = client.GetStream();
            reader = new StreamReader(stream);
            writer = new StreamWriter(stream);

            sendMessage("1");

            String[] parts;
            while (true)
            {
                String str = reader.ReadLine();
                if (str != null)
                {
                    parts = str.Split(new char[] { (char)29 });

                    if(parts.Length == 1)
                    {
                        int value = int.Parse(parts[0]);
                        DataEventArgs args = new DataEventArgs();
                        args.setDataType("fox");
                        args.setDivision(value-1);
                        Listener(this, args);
                    }
                }
            }
        }
    }
}
