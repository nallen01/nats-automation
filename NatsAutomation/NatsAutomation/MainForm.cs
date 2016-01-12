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

        private Panel FoxPanel;
        private Panel StatusPanel;

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

                Service.AddListener(new EventHandler(ServiceListener));
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

            // Show the form!
            ShowForm();
        }

        private void SetSize(int width, int height)
        {
            this.ClientSize = new System.Drawing.Size(width, height);

            if (Config.IncludeFox)
            {
                this.StatusPanel.Height = height - 20;
                this.StatusPanel.Width = (width / 2) - 20;

                this.FoxPanel.Height = height - 20;
                this.FoxPanel.Width = (width / 2) - 20;
            }
            else
            {
                this.StatusPanel.Height = height - 20;
                this.StatusPanel.Width = width - 20;
            }
        }

        public void SetHeight(int height)
        {
            SetSize(this.ClientRectangle.Width, height);
        }

        private void ShowForm()
        {
            int deltaH = this.Height - this.ClientRectangle.Height;
            int deltaW = this.Width - this.ClientRectangle.Width;

            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            if (Config.IncludeFox) {
                this.StatusPanel = new Panel();
                this.Controls.Add(this.StatusPanel);
                this.StatusPanel.Location = new System.Drawing.Point(10, 10);

                this.FoxPanel = new Panel();
                this.Controls.Add(this.FoxPanel);
                this.FoxPanel.Location = new System.Drawing.Point(310, 10);

                SetSize(600, 400);

                ShowFoxSelectors();
            }
            else
            {
                this.StatusPanel = new Panel();
                this.Controls.Add(this.StatusPanel);
                this.StatusPanel.Location = new System.Drawing.Point(10, 10);

                SetSize(300, 400);
            }

            ShowStatus();
        }

        private void ShowStatus()
        {
            Label lbl = new Label();
            this.StatusPanel.Controls.Add(lbl);
            lbl.Text = "Current Status";
            lbl.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            lbl.Location = new System.Drawing.Point(0, 0);
            lbl.Size = new System.Drawing.Size(280, 20);
            lbl.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

            int top = 30;
            int i = 0;
            foreach (String Division in Service.getDivisions())
            {
                Label div_name = new Label();
                this.StatusPanel.Controls.Add(div_name);
                div_name.Text = Division;
                div_name.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                div_name.Location = new System.Drawing.Point(0, top);
                div_name.Size = new System.Drawing.Size(280, 20);
                div_name.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

                top += 25;

                Label cur_match_label = new Label();
                this.StatusPanel.Controls.Add(cur_match_label);
                cur_match_label.Text = "Current Match:";
                cur_match_label.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                cur_match_label.Location = new System.Drawing.Point(0, top);
                cur_match_label.Size = new System.Drawing.Size(135, 20);
                cur_match_label.TextAlign = System.Drawing.ContentAlignment.MiddleRight;

                TextBox cur_match = new TextBox();
                this.StatusPanel.Controls.Add(cur_match);
                cur_match.Name = "txt_match_" + i;
                cur_match.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                cur_match.Location = new System.Drawing.Point(145, top);
                cur_match.Size = new System.Drawing.Size(135, 20);
                cur_match.ReadOnly = true;

                top += 25;

                Label cur_field_label = new Label();
                this.StatusPanel.Controls.Add(cur_field_label);

                cur_field_label.Text = "Current Field:";
                cur_field_label.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                cur_field_label.Location = new System.Drawing.Point(0, top);
                cur_field_label.Size = new System.Drawing.Size(135, 20);
                cur_field_label.TextAlign = System.Drawing.ContentAlignment.MiddleRight;

                TextBox cur_field = new TextBox();
                this.StatusPanel.Controls.Add(cur_field);
                cur_field.Name = "txt_field_" + i;
                cur_field.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                cur_field.Location = new System.Drawing.Point(145, top);
                cur_field.Size = new System.Drawing.Size(135, 20);
                cur_field.ReadOnly = true;

                top += 25;

                Label cur_display_label = new Label();
                this.StatusPanel.Controls.Add(cur_display_label);
                cur_display_label.Text = "Current Display:";
                cur_display_label.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                cur_display_label.Location = new System.Drawing.Point(0, top);
                cur_display_label.Size = new System.Drawing.Size(135, 20);
                cur_display_label.TextAlign = System.Drawing.ContentAlignment.MiddleRight;

                TextBox cur_display = new TextBox();
                this.StatusPanel.Controls.Add(cur_display);
                cur_display.Name = "txt_display_" + i;
                cur_display.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                cur_display.Location = new System.Drawing.Point(145, top);
                cur_display.Size = new System.Drawing.Size(135, 20);
                cur_display.ReadOnly = true;

                UpdateQueuedMatch(i);
                UpdateAudienceDisplay(i);

                if (Config.IncludeFox)
                {
                    top += 25;

                    Label cur_fox_label = new Label();
                    this.StatusPanel.Controls.Add(cur_fox_label);
                    cur_fox_label.Text = "Is Fox Shown?";
                    cur_fox_label.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                    cur_fox_label.Location = new System.Drawing.Point(0, top);
                    cur_fox_label.Size = new System.Drawing.Size(135, 20);
                    cur_fox_label.TextAlign = System.Drawing.ContentAlignment.MiddleRight;

                    CheckBox cur_fox = new CheckBox();
                    this.StatusPanel.Controls.Add(cur_fox);
                    cur_fox.Name = "chk_fox_" + i;
                    cur_fox.Location = new System.Drawing.Point(145, top);
                    cur_fox.Size = new System.Drawing.Size(135, 20);
                    cur_fox.Enabled = false;

                    UpdateFox(i);
                }

                top += 50;
                i++;
            }

            SetHeight(top);
        }

        private void ShowFoxSelectors()
        {
            Label lbl = new Label();
            this.FoxPanel.Controls.Add(lbl);
            lbl.Text = "Fox Selector";
            lbl.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            lbl.Location = new System.Drawing.Point(0, 0);
            lbl.Size = new System.Drawing.Size(270, 20);
            lbl.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

            int top = 30;
            int i = 0;
            foreach (String Division in Service.getDivisions())
            {
                Label tmp = new Label();
                this.FoxPanel.Controls.Add(tmp);
                tmp.Text = Division;
                tmp.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                tmp.Location = new System.Drawing.Point(0, top);
                tmp.Size = new System.Drawing.Size(130, 20);
                tmp.TextAlign = System.Drawing.ContentAlignment.MiddleRight;

                RadioButton check = new RadioButton();
                this.FoxPanel.Controls.Add(check);
                check.Location = new System.Drawing.Point(140, top);
                check.Name = "fox_click_" + i;
                check.Size = new System.Drawing.Size(130, 20);
                check.Click += new System.EventHandler(this.Fox_Check);

                top += 25;
                i++;
            }
        }

        private Control GetFirstChildByName(Control.ControlCollection controls, String name)
        {
            Control[] result = controls.Find(name, true);
            if (result.Length > 0)
            {
                return result[0];
            }

            return null;
        }

        private void UpdateQueuedMatch(int division)
        {
            int[] queued = Service.getQueuedMatch(division);
            String match_name = Match.toName(queued[0], queued[1], queued[2]);
            String field_name = "";
            if (!String.IsNullOrWhiteSpace(match_name))
            {
                field_name = Service.getField(Service.GetCurField(division));
            }
            else
            {
                match_name = "NO MATCH";
            }

            Control cur_match = GetFirstChildByName(this.StatusPanel.Controls, "txt_match_" + division);
            if (cur_match != null)
            {
                ((TextBox)cur_match).Text = match_name;
            }

            Control cur_field = GetFirstChildByName(this.StatusPanel.Controls, "txt_field_" + division);
            if (cur_field != null)
            {
                ((TextBox)cur_field).Text = field_name;
            }

            // Update the vision to the new field
            for (int i = 0; i < VisionTasks.Count; i++)
            {
                if (VisionTasks[i].ContainsKey(Service.GetCurField(division)))
                {
                    VisionServices[i].RunMacro(VisionTasks[i][Service.GetCurField(division)]);
                }
            }

            // Update the lighting to the new field
            for (int i = 0; i < LightingTasks.Count; i++)
            {
                if (LightingTasks[i].ContainsKey(Service.GetCurField(division)))
                {
                    LightingServices[i].RunSequence(LightingTasks[i][Service.GetCurField(division)]);
                }
            }
        }

        private void UpdateAudienceDisplay(int division)
        {
            Control cur_display = GetFirstChildByName(this.StatusPanel.Controls, "txt_display_" + division);
            if (cur_display != null)
            {
                ((TextBox)cur_display).Text = Service.GetAudienceDisplay(division);
            }
        }

        public void UpdateFox(int division)
        {
            Control fox_select = GetFirstChildByName(this.FoxPanel.Controls, "fox_click_" + division);
            Control fox_out = GetFirstChildByName(this.StatusPanel.Controls, "chk_fox_" + division);
            if (fox_select != null && fox_out != null)
            {
                Boolean output = false;
                if (((RadioButton)fox_select).Checked)
                {
                    if (Service.GetValidDisplayForFox(division))
                    {
                        output = true;
                    }
                }

                ((CheckBox)fox_out).Checked = output;
            }
        }

        private void Fox_Check(object sender, EventArgs e)
        {
            if (sender is CheckBox)
            {
                CheckBox fox_check = (CheckBox)sender;

                int division = Int32.Parse(fox_check.Name.Substring(fox_check.Name.LastIndexOf("_") + 1));

                UpdateFox(division);
            }
        }

        private void ServiceListener(Object sender, EventArgs e)
        {
            DataEventArgs args = e as DataEventArgs;

            if (args != null)
            {
                switch (args.getDataType())
                {
                    case "6": // Queued Match
                        this.Invoke(new MethodInvoker(() => UpdateQueuedMatch(args.getDivision())));
                        if (Config.IncludeFox) { this.Invoke(new MethodInvoker(() => UpdateFox(args.getDivision()))); }
                        break;
                    case "12": // Audience Display Match
                        this.Invoke(new MethodInvoker(() => UpdateAudienceDisplay(args.getDivision())));
                        if (Config.IncludeFox) { this.Invoke(new MethodInvoker(() => UpdateFox(args.getDivision()))); }
                        break;
                }
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            CleanUp();
        }
    }
}
