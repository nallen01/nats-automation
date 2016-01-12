using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using System.Linq;
using BMDSwitcherAPI;

namespace NatsAutomation
{
    public partial class MainForm : Form
    {
        public static String APPLICATION_NAME = "Nats Automation";
        public static String CONFIG_FILE = @"NatsAutomation.cfg";

        public static Boolean IGNORE_VISION = true;
        public static Boolean IGNORE_LIGHTING = true;

        private Configuration Config;

        private CommsService Service;
        private List<VisionService> VisionServices;
        private List<LightingService> LightingServices;

        private Dictionary<int, Dictionary<int, int>> VisionTasks;
        private Dictionary<int, Dictionary<int, int>> LightingTasks;

        public MainForm()
        {
            InitializeComponent();
        }

        public void CleanUp()
        {
            if (Service != null)
            {
                Service.Logout();
            }

            if (LightingServices != null)
            {
                foreach (LightingService LightService in LightingServices)
                {
                    LightService.CleanUp();
                }
            }
        }

        public void MainForm_Load(object sender, EventArgs e)
        {
            // Load the configuration File
            try
            {
                string configData = System.IO.File.ReadAllText(CONFIG_FILE);
                Config = new Configuration(configData);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to read configuration file '" + CONFIG_FILE + "'\n\n" + ex.Message, APPLICATION_NAME, MessageBoxButtons.OK, MessageBoxIcon.Error);
                CleanUp();
                Application.Exit();
                return;
            }


            // Connect to EM
            try
            {
                if(String.IsNullOrWhiteSpace(Config.EventManagerIP))
                    throw new Exception("No IP specified in Configuration File");
                if(String.IsNullOrWhiteSpace(Config.EventManagerUsername))
                    throw new Exception("No Username specified in Configuration File");
                if(String.IsNullOrWhiteSpace(Config.EventManagerPassword))
                    throw new Exception("No Password specified in Configuration File");

                Service = new CommsService();
                Service.Login(Config.EventManagerIP, Config.EventManagerUsername, Config.EventManagerPassword);
                int i = 1;
                while (!Service.getReceiveFinished())
                {
                    Thread.Sleep(10);

                    if (10 * (i++) > 2000)
                    {
                        throw new Exception("An unknown error occured");
                    }
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show("Unable to connect to Event Manager server:\n\n" + ex.Message, APPLICATION_NAME, MessageBoxButtons.OK, MessageBoxIcon.Error);
                CleanUp();
                Application.Exit();
                return;
            }
            

            // Connect to Vision
            VisionServices = new List<VisionService>();
            VisionTasks = new Dictionary<int, Dictionary<int, int>>();
            try
            {
                foreach (String VisionMixer in Config.VisionMixers)
                {
                    VisionTasks.Add(VisionServices.Count, new Dictionary<int, int>());
                    VisionServices.Add(new VisionService(VisionMixer));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to connect to Vision Mixer #" + (VisionServices.Count+1) + ":\n\n" + ex.Message, APPLICATION_NAME, MessageBoxButtons.OK, MessageBoxIcon.Error);
                CleanUp();
                Application.Exit();
                return;
            }
            

            // Connect to Lighting
            LightingServices = new List<LightingService>();
            LightingTasks = new Dictionary<int, Dictionary<int, int>>();
            try
            {
                foreach (String LightingServer in Config.LightingServers)
                {
                    LightingTasks.Add(LightingServices.Count, new Dictionary<int, int>());
                    LightingServices.Add(new LightingService(LightingServer));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to connect to Lighting Device #" + (LightingServices.Count + 1) + ":\n\n" + ex.Message, APPLICATION_NAME, MessageBoxButtons.OK, MessageBoxIcon.Error);
                CleanUp();
                Application.Exit();
                return;
            }


            // Validate Configuration File
            try {
                int i = 1;
                foreach (VisionEntry Entry in Config.VisionEntries)
                {
                    if (Entry.ServerIndex >= VisionServices.Count)
                        throw new Exception("Invalid Server Index for Vision Entry " + i);

                    if (!Service.getDivisions().Any(div => div.Equals(Entry.DivisionName)))
                        throw new Exception("Invalid Division Name for Vision Entry " + i);

                    if (!Service.getFieldsForDivision(Entry.DivisionName).Any(field => field.Equals(Entry.FieldName)))
                        throw new Exception("Invalid Field Name for Vision Entry " + i);

                    int fieldId = Service.getFieldIdForDivisionAndName(Entry.DivisionName, Entry.FieldName);

                    if (VisionTasks[Entry.ServerIndex].ContainsKey(fieldId))
                        throw new Exception("Vision Entry " + i + " contains a Mixer-Division-Field pair that has already been declared");

                    VisionTasks[Entry.ServerIndex].Add(fieldId, Entry.MacroNumber);

                    i++;
                }

                i = 1;
                foreach (LightingEntry Entry in Config.LightingEntries)
                {
                    if (Entry.ServerIndex >= LightingServices.Count)
                        throw new Exception("Invalid Server Index for Lighting Entry " + i);

                    if (!Service.getDivisions().Any(div => div.Equals(Entry.DivisionName)))
                        throw new Exception("Invalid Division Name for Lighting Entry " + i);

                    if (!Service.getFieldsForDivision(Entry.DivisionName).Any(field => field.Equals(Entry.FieldName)))
                        throw new Exception("Invalid Field Name for Lighting Entry " + i);

                    int fieldId = Service.getFieldIdForDivisionAndName(Entry.DivisionName, Entry.FieldName);

                    if (LightingTasks[Entry.ServerIndex].ContainsKey(fieldId))
                        throw new Exception("Lighting Entry " + i + " contains a Server-Division-Field pair that has already been declared");

                    LightingTasks[Entry.ServerIndex].Add(fieldId, Entry.SequenceNumber);

                    i++;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Configuration Error:\n\n" + ex.Message, APPLICATION_NAME, MessageBoxButtons.OK, MessageBoxIcon.Error);
                CleanUp();
                Application.Exit();
                return;
            }

            var x = 1;
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            CleanUp();
        }
    }
}
