using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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

            ShowSubmenuNames.IsChecked = settings.ShowSubmenuNames;

            RefreshTcPresetsDisplay();
            RefreshDcPresetsDisplay();

            // Events — DC
            DcEnabled.Checked += (_, _) => SaveDcSettings();
            DcEnabled.Unchecked += (_, _) => SaveDcSettings();
            DcExePath.LostFocus += (_, _) => SaveDcSettings();
            DcSettingsPath.LostFocus += (_, _) => SaveDcSettings();
            DcAdditionalArgs.LostFocus += (_, _) => SaveDcSettings();

            // Events — TC
            TcEnabled.Checked += (_, _) => SaveTcSettings();
            TcEnabled.Unchecked += (_, _) => SaveTcSettings();
            TcExePath.LostFocus += (_, _) => SaveTcSettings();
            TcSettingsPath.LostFocus += (_, _) => SaveTcSettings();
            TcAdditionalArgs.LostFocus += (_, _) => SaveTcSettings();

            // Events — Global
            ShowSubmenuNames.Checked += (_, _) => SaveGlobalSettings();
            ShowSubmenuNames.Unchecked += (_, _) => SaveGlobalSettings();

            // Events — Browse buttons
            DcExeBrowse.Click += (_, _) => BrowseFile(DcExePath, "Executable files (*.exe)|*.exe|All files (*.*)|*.*");
            DcSettingsBrowse.Click += (_, _) => BrowseFile(DcSettingsPath, "XML files (*.xml)|*.xml|All files (*.*)|*.*");
            TcExeBrowse.Click += (_, _) => BrowseFile(TcExePath, "Executable files (*.exe)|*.exe|All files (*.*)|*.*");
            TcSettingsBrowse.Click += (_, _) => BrowseFile(TcSettingsPath, "INI files (*.ini)|*.ini|All files (*.*)|*.*");

            // Events — TC launch presets
            TcPresetAdd.Click += (_, _) => AddPreset(_settings.TcLaunchPresets, TcPresetsView);
            TcPresetEdit.Click += (_, _) => EditPreset(_settings.TcLaunchPresets, TcPresetsView);
            TcPresetDelete.Click += (_, _) => DeletePreset(_settings.TcLaunchPresets, TcPresetsView);

            // Events — DC launch presets
            DcPresetAdd.Click += (_, _) => AddPreset(_settings.DcLaunchPresets, DcPresetsView);
            DcPresetEdit.Click += (_, _) => EditPreset(_settings.DcLaunchPresets, DcPresetsView);
            DcPresetDelete.Click += (_, _) => DeletePreset(_settings.DcLaunchPresets, DcPresetsView);

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

        private void SavePresets()
        {
            RefreshTcPresetsDisplay();
            RefreshDcPresetsDisplay();
            _context.API.SaveSettingJsonStorage<Settings>();
        }

        private void RefreshTcPresetsDisplay()
        {
            ToolConfig? tcTool = _settings.GetTools().First(t => t.ToolType == ToolType.TotalCommander);
            TcPresetsView.ItemsSource = _settings.TcLaunchPresets
                .Select(p => new LaunchPresetDisplay(p, tcTool))
                .ToList();
        }

        private void RefreshDcPresetsDisplay()
        {
            ToolConfig? dcTool = _settings.GetTools().First(t => t.ToolType == ToolType.DoubleCommander);
            DcPresetsView.ItemsSource = _settings.DcLaunchPresets
                .Select(p => new LaunchPresetDisplay(p, dcTool))
                .ToList();
        }

        private void SaveGlobalSettings()
        {
            _settings.ShowSubmenuNames = ShowSubmenuNames.IsChecked ?? false;

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
            DcPresetsView.IsEnabled = dcEnabled;
            DcPresetAdd.IsEnabled = dcEnabled;
            DcPresetEdit.IsEnabled = dcEnabled;
            DcPresetDelete.IsEnabled = dcEnabled;

            bool tcEnabled = TcEnabled.IsChecked == true;
            TcExePath.IsEnabled = tcEnabled;
            TcSettingsPath.IsEnabled = tcEnabled;
            TcAdditionalArgs.IsEnabled = tcEnabled;
            TcExeBrowse.IsEnabled = tcEnabled;
            TcSettingsBrowse.IsEnabled = tcEnabled;
            TcPresetsView.IsEnabled = tcEnabled;
            TcPresetAdd.IsEnabled = tcEnabled;
            TcPresetEdit.IsEnabled = tcEnabled;
            TcPresetDelete.IsEnabled = tcEnabled;
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

        private void AddPreset(List<LaunchPreset> presets, ListView listView)
        {
            var newPreset = new LaunchPreset();
            var window = new LaunchPresetSettingWindow("Add Launch Preset", newPreset);
            if (window.ShowDialog() == true)
            {
                presets.Add(newPreset);
                SavePresets();
            }
        }

        private void EditPreset(List<LaunchPreset> presets, ListView listView)
        {
            if (listView.SelectedItem is not LaunchPresetDisplay selected)
                return;

            var source = selected.Source;
            var editPreset = new LaunchPreset
            {
                Name = string.IsNullOrWhiteSpace(source.Name) ? selected.DisplayName : source.Name,
                Description = string.IsNullOrWhiteSpace(source.Description) ? selected.DisplayDescription : source.Description,
                Arguments = source.Arguments
            };

            var window = new LaunchPresetSettingWindow("Edit Launch Preset", editPreset);
            if (window.ShowDialog() == true)
            {
                var index = presets.IndexOf(source);
                presets[index] = editPreset;
                SavePresets();
            }
        }

        private void DeletePreset(List<LaunchPreset> presets, ListView listView)
        {
            if (listView.SelectedItem is not LaunchPresetDisplay selected)
                return;

            var source = selected.Source;
            var presetName = string.IsNullOrWhiteSpace(source.Name) ? "untitled" : source.Name;
            var result = _context.API.ShowMsgBox(
                $"Are you sure you want to delete \"{presetName}\"?",
                "Delete Launch Preset",
                MessageBoxButton.YesNo);

            if (result == MessageBoxResult.Yes)
            {
                presets.Remove(source);
                SavePresets();
            }
        }

        private void TcPresetsView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (((FrameworkElement)e.OriginalSource).DataContext is LaunchPresetDisplay)
            {
                EditPreset(_settings.TcLaunchPresets, TcPresetsView);
            }
        }

        private void DcPresetsView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (((FrameworkElement)e.OriginalSource).DataContext is LaunchPresetDisplay)
            {
                EditPreset(_settings.DcLaunchPresets, DcPresetsView);
            }
        }

        private void ListView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var listView = sender as ListView;
            var gView = listView.View as GridView;

            var workingWidth = listView.ActualWidth - SystemParameters.VerticalScrollBarWidth;

            if (workingWidth <= 0) 
                return;

            gView.Columns[0].Width = workingWidth * 0.27;
            gView.Columns[1].Width = workingWidth * 0.365;
            gView.Columns[2].Width = workingWidth * 0.365;
        }

        private sealed class LaunchPresetDisplay
        {
            public LaunchPreset Source { get; }
            public string DisplayName { get; }
            public string DisplayDescription { get; }
            public string Arguments => Source.Arguments ?? string.Empty;

            public LaunchPresetDisplay(LaunchPreset source, ToolConfig tool)
            {
                Source = source;

                // If user did not write a Name, use a default text "Open in <tool.DisplayName> (<args>)"
                if (!string.IsNullOrWhiteSpace(source.Name))
                {
                    DisplayName = source.Name;
                }
                else
                {
                    string args = source.Arguments ?? string.Empty;
                    DisplayName = string.IsNullOrWhiteSpace(args)
                        ? $"Open in {tool.DisplayName}"
                        : $"Open in {tool.DisplayName} ({args})";
                }

                // If user did not write a Description, use a default text "<executableName> <args>"
                if (!string.IsNullOrWhiteSpace(source.Description))
                {
                    DisplayDescription = source.Description;
                }
                else
                {
                    string exeName = System.IO.Path.GetFileName(tool.ExecutablePath);
                    string args = source.Arguments ?? string.Empty;
                    DisplayDescription = string.IsNullOrWhiteSpace(args) ? exeName : $"{exeName} {args}";
                }
            }
        }
    }
}