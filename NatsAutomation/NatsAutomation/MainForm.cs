using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using BMDSwitcherAPI;

namespace NatsAutomation
{
    public partial class MainForm : Form
    {
        public static String APPLICATION_NAME = "Nats Automation";
        public static String CONFIG_FILE = @"NatsAutomation.cfg";

        public static Boolean IGNORE_VISION = true;
        public static Boolean IGNORE_LIGHTING = false;

        private Configuration Config;

        private CommsService Service;
        private List<VisionService> VisionServices;
        private List<LightingService> LightingServices;

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
            this.VisionServices = new List<VisionService>();
            try
            {
                foreach (String VisionMixer in Config.VisionMixers)
                {
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
            this.LightingServices = new List<LightingService>();
            try
            {
                foreach (String LightingServer in Config.LightingServers)
                {
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


            var x = 1;
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            CleanUp();
        }
    }
}
