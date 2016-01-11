using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NatsAutomation
{
    public partial class MainForm : Form
    {
        public static String CONFIG_FILE = @"NatsAutomation.cfg";

        private Configuration config;

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
                config = new Configuration(configData);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to read configuration file '" + CONFIG_FILE + "'\n\n" + ex.Message, "Nats Automation", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }


            // Connect to EM


            // Connect to Vision


            // Connect to Lighting


            // Validate Configuration File


            var x = 1;
        }
    }
}
