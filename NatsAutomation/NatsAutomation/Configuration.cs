using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NatsAutomation
{
    class Configuration
    {
        public String EventManagerIP;
        public String EventManagerUsername;
        public String EventManagerPassword;

        public int NumLightingServers;
        public List<String> LightingIPs;
        public List<LightingEntry> LightingEntries;

        public int NumVisionServers;
        public List<String> VisionIPs;
        public List<VisionEntry> VisionEntries;

        public Configuration(String[] lines)
        {
            ParseConfigurationData(lines);
        }

        public Configuration(String data)
        {
            ParseConfigurationData(data.Split('\n'));
        }

        private void ParseConfigurationData(String[] lines)
        {
            this.NumLightingServers = 0;
            this.LightingIPs = new List<String>();
            this.LightingEntries = new List<LightingEntry>();
            this.NumVisionServers = 0;
            this.VisionIPs = new List<String>();
            this.VisionEntries = new List<VisionEntry>();

            for (int i = 0; i < lines.Length; i++)
            {
                if(!lines[i].StartsWith("#"))
                {
                    String[] parts = lines[i].Split(new string[] { "\t" }, StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim()).ToArray();

                    if(parts.Length > 0) {
                        if (parts[0].Equals("em_ip"))
                        {
                            if(parts.Length == 2) {
                                this.EventManagerIP = parts[1];
                            }
                        }
                        else if (parts[0].Equals("em_username"))
                        {
                            if (parts.Length == 2)
                            {
                                this.EventManagerUsername = parts[1];
                            }
                        }
                        else if (parts[0].Equals("em_password"))
                        {
                            if (parts.Length == 2)
                            {
                                this.EventManagerPassword = parts[1];
                            }
                        }
                        else if (parts[0].Equals("lighting_ip"))
                        {
                            if (parts.Length == 2)
                            {
                                this.LightingIPs.Add(parts[1]);
                                this.NumLightingServers++;
                            }
                        }
                        else if (parts[0].Equals("lighting_entry"))
                        {
                            if (parts.Length == 5)
                            {
                                int serverIndex = 0;
                                int sequenceNumber = 0;

                                if (!Int32.TryParse(parts[1], out serverIndex))
                                    throw new Exception("Unknown Server Index '" + parts[2] + "' on line " + i);

                                if (!Int32.TryParse(parts[4], out sequenceNumber))
                                    throw new Exception("Unknown Sequence Number '" + parts[5] + "' on line " + i);

                                this.LightingEntries.Add(new LightingEntry()
                                {
                                    ServerIndex = serverIndex,
                                    DivisionName = parts[2],
                                    FieldName = parts[3],
                                    SequenceNumber = sequenceNumber
                                });
                            }
                        }
                        else if (parts[0].Equals("vision_ip"))
                        {
                            if (parts.Length == 2)
                            {
                                this.VisionIPs.Add(parts[1]);
                                this.NumVisionServers++;
                            }
                        }
                        else if (parts[0].Equals("vision_entry"))
                        {
                            if (parts.Length == 5)
                            {
                                int serverIndex = 0;
                                int macroNumber = 0;

                                if (!Int32.TryParse(parts[1], out serverIndex))
                                    throw new Exception("Unknown Server Index '" + parts[2] + "' on line " + i);

                                if (!Int32.TryParse(parts[4], out macroNumber))
                                    throw new Exception("Unknown Macro Number '" + parts[5] + "' on line " + i);

                                this.VisionEntries.Add(new VisionEntry()
                                {
                                    ServerIndex = serverIndex,
                                    DivisionName = parts[2],
                                    FieldName = parts[3],
                                    MacroNumber = macroNumber
                                });
                            }
                        }
                    }
                }
            }
        }
    }

    public class LightingEntry
    {
        public int ServerIndex;
        public String DivisionName;
        public String FieldName;
        public int SequenceNumber;
    }

    public class VisionEntry
    {
        public int ServerIndex;
        public String DivisionName;
        public String FieldName;
        public int MacroNumber;
    }
}
