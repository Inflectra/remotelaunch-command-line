using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Inflectra.RemoteLaunch.Interfaces;
using Inflectra.RemoteLaunch.Interfaces.DataObjects;
using System.Text.RegularExpressions;

namespace Inflectra.RemoteLaunch.Engines.CommandLine
{
    /// <summary>
    /// Interaction logic for AutomationEngineSettings.xaml
    /// </summary>
    /// <remarks>
    /// This panel is used to display and automation-engine specific configuration settings
    /// </remarks>
    public partial class AutomationEngineSettings : UserControl
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public AutomationEngineSettings()
        {
            InitializeComponent();
            this.LoadSettings();
        }

        /// <summary>
        /// Loads the saved settings
        /// </summary>
        private void LoadSettings()
        {
            //Populate the combo box
            this.cboDefaultStatus.ItemsSource = Enum.GetValues(typeof(AutomatedTestRun4.TestStatusEnum));

            //Load the various properties
            this.chkLogResults.IsChecked = Properties.Settings.Default.LogResults;
            this.chkRunAsAdmin.IsChecked = Properties.Settings.Default.RunAsAdmin;
            this.txtBlockedRegex.Text = Properties.Settings.Default.BlockedRegex;
            this.txtCautionRegex.Text = Properties.Settings.Default.CautionRegex;
            this.txtFailRegex.Text = Properties.Settings.Default.FailRegex;
            this.txtPassRegex.Text = Properties.Settings.Default.PassRegex;
            if (Enum.IsDefined(typeof(AutomatedTestRun4.TestStatusEnum), Properties.Settings.Default.DefaultStatus))
            {
                AutomatedTestRun4.TestStatusEnum defaultStatus = (AutomatedTestRun4.TestStatusEnum)Properties.Settings.Default.DefaultStatus;
                this.cboDefaultStatus.SelectedValue = defaultStatus;
            }
        }

        /// <summary>
        /// Saves the specified settings.
        /// </summary>
        public void SaveSettings()
        {
            //Get the various properties
            if (this.chkLogResults.IsChecked.HasValue)
            {
                Properties.Settings.Default.LogResults = this.chkLogResults.IsChecked.Value;
            }
            if (this.chkRunAsAdmin.IsChecked.HasValue)
            {
                Properties.Settings.Default.RunAsAdmin = this.chkRunAsAdmin.IsChecked.Value;
            }

            Properties.Settings.Default.BlockedRegex = this.txtBlockedRegex.Text.Trim();
            Properties.Settings.Default.CautionRegex = this.txtCautionRegex.Text.Trim();
            Properties.Settings.Default.FailRegex = this.txtFailRegex.Text.Trim();
            Properties.Settings.Default.PassRegex = this.txtPassRegex.Text.Trim();
            if (this.cboDefaultStatus.SelectedValue is AutomatedTestRun4.TestStatusEnum)
            {
                AutomatedTestRun4.TestStatusEnum defaultStatus = (AutomatedTestRun4.TestStatusEnum)this.cboDefaultStatus.SelectedValue;
                Properties.Settings.Default.DefaultStatus = (int)defaultStatus;
            }

            //Save the properties and reload
            Properties.Settings.Default.Save();
            this.LoadSettings();
        }

        /// <summary>
        /// Called when the Regex test button is clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnTestRegex_Click(object sender, RoutedEventArgs e)
        {
            //Save the settings first
            SaveSettings();

            //Get the sample text
            string results = this.txtTestMessage.Text;

            //We use the Regexes to determine the status
            AutomatedTestRun4.TestStatusEnum status = (AutomatedTestRun4.TestStatusEnum)Properties.Settings.Default.DefaultStatus;
            Regex passRegex = new Regex(Properties.Settings.Default.PassRegex, RegexOptions.IgnoreCase);
            Regex failRegex = new Regex(Properties.Settings.Default.FailRegex, RegexOptions.IgnoreCase);
            Regex cautionRegex = new Regex(Properties.Settings.Default.CautionRegex, RegexOptions.IgnoreCase);
            Regex blockedRegex = new Regex(Properties.Settings.Default.BlockedRegex, RegexOptions.IgnoreCase);

            //Check passed
            if (passRegex.IsMatch(results))
            {
                status = AutomatedTestRun4.TestStatusEnum.Passed;
            }
            if (cautionRegex.IsMatch(results))
            {
                status = AutomatedTestRun4.TestStatusEnum.Caution;
            }
            if (failRegex.IsMatch(results))
            {
                status = AutomatedTestRun4.TestStatusEnum.Failed;
            }
            if (blockedRegex.IsMatch(results))
            {
                status = AutomatedTestRun4.TestStatusEnum.Blocked;
            }

            MessageBox.Show(String.Format("This test result would be recorded as '{0}'", status), "Regular Expression Test", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
