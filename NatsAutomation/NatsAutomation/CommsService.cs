using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace NatsAutomation
{
    public class CommsService
    {
        private static int PORT = 5555;
        private static int SOCKET_CONNECTION_TIMEOUT_MS = 1000;
        private static List<int> ValidAuthGroups = new List<int> { 1 };
        private static List<int> ShouldShowFox = new List<int> { 3 };

        private static String[] AudienceDisplayNames = new String[] {
            "None",
            "Intro",
            "In-Match",
            "Saved Match Results",
            "Rankings",
            "Logo",
            "Alliance Selection",
            "Elim Bracket",
            "Slides",
            "SC Rankings",
            "Schedule"
        };

        private String IP;
        private String Username;
        private String Password;
        private int AuthGroup;

        private EventHandler Listeners;

        private TcpClient Client;
        private StreamReader ClientIn;
        private StreamWriter ClientOut;

        private Boolean[] ReceiveFinished = { false, false, false };

        private List<String> Divisions;
        private Dictionary<int, List<int>> DivisionFields;

        private List<String> Fields;
        private List<int[]> QueuedMatch;
        private List<int> CurField;

        private List<int> AudienceDisplay;

        public CommsService()
        {
            Divisions = new List<String>();
            DivisionFields = new Dictionary<int, List<int>>();

            Fields = new List<String>();
            QueuedMatch = new List<int[]>();
            CurField = new List<int>();

            AudienceDisplay = new List<int>();
        }

        private static String GetStringMD5(String data)
        {
            byte[] encodedPassword = new UTF8Encoding().GetBytes(data);
            byte[] hash = ((HashAlgorithm)CryptoConfig.CreateFromName("MD5")).ComputeHash(encodedPassword);
            return BitConverter.ToString(hash).Replace("-", string.Empty).ToLower();
        }

        private void CleanUp()
        {
            if (Client != null)
                Client.Close();
            if (ClientIn != null)
                ClientIn.Close();
            if (ClientOut != null)
                ClientOut.Close();

            for (int i = 0; i < ReceiveFinished.Length; i++ )
            {
                ReceiveFinished[i] = false;
            }

            IP = null;
            Username = null;
            Password = null;
            AuthGroup = 0;

            Divisions.Clear();
            DivisionFields.Clear();

            Fields.Clear();
            QueuedMatch.Clear();
            CurField.Clear();

            AudienceDisplay.Clear();
        }


        private Boolean SendMessage(String message)
        {
            if (ClientOut != null)
            {
                try
                {
                    ClientOut.WriteLine(message);
                    ClientOut.Flush();

                    return true;
                }
                catch (Exception) { }
            }

            return false;
        }

        private void Listener()
        {
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                if (Client == null)
					return;

				while (true) {
					if(AuthGroup <= 0) {
						return;
					}

					String[] parts;
					try {
						String rcv = ClientIn.ReadLine();
						if(rcv != null) {
							parts = rcv.Split((char)29);
							if(parts.Length > 0) {
                                Boolean valid = false;
                                int division = -1;

                                if (parts[0].Equals("2"))
                                {
                                    ReceiveFinished[0] = true;
                                }
                                else if (parts[0].Equals("3"))
                                {
                                    ReceiveFinished[1] = true;
                                }
                                else if (parts[0].Equals("6") && (parts.Length == 8))
                                {
                                    valid = true;
                                    division = Int32.Parse(parts[1]) - 1;
                                    QueuedMatch[division][0] = Int32.Parse(parts[2]);
                                    QueuedMatch[division][1] = Int32.Parse(parts[3]);
                                    QueuedMatch[division][2] = Int32.Parse(parts[4]);
                                    CurField[division] = Int32.Parse(parts[5]) - 1;
                                }
                                else if (parts[0].Equals("12") && (parts.Length == 3))
                                {
                                    valid = true;
                                    division = Int32.Parse(parts[1]) - 1;
                                    if (parts[2].Equals("0"))
                                    {
                                        AudienceDisplay[division] = 1;
                                    }
                                    else
                                    {
                                        AudienceDisplay[division] = Int32.Parse(parts[2]);
                                    }
                                }
								else if(parts[0].Equals("22"))
                                {
                                    valid = true;
                                    Fields.Clear();

                                    for (int i = 0; i < (parts.Length-2)/2; i++)
                                    {
                                        int divId = Int32.Parse(parts[2 + i*2]) - 1;
                                        if (!DivisionFields.ContainsKey(divId))
                                            DivisionFields.Add(divId, new List<int>());

                                        DivisionFields[divId].Add(Fields.Count);
                                        Fields.Add(parts[1 + i*2]);
                                    }
                                }
                                else if (parts[0].Equals("24"))
                                {
                                    valid = true;
                                    ReceiveFinished[2] = true;

                                    QueuedMatch.Clear();
                                    CurField.Clear();
                                    AudienceDisplay.Clear();

                                    for (int i = 1; i < parts.Length - 1; i++)
                                    {
                                        Divisions.Add(parts[i]);
                                        QueuedMatch.Add(new int[] { -1, -1, -1 });
                                        CurField.Add(0);
                                        AudienceDisplay.Add(1);
                                    }
                                }
								
								
								if(valid) {
									FireEvent(parts[0], division);
								}
								
							}
						}
						else {
							if(Client == null) {
								Logout();
							}
							break;
						}

						Thread.Sleep(10);
					}
					catch (Exception e) {
						if(Client == null) {
							Logout();
						}
						break;
					}
				}
            }).Start();
        }

        public void AddListener(EventHandler listener)
        {
            Listeners += listener;
        }

        private void FireEvent(String type, int division = -1)
        {
            if (Listeners != null)
            {
                DataEventArgs args = new DataEventArgs();
                args.setDataType(type);
                args.setDivision(division);
                Listeners(this, args);
            }
        }

        public Boolean getReceiveFinished() {
		    foreach(Boolean val in ReceiveFinished) {
			    if(!val)
				    return false;
		    }
		
		    return true;
	    }

        public String[] getDivisions()
        {
            return Divisions.ToArray();
        }

        public int getDivisionIdForName(String division)
        {
            for (int i = 0; i < Divisions.Count; i++)
            {
                if (Divisions[i].Equals(division))
                {
                    return i;
                }
            }

            return -1;
        }

        public String[] getFieldsForDivision(String DivisionName)
        {
            for (int i = 0; i < Divisions.Count; i++)
            {
                if (Divisions[i].Equals(DivisionName))
                {
                    if (DivisionFields.ContainsKey(i))
                    {
                        List<String> fields = new List<String>();
                        foreach (int fieldId in DivisionFields[i])
                        {
                            fields.Add(Fields[fieldId]);
                        }

                        return fields.ToArray();
                    }
                }
            }

            return null;
        }

        public int getFieldIdForDivisionAndName(String division, String field)
        {
            for (int i = 0; i < Divisions.Count; i++)
            {
                if (Divisions[i].Equals(division))
                {
                    if (DivisionFields.ContainsKey(i))
                    {
                        for (int j = 0; j < DivisionFields[i].Count; j++)
                        {
                            if (Fields[DivisionFields[i][j]].Equals(field))
                            {
                                return DivisionFields[i][j];
                            }
                        }
                    }
                }
            }

            return -1;
        }

        public String getField(int index)
        {
            return Fields[index];
        }

        public int[] getQueuedMatch(int division)
        {
            return QueuedMatch[division];
        }

        public int GetCurField(int division)
        {
            return CurField[division];
        }

        public String GetAudienceDisplay(int division)
        {
            int selection = 0;
            if (AudienceDisplay[division] <= 8)
            {
                selection = AudienceDisplay[division] - 1;
            }
            else if (AudienceDisplay[division] == 9)
            {
                selection = 9;
            }
            else if (AudienceDisplay[division] == 12)
            {
                selection = 8;
            }
            else if (AudienceDisplay[division] == 13)
            {
                selection = 10;
            }
            return AudienceDisplayNames[selection];
        }

        public Boolean GetValidDisplayForFox(int division) {
            return ShouldShowFox.Contains(AudienceDisplay[division]);
        }

        public void Login(String ip, String username, String password)
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

            String rcv = ClientIn.ReadLine();

            SendMessage(username + ((char)29) + GetStringMD5(GetStringMD5(password + "thisll throw off decrypters!") + rcv));

            rcv = ClientIn.ReadLine();

            if (rcv != null && !rcv.Equals("0"))
            {
                IP = ip;
                Username = username;
                Password = password;
                AuthGroup = Int32.Parse(rcv);

                if (ValidAuthGroups.Contains(AuthGroup))
                {
                    Listener();
                    return;
                }

                CleanUp();
                throw new Exception("This user doesn't have enough privileges");
            }

            CleanUp();
            throw new Exception("Invalid Username or Password");
        }

        public void Logout()
        {
            if (AuthGroup > 0)
            {
                CleanUp();
                //fireEvent("-1");
            }
        }
    }
}
