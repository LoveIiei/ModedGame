using ModedGame.Models;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using ModedGame.Services;

namespace ModedGame
{
    public partial class VersionSelectorWindow : Window
    {
        // Public property to retrieve the user's choice
        public ModrinthVersion SelectedVersion { get; private set; }

        // The constructor now accepts a list of versions to display
        public VersionSelectorWindow(List<ModrinthVersion> versions)
        {
            InitializeComponent();

            // We process the versions to make them more display-friendly
            var displayVersions = versions.Select(v => new ModrinthVersionDisplay(v)).ToList();
            VersionsListView.ItemsSource = displayVersions;
        }

        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            if (VersionsListView.SelectedItem is ModrinthVersionDisplay selectedDisplayVersion)
            {
                // Store the original version object
                SelectedVersion = selectedDisplayVersion.OriginalVersion;
                this.DialogResult = true; // Signals that a selection was made
                this.Close();
            }
            else
            {
                MessageBox.Show("Please select a version from the list.", "No Version Selected");
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false; // Signals that the user cancelled
            this.Close();
        }
    }

    // A helper class to make the version data easier to display in the ListView
    public class ModrinthVersionDisplay
    {
        public ModrinthVersion OriginalVersion { get; }

        public string Name => OriginalVersion.Name;
        public string GameVersionsText => string.Join(", ", OriginalVersion.GameVersions);
        public string LoadersText => string.Join(", ", OriginalVersion.Loaders);
        public string VersionType => OriginalVersion.VersionType;

        public ModrinthVersionDisplay(ModrinthVersion original)
        {
            OriginalVersion = original;
        }
    }
}