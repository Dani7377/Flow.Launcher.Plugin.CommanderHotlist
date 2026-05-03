using System.Windows.Controls;
using Microsoft.Win32;

namespace Flow.Launcher.Plugin.CommanderHotlist
{
    public partial class SettingsControl : UserControl
    {
        private readonly Settings _settings;
        private readonly PluginInitContext _context;

        public SettingsControl(PluginInitContext context, Settings settings)
        {
            InitializeComponent();

            _context = context;
            _settings = settings;

            // Load current values from settings into the XAML controls
            DcEnabled.IsChecked = settings.DcEnabled;
            DcExePath.Text = settings.DcExecutablePath;
            DcSettingsPath.Text = settings.DcSettingsXmlPath;
            DcAdditionalArgs.Text = settings.DcAdditionalArguments;

            TcEnabled.IsChecked = settings.TcEnabled;
            TcExePath.Text = settings.TcExecutablePath;
            TcSettingsPath.Text = settings.TcSettingsIniPath;
            TcAdditionalArgs.Text = settings.TcAdditionalArguments;

            // Events
            DcEnabled.Checked += (_, _) => SaveDcSettings();
            DcEnabled.Unchecked += (_, _) => SaveDcSettings();
            DcExePath.LostFocus += (_, _) => SaveDcSettings();
            DcSettingsPath.LostFocus += (_, _) => SaveDcSettings();
            DcAdditionalArgs.LostFocus += (_, _) => SaveDcSettings();

            TcEnabled.Checked += (_, _) => SaveTcSettings();
            TcEnabled.Unchecked += (_, _) => SaveTcSettings();
            TcExePath.LostFocus += (_, _) => SaveTcSettings();
            TcSettingsPath.LostFocus += (_, _) => SaveTcSettings();
            TcAdditionalArgs.LostFocus += (_, _) => SaveTcSettings();

            DcExeBrowse.Click += (_, _) => BrowseFile(DcExePath, "Executable files (*.exe)|*.exe|All files (*.*)|*.*");
            DcSettingsBrowse.Click += (_, _) => BrowseFile(DcSettingsPath, "XML files (*.xml)|*.xml|All files (*.*)|*.*");
            TcExeBrowse.Click += (_, _) => BrowseFile(TcExePath, "Executable files (*.exe)|*.exe|All files (*.*)|*.*");
            TcSettingsBrowse.Click += (_, _) => BrowseFile(TcSettingsPath, "INI files (*.ini)|*.ini|All files (*.*)|*.*");

            // Initialize enabled/disabled states
            UpdateEnabledStates();
        }

        private void SaveDcSettings()
        {
            _settings.DcEnabled = DcEnabled.IsChecked ?? false;
            _settings.DcExecutablePath = DcExePath.Text;
            _settings.DcSettingsXmlPath = DcSettingsPath.Text;
            _settings.DcAdditionalArguments = DcAdditionalArgs.Text;

            UpdateEnabledStates();
            _context.API.SaveSettingJsonStorage<Settings>();
        }

        private void SaveTcSettings()
        {
            _settings.TcEnabled = TcEnabled.IsChecked ?? false;
            _settings.TcExecutablePath = TcExePath.Text;
            _settings.TcSettingsIniPath = TcSettingsPath.Text;
            _settings.TcAdditionalArguments = TcAdditionalArgs.Text;

            UpdateEnabledStates();
            _context.API.SaveSettingJsonStorage<Settings>();
        }

        private void UpdateEnabledStates()
        {
            bool dcEnabled = DcEnabled.IsChecked == true;
            DcExePath.IsEnabled = dcEnabled;
            DcSettingsPath.IsEnabled = dcEnabled;
            DcAdditionalArgs.IsEnabled = dcEnabled;
            DcExeBrowse.IsEnabled = dcEnabled;
            DcSettingsBrowse.IsEnabled = dcEnabled;

            bool tcEnabled = TcEnabled.IsChecked == true;
            TcExePath.IsEnabled = tcEnabled;
            TcSettingsPath.IsEnabled = tcEnabled;
            TcAdditionalArgs.IsEnabled = tcEnabled;
            TcExeBrowse.IsEnabled = tcEnabled;
            TcSettingsBrowse.IsEnabled = tcEnabled;
        }

        private void BrowseFile(TextBox textBox, string filter)
        {
            var dialog = new OpenFileDialog
            {
                CheckFileExists = true,
                Multiselect = false,
                Filter = filter
            };

            if (dialog.ShowDialog() == true)
            {
                textBox.Text = dialog.FileName;

                // Persist immediately after browsing
                SaveDcSettings();
                SaveTcSettings();
            }
        }
    }
}