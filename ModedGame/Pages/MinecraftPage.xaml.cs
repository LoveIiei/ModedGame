using Microsoft.Win32;
using ModedGame.GameData;
using ModedGame.Models;
using ModedGame.Services;
using ModedGame.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static ModedGame.Models.MCVersionInfo;
using Path = System.IO.Path;

namespace ModedGame.Pages
{
    /// <summary>
    /// Interaction logic for MinecraftPage.xaml
    /// </summary>
    public partial class MinecraftPage : Page
    {
        private readonly ModrinthService _modrinthService;
        private int _currentPage = 1;
        private int _itemsPerPage = 20;
        private int _totalItems = 0;
        private int _totalPages = 0;
        private ModrinthSearchResponse _lastSearchResult;
        private Func<int, int, Task<ModrinthSearchResponse>> _currentSearchFunction;
        public MinecraftPage()
        {
            InitializeComponent();
            _modrinthService = new ModrinthService();
            DataContext = new MCFileTypeViewModel();
            LoadInstalledVersions();
            UpdatePaginationUI();
        }
        private void FileTypePicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = FileTypePicker.SelectedItem?.ToString();
            MessageBox.Show($"Selected: {selected}");
        }

        private string Get_FolderPath()
        {
            string folderPath = null;
            if (MinecraftPaths.MinecraftExists)
            {
                string selectedId = FileTypePicker.SelectedValue.ToString();
                switch (selectedId)
                {
                    case "1":
                        folderPath = MinecraftPaths.ModsFolder;
                        break;
                    case "2":
                        folderPath = MinecraftPaths.ResourcePacksFolder;
                        break;
                    case "3":
                        folderPath = MinecraftPaths.ShadersFolder;
                        break;
                }
            }
            return folderPath;
        }

        private void File_Click(object sender, RoutedEventArgs e)
        {
            string selectedFilePath;
            var selectedType = FileTypePicker.SelectedItem as MCFileTypes;
            if (selectedType == null) return;

            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = $"Select {selectedType.Name} File",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };

            switch (selectedType.Name.ToLower())
            {
                case "mod":
                    openFileDialog.Filter = "Mod files (*.jar)|*.jar|All files (*.*)|*.*";
                    break;
                case "resourcepack":
                    openFileDialog.Filter = "Resource Pack files (*.zip)|*.zip|All files (*.*)|*.*";
                    break;
                case "shaders":
                    openFileDialog.Filter = "Shader files (*.zip)|*.zip|All files (*.*)|*.*";
                    break;
                default:
                    openFileDialog.Filter = "All files (*.*)|*.*";
                    break;
            }

            if (openFileDialog.ShowDialog() == true)
            {
                selectedFilePath = openFileDialog.FileName;
                MCFileResult.Text = $"Selected: {Path.GetFileName(selectedFilePath)}";
            } else
            {
                MessageBox.Show(
                    "Error selecting files, please try again",
                    "Error",                            // window title
                     MessageBoxButton.OK,                // buttons
                     MessageBoxImage.Error               // error icon       
                     );
                return;
            }
            if (MinecraftPaths.MinecraftExists)
            {
                string folderPath = Get_FolderPath();
                try
                {
                    string mcFileName = System.IO.Path.GetFileName(selectedFilePath);
                    File.Move(selectedFilePath, System.IO.Path.Combine(folderPath, mcFileName));
                    MCFileResult.Text += $"\n👍Successfully added {mcFileName}";
                } catch (Exception ex) {
                    MessageBox.Show(
                     ex.Message,
                     "Error",
                     MessageBoxButton.OK,
                     MessageBoxImage.Error       
                     );
                }
            } 
            else
            {
                MessageBox.Show(
                    "CAN NOT find Minecraft Folder on this computer, please make sure you have it installed.",
                    "Error",
                     MessageBoxButton.OK,
                     MessageBoxImage.Error
                     );
                return;
            }

        }

        private async void OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            if (!MinecraftPaths.MinecraftExists)
            {
                MessageBox.Show("Minecraft Folder does not exist!",
                    "Error",
                     MessageBoxButton.OK,
                     MessageBoxImage.Error);
                return;
            }
            string folderPath = Get_FolderPath();
            Process.Start("explorer.exe", folderPath);
        }

        private async void SearchMods_Click(object sender, RoutedEventArgs e)
        {
            _currentPage = 1;
            await SearchModsAsync();
        }

        // Popular mods button click
        private async void LoadPopularMods_Click(object sender, RoutedEventArgs e)
        {
            _currentPage = 1; // Reset to first page
            _currentSearchFunction = (offset, limit) => _modrinthService.GetPopularModsAsync(limit, offset);
            await LoadModsWithPaginationAsync();
        }

        // Technology mods button click
        private async void LoadTechMods_Click(object sender, RoutedEventArgs e)
        {
            _currentPage = 1; // Reset to first page
            _currentSearchFunction = (offset, limit) => _modrinthService.GetModsByCategoryAsync("technology", limit, offset);
            await LoadModsWithPaginationAsync();
        }

        // Magic mods button click
        private async void LoadMagicMods_Click(object sender, RoutedEventArgs e)
        {
            _currentPage = 1; // Reset to first page
            _currentSearchFunction = (offset, limit) => _modrinthService.GetModsByCategoryAsync("magic", limit, offset);
            await LoadModsWithPaginationAsync();
        }

        // Generic search method
        private async Task SearchModsAsync()
        {
            var query = SearchBox.Text?.Trim() ?? "";
            var category = GetSelectedComboBoxValue(CategoryFilter);
            var version = GetSelectedComboBoxValue(VersionFilter);
            var loader = GetSelectedComboBoxValue(LoaderFilter);
            var sort = GetSelectedComboBoxTag(SortFilter) ?? "relevance";

            _currentSearchFunction = (offset, limit) => _modrinthService.SearchProjectsAsync(
                query, "mod", category, version, loader, limit, offset, sort);

            await LoadModsWithPaginationAsync();
        }

        // Generic mod loading method
        private async Task LoadModsWithPaginationAsync()
        {
            if (_currentSearchFunction == null) return;

            try
            {
                ShowLoading(true);

                var offset = (_currentPage - 1) * _itemsPerPage;
                var result = await _currentSearchFunction(offset, _itemsPerPage);

                _lastSearchResult = result;
                _totalItems = result.TotalHits;
                _totalPages = (int)Math.Ceiling((double)_totalItems / _itemsPerPage);

                ModsList.ItemsSource = result.Hits;
                ResultsCount.Text = $"Found {result.TotalHits:N0} mods";

                UpdatePaginationUI();
                ShowLoading(false);
            }
            catch (Exception ex)
            {
                ShowLoading(false);
                MessageBox.Show($"Error loading mods: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Pagination event handlers
        private async void FirstPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 1)
            {
                _currentPage = 1;
                await LoadModsWithPaginationAsync();
            }
        }

        private async void PreviousPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                await LoadModsWithPaginationAsync();
            }
        }

        private async void NextPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage < _totalPages)
            {
                _currentPage++;
                await LoadModsWithPaginationAsync();
            }
        }

        private async void LastPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage < _totalPages && _totalPages > 0)
            {
                _currentPage = _totalPages;
                await LoadModsWithPaginationAsync();
            }
        }

        private async void PageTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                if (int.TryParse(CurrentPageTextBox.Text, out int pageNumber))
                {
                    if (pageNumber >= 1 && pageNumber <= _totalPages)
                    {
                        _currentPage = pageNumber;
                        await LoadModsWithPaginationAsync();
                    }
                    else
                    {
                        CurrentPageTextBox.Text = _currentPage.ToString();
                        MessageBox.Show($"Please enter a page number between 1 and {_totalPages}", "Invalid Page",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                else
                {
                    CurrentPageTextBox.Text = _currentPage.ToString();
                }
            }
        }

        private void UpdatePaginationUI()
        {
            CurrentPageTextBox.Text = _currentPage.ToString();
            TotalPagesText.Text = $" of {_totalPages}";

            // Update item range display
            if (_totalItems > 0)
            {
                var startItem = ((_currentPage - 1) * _itemsPerPage) + 1;
                var endItem = Math.Min(_currentPage * _itemsPerPage, _totalItems);
                ItemsRangeText.Text = $"({startItem:N0}-{endItem:N0} of {_totalItems:N0} items)";
            }
            else
            {
                ItemsRangeText.Text = "(0 items)";
            }

            // Enable/disable navigation buttons
            FirstPageButton.IsEnabled = _currentPage > 1;
            PreviousPageButton.IsEnabled = _currentPage > 1;
            NextPageButton.IsEnabled = _currentPage < _totalPages;
            LastPageButton.IsEnabled = _currentPage < _totalPages;

            // Handle edge cases
            if (_totalPages == 0)
            {
                FirstPageButton.IsEnabled = false;
                PreviousPageButton.IsEnabled = false;
                NextPageButton.IsEnabled = false;
                LastPageButton.IsEnabled = false;
                CurrentPageTextBox.IsEnabled = false;
            }
            else
            {
                CurrentPageTextBox.IsEnabled = true;
            }
        }

        // Download mod button click
        private async void DownloadMod_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var mod = button?.Tag as ModrinthProject;

            if (mod == null) return;

            try
            {
                button.IsEnabled = false;
                button.Content = "Loading...";

                var versions = await _modrinthService.GetProjectVersionsAsync(mod.ProjectId);

                if (versions == null || !versions.Any())
                {
                    MessageBox.Show("No versions found for this mod.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    // We must return here, but first, restore the button's original state.
                    button.Content = "Download";
                    button.IsEnabled = true;
                    return;
                }

                // Restore the button's state before showing the dialog, as the dialog is a blocking operation.
                button.Content = "Download";
                button.IsEnabled = true;

                var versionSelectorWindow = new VersionSelectorWindow(versions);

                // ShowDialog() is a "modal" operation, which means it will pause the execution of this method
                // until the user either clicks "Select" or "Cancel" (or closes the window).
                bool? result = versionSelectorWindow.ShowDialog();

                if (result != true)
                {
                    return; // User cancelled the selection.
                }

                var chosenVersion = versionSelectorWindow.SelectedVersion;

                if (chosenVersion?.Files?.Any() == true)
                {
                    button.IsEnabled = false; // Disable the button again for the download itself.
                    button.Content = "Downloading...";

                    var primaryFile = chosenVersion.Files.FirstOrDefault(f => f.Primary) ?? chosenVersion.Files.First();

                    var progressWindow = new ProgressWindow($"Downloading {mod.Title} (v{chosenVersion.Name})...");
                    progressWindow.Show();

                    var progress = new Progress<int>(percentage =>
                    {
                        progressWindow.UpdateProgress(percentage);
                    });

                    var modsPath = System.IO.Path.Combine(MinecraftPaths.MinecraftRoot, "mods");
                    Directory.CreateDirectory(modsPath);

                    bool success = await _modrinthService.DownloadModAsync(primaryFile, modsPath, progress);
                    progressWindow.Close();

                    if (success)
                    {
                        MessageBox.Show($"Successfully downloaded {mod.Title}!", "Download Complete",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        button.Content = "Downloaded"; // Leave the button in a "Downloaded" state
                        button.Background = System.Windows.Media.Brushes.LightGreen;
                        button.IsEnabled = false; // Prevent re-downloading
                    }
                }
                else
                {
                    MessageBox.Show("No download files found for the selected version.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error getting mod versions or downloading: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // This 'finally' block ensures the button is re-enabled if any error occurs
                // or if the download fails, UNLESS it has been successfully downloaded.
                if (button.Content.ToString() != "Downloaded")
                {
                    button.IsEnabled = true;
                    button.Content = "Download";
                }
            }
        }

        // Helper methods
        private void ShowLoading(bool isLoading)
        {
            LoadingText.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
            SearchButton.IsEnabled = !isLoading;

            // Disable pagination controls while loading
            if (!isLoading)
            {
                UpdatePaginationUI(); // This will set the correct enabled state
            }
            else
            {
                FirstPageButton.IsEnabled = false;
                PreviousPageButton.IsEnabled = false;
                NextPageButton.IsEnabled = false;
                LastPageButton.IsEnabled = false;
                CurrentPageTextBox.IsEnabled = false;
            }
        }

        private string GetSelectedComboBoxValue(ComboBox comboBox)
        {
            var selected = comboBox.SelectedItem as ComboBoxItem;
            var content = selected?.Content?.ToString();

            if (content?.StartsWith("All ") == true)
                return "";

            return content ?? "";
        }

        private string GetSelectedComboBoxTag(ComboBox comboBox)
        {
            var selected = comboBox.SelectedItem as ComboBoxItem;
            return selected?.Tag?.ToString();
        }

        private void LoadInstalledVersions()
        {
            if (!MinecraftPaths.MinecraftExists) return;

            var versionFolders = Directory.GetDirectories(MinecraftPaths.VersionsFolder);
            var validVersions = new List<string>();

            foreach (var folder in versionFolders)
            {
                string versionName = Path.GetFileName(folder);
                string jsonFile = Path.Combine(folder, $"{versionName}.json");

                if (!File.Exists(jsonFile))
                {
                    // Fallback to any JSON file in the folder
                    jsonFile = Directory.GetFiles(folder, "*.json").FirstOrDefault();
                }

                if (jsonFile == null) continue;

                try
                {
                    var jsonText = File.ReadAllText(jsonFile);
                    var versionInfo = JsonSerializer.Deserialize<MCVersionInfo>(jsonText, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (versionInfo?.Id != null)
                    {
                        validVersions.Add(versionInfo.Id);
                    }
                }
                catch (Exception ex)
                {
                }
            }

            GameVersionPicker.ItemsSource = validVersions;
            if (GameVersionPicker.Items.Count > 0)
                GameVersionPicker.SelectedIndex = GameVersionPicker.Items.Count - 1; // latest
        }
        private void LaunchSelectedVersion(object sender, RoutedEventArgs e)
        {
            string selectedVersion = GameVersionPicker.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(selectedVersion))
            {
                MessageBox.Show("Please select a version!");
                return;
            }

            try
            {
                var launcher = new SimplifiedMinecraftLauncher();
                launcher.LaunchMinecraft(selectedVersion);
                MessageBox.Show($"Minecraft {selectedVersion} launched!");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to launch Minecraft: {ex.Message}");
            }
        }

        public class SimplifiedMinecraftLauncher
        {
            private string minecraftDir;
            private string javaPath;

            private MCVersionInfo GetMergedVersionInfo(string version)
            {
                var versionDir = Path.Combine(MinecraftPaths.MinecraftRoot, "versions", version);
                var versionJsonPath = Path.Combine(versionDir, $"{version}.json");

                if (!File.Exists(versionJsonPath))
                {
                    throw new Exception($"Version JSON not found for {version}");
                }

                var jsonText = File.ReadAllText(versionJsonPath);
                var versionInfo = JsonSerializer.Deserialize<MCVersionInfo>(jsonText, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                // If this version inherits from another, load and merge them
                if (!string.IsNullOrEmpty(versionInfo.InheritsFrom))
                {
                    var baseVersionInfo = GetMergedVersionInfo(versionInfo.InheritsFrom);

                    // Merge: The modded version's properties override the base's
                    baseVersionInfo.Id = versionInfo.Id; // Use the final version ID
                    if (!string.IsNullOrEmpty(versionInfo.MainClass))
                    {
                        baseVersionInfo.MainClass = versionInfo.MainClass;
                    }
                    if (versionInfo.Arguments != null)
                    {
                        // Simple merge: Add modded arguments to the base arguments
                        if (versionInfo.Arguments.Game != null)
                            baseVersionInfo.Arguments.Game.AddRange(versionInfo.Arguments.Game);
                        if (versionInfo.Arguments.Jvm != null)
                            baseVersionInfo.Arguments.Jvm.AddRange(versionInfo.Arguments.Jvm);
                    }

                    // Prepend modded libraries to ensure they are loaded first
                    if (versionInfo.Libraries != null)
                    {
                        baseVersionInfo.Libraries.InsertRange(0, versionInfo.Libraries);
                    }

                    return baseVersionInfo;
                }

                return versionInfo;
            }

            public SimplifiedMinecraftLauncher()
            {
                minecraftDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft");
                javaPath = FindJavaExecutable();

                if (string.IsNullOrEmpty(javaPath))
                {
                    throw new Exception("Java not found! Please install Java.");
                }
            }

            public void LaunchMinecraft(string version)
            {
                // Use the new method to get the complete, merged version info
                var versionInfo = GetMergedVersionInfo(version);

                var versionDir = Path.Combine(minecraftDir, "versions", version);
                var versionJar = Path.Combine(versionDir, $"{version}.jar");

                // For inherited versions, the JAR is often the parent's JAR
                if (!File.Exists(versionJar) && !string.IsNullOrEmpty(versionInfo.InheritsFrom))
                {
                    versionJar = Path.Combine(minecraftDir, "versions", versionInfo.InheritsFrom, $"{versionInfo.InheritsFrom}.jar");
                }

                if (!File.Exists(versionJar))
                {
                    throw new Exception($"Version JAR not found for {version}");
                }

                var nativesDir = Path.Combine(versionDir, $"{version}-natives");
                SetupNatives(versionInfo, nativesDir);

                var classpath = BuildClasspath(versionInfo, versionJar);
                var cmd = BuildLaunchCommand(versionInfo, version, classpath, nativesDir); // Pass the full versionInfo

                var startInfo = new ProcessStartInfo
                {
                    FileName = javaPath,
                    Arguments = cmd,
                    UseShellExecute = false,
                    WorkingDirectory = minecraftDir,
                    CreateNoWindow = false
                };

                Process.Start(startInfo);
            }

            private void SetupNatives(MCVersionInfo versionInfo, string nativesDir)
            {
                if (Directory.Exists(nativesDir))
                {
                    Directory.Delete(nativesDir, true);
                }
                Directory.CreateDirectory(nativesDir);

                if (versionInfo.Libraries == null) return;

                foreach (var library in versionInfo.Libraries)
                {
                    if (library.Natives?.ContainsKey("windows") == true)
                    {
                        var nativeJar = GetLibraryPath(library.Name + ":" + library.Natives["windows"]);
                        if (File.Exists(nativeJar))
                        {
                            ExtractNativesFromJar(nativeJar, nativesDir);
                        }
                    }
                }
            }

            private void ExtractNativesFromJar(string jarPath, string nativesDir)
            {
                using (var archive = ZipFile.OpenRead(jarPath))
                {
                    foreach (var entry in archive.Entries)
                    {
                        if (entry.Name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                        {
                            var destPath = Path.Combine(nativesDir, entry.Name);
                            entry.ExtractToFile(destPath, true);
                        }
                    }
                }
            }

            private string BuildClasspath(MCVersionInfo versionInfo, string versionJar)
            {
                var classpath = new List<string> { versionJar };

                if (versionInfo.Libraries != null)
                {
                    foreach (var library in versionInfo.Libraries)
                    {
                        // Skip native-only libraries
                        if (library.Natives != null) continue;

                        var libPath = GetLibraryPath(library.Name);
                        if (File.Exists(libPath))
                        {
                            classpath.Add(libPath);
                        }
                    }
                }

                return string.Join(";", classpath);
            }

            private string GetLibraryPath(string libraryName)
            {
                // Parse: group:artifact:version[:classifier]
                var parts = libraryName.Split(':');
                if (parts.Length < 3) return null;

                var group = parts[0].Replace('.', Path.DirectorySeparatorChar);
                var artifact = parts[1];
                var version = parts[2];
                var classifier = parts.Length > 3 ? $"-{parts[3]}" : "";

                var jarName = $"{artifact}-{version}{classifier}.jar";
                return Path.Combine(minecraftDir, "libraries", group, artifact, version, jarName);
            }

            private string BuildLaunchCommand(MCVersionInfo versionInfo, string version, string classpath, string nativesDir)
            {
                var args = new List<string>();

                // 1. Process JVM Arguments from JSON
                if (versionInfo.Arguments?.Jvm != null)
                {
                    foreach (var argObj in versionInfo.Arguments.Jvm)
                    {
                        if (argObj is JsonElement element && element.ValueKind == JsonValueKind.String)
                        {
                            args.Add(element.GetString());
                        }
                    }
                }

                // Add Main Class - It's technically a JVM argument
                args.Add(versionInfo.MainClass);

                // 2. Process Game Arguments from JSON
                if (versionInfo.Arguments?.Game != null)
                {
                    foreach (var argObj in versionInfo.Arguments.Game)
                    {
                        if (argObj is JsonElement element && element.ValueKind == JsonValueKind.String)
                        {
                            args.Add(element.GetString());
                        }
                    }
                }

                // 3. Define placeholders with RAW, UNQUOTED values
                var replacements = new Dictionary<string, string>
    {
        { "${auth_player_name}", "Player" },
        { "${version_name}", version },
        { "${game_directory}", minecraftDir }, // No quotes!
        { "${assets_root}", Path.Combine(minecraftDir, "assets") }, // No quotes!
        { "${assets_index_name}", versionInfo.AssetIndex?.Id ?? versionInfo.InheritsFrom ?? version },
        { "${auth_uuid}", "00000000-0000-0000-0000-000000000000" },
        { "${auth_access_token}", "0" },
        { "${user_type}", "legacy" },
        { "${version_type}", "release" },
        { "${natives_directory}", nativesDir }, // No quotes!
        { "${launcher_name}", "MyLauncher" },
        { "${launcher_version}", "1.0" },
        { "${classpath}", classpath } // No quotes!
    };

                // 4. Perform placeholder replacement safely
                for (int i = 0; i < args.Count; i++)
                {
                    foreach (var pair in replacements)
                    {
                        args[i] = args[i].Replace(pair.Key, pair.Value);
                    }
                }

                // Prepend essential JVM arguments that aren't in the JSON list
                var finalJvmArgs = new List<string>
    {
        "-Xmx4G",
        $"-Djava.library.path={nativesDir}" // No quotes here yet
    };
                args.InsertRange(0, finalJvmArgs);

                // 5. Final, critical step: Quote any argument that contains spaces.
                for (int i = 0; i < args.Count; i++)
                {
                    // The classpath argument (-cp) needs special handling as its value is next
                    if (args[i] == "-cp" && i + 1 < args.Count)
                    {
                        // Quote the classpath value which is the next argument
                        args[i + 1] = $"\"{args[i + 1]}\"";
                    }
                    else if (args[i].Contains(' ') && !args[i].StartsWith("\""))
                    {
                        // Quote any other argument that has a space
                        args[i] = $"\"{args[i]}\"";
                    }
                }

                return string.Join(" ", args);
            }

            private string FindJavaExecutable()
            {
                // Try common locations
                var candidates = new[]
                {
            "java",
            @"C:\Program Files\Java\jdk-17.0.2\bin\java.exe",
            @"C:\Program Files\Java\jre-1.8\bin\java.exe",
            @"C:\Program Files (x86)\Java\jre1.8.0_291\bin\java.exe"
        };

                foreach (var candidate in candidates)
                {
                    try
                    {
                        var process = Process.Start(new ProcessStartInfo
                        {
                            FileName = candidate,
                            Arguments = "-version",
                            UseShellExecute = false,
                            RedirectStandardError = true,
                            CreateNoWindow = true
                        });

                        process.WaitForExit();
                        if (process.ExitCode == 0)
                        {
                            return candidate;
                        }
                    }
                    catch { }
                }

                // Check JAVA_HOME
                var javaHome = Environment.GetEnvironmentVariable("JAVA_HOME");
                if (!string.IsNullOrEmpty(javaHome))
                {
                    var javaExe = Path.Combine(javaHome, "bin", "java.exe");
                    if (File.Exists(javaExe))
                        return javaExe;
                }

                return null;
            }
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            _modrinthService?.Dispose();
        }
    }
}
