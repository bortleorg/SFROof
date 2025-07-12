using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace SFROofsSafetyMonitor
{
    public partial class SettingsForm : Form
    {
        public SettingsForm()
        {
            InitializeComponent();
            LoadRoofs();
        }

        private void LoadRoofs()
        {
            try
            {
                var roofs = AlpacaSafetyMonitor.LoadRoofs();
                comboBoxRoofs.Items.Clear();
                foreach (var roof in roofs)
                    comboBoxRoofs.Items.Add(roof.Name);

                comboBoxRoofs.SelectedItem = Properties.Settings.Default.SelectedRoofName;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load roof configs: " + ex.Message);
            }
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.SelectedRoofName = comboBoxRoofs.SelectedItem?.ToString();
            Properties.Settings.Default.Save();
            DialogResult = DialogResult.OK;
            Close();
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}