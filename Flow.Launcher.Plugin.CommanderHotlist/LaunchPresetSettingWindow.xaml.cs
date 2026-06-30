using System.Windows;
using System.Windows.Input;

namespace Flow.Launcher.Plugin.CommanderHotlist
{
    public partial class LaunchPresetSettingWindow : Window
    {
        private readonly LaunchPreset _currentPreset;

        public LaunchPresetSettingWindow(string title, LaunchPreset preset)
        {
            InitializeComponent();
            TitleText.Text = title;
            _currentPreset = preset;
            NameBox.Text = preset.Name;
            DescriptionBox.Text = preset.Description ?? string.Empty;
            ArgumentsBox.Text = preset.Arguments ?? string.Empty;
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            _currentPreset.Name = string.IsNullOrWhiteSpace(NameBox.Text) ? string.Empty : NameBox.Text.Trim();
            _currentPreset.Description = string.IsNullOrWhiteSpace(DescriptionBox.Text) ? null : DescriptionBox.Text.Trim();
            _currentPreset.Arguments = string.IsNullOrWhiteSpace(ArgumentsBox.Text) ? null : ArgumentsBox.Text.Trim();

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void WindowKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ConfirmButton_Click(sender, e);
            }
        }
    }
}
