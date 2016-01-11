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

        private Configuration Config;

        private CommsService Service;
        private List<VisionService> VisionServices;
        private List<LightingService> LightingServices;

        public MainForm()
        {
            InitializeComponent();
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
                Application.Exit();
                return;
            }
            

            // Connect to Vision
            int currentVision = 1;
            this.VisionServices = new List<VisionService>();
            try
            {
                _BMDSwitcherConnectToFailure failReason = 0;

                CBMDSwitcherDiscovery switcher_discovery = new CBMDSwitcherDiscovery();
                if (switcher_discovery == null)
                {
                    throw new Exception("ATEM Switcher Software not installed");
                }

                foreach (String IP in Config.VisionIPs)
                {
                    try
                    {
                        IBMDSwitcher Switcher;
                        switcher_discovery.ConnectTo(IP, out Switcher, out failReason);
                        VisionServices.Add(new VisionService(Switcher));
                        currentVision++;
                    }
                    catch (COMException)
                    {
                        switch (failReason)
                        {
                            case _BMDSwitcherConnectToFailure.bmdSwitcherConnectToFailureNoResponse:
                                throw new Exception("No response from Switcher at " + IP);
                            case _BMDSwitcherConnectToFailure.bmdSwitcherConnectToFailureIncompatibleFirmware:
                                throw new Exception("Switcher at " + IP + " has incompatible firmware");
                            default:
                                throw new Exception("Connection to " + IP + " failed for unknown reason");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to connect to Vision Mixer #" + currentVision + ":\n\n" + ex.Message, APPLICATION_NAME, MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
                return;
            }
            

            // Connect to Lighting
            int currentLighting = 1;
            this.LightingServices = new List<LightingService>();
            try
            {
                foreach (String IP in Config.LightingIPs)
                {
                    // TODO: Add actual code here to connect to lighting service
                    LightingServices.Add(new LightingService());
                    currentLighting++;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to connect to Lighting Device #" + currentLighting + ":\n\n" + ex.Message, APPLICATION_NAME, MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
                return;
            }


            // Validate Configuration File


            var x = 1;
        }
    }
}
