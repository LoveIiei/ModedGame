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
                string selectedId = FileTypePicker.SelectedValue.ToString();
                string folderPath = null;
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

                // Get the latest version
                var versions = await _modrinthService.GetProjectVersionsAsync(mod.ProjectId);
                var latestVersion = versions.FirstOrDefault();

                if (latestVersion?.Files?.Any() == true)
                {
                    var primaryFile = latestVersion.Files.FirstOrDefault(f => f.Primary) ??
                                     latestVersion.Files.First();

                    var progressWindow = new ProgressWindow($"Downloading {mod.Title}...");
                    progressWindow.Show();

                    var progress = new Progress<int>(percentage =>
                    {
                        progressWindow.UpdateProgress(percentage);
                    });

                    // Download to mods folder
                    var modsPath = System.IO.Path.Combine(MinecraftPaths.MinecraftRoot, "mods");
                    Directory.CreateDirectory(modsPath);

                    bool success = await _modrinthService.DownloadModAsync(primaryFile, modsPath, progress);
                    progressWindow.Close();
                    if (success)
                    {
                        MessageBox.Show($"Successfully downloaded {mod.Title}!", "Download Complete",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        button.Content = "Downloaded";
                        button.Background = System.Windows.Media.Brushes.LightGreen;
                    }
                }
                else
                {
                    MessageBox.Show("No download files found for this mod.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error downloading mod: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                button.IsEnabled = true;
                if (button.Content.ToString() == "Loading...")
                    button.Content = "Download";
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

                    if (versionInfo?.Type == "release" || versionInfo?.Type == "snapshot")
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
                var versionDir = Path.Combine(minecraftDir, "versions", version);
                var versionJson = Path.Combine(versionDir, $"{version}.json");
                var versionJar = Path.Combine(versionDir, $"{version}.jar");

                if (!File.Exists(versionJson) || !File.Exists(versionJar))
                {
                    throw new Exception($"Version files not found for {version}");
                }

                // Parse version info
                var versionInfo = JsonSerializer.Deserialize<MCVersionInfo>(
                    File.ReadAllText(versionJson),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                // Extract and setup natives
                var nativesDir = Path.Combine(versionDir, $"{version}-natives");
                SetupNatives(versionInfo, nativesDir);

                // Build classpath
                var classpath = BuildClasspath(versionInfo, versionJar);

                // Build command
                var cmd = BuildLaunchCommand(versionInfo, version, classpath, nativesDir);

                // Launch
                var startInfo = new ProcessStartInfo
                {
                    FileName = javaPath,
                    Arguments = cmd,
                    UseShellExecute = false,
                    WorkingDirectory = minecraftDir,
                    CreateNoWindow = false
                };

                var process = Process.Start(startInfo);

                // Check if it crashed immediately
                System.Threading.Thread.Sleep(2000);
                if (process.HasExited)
                {
                    throw new Exception($"Minecraft crashed immediately. Exit code: {process.ExitCode}");
                }
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

            private string BuildLaunchCommand(MCVersionInfo versionInfo, string version, string classpath, string nativesDir, bool isModded = false)
            {
                var args = new List<string>();

                // Memory settings - increase for modded versions
                if (isModded)
                {
                    args.Add("-Xmx4G");
                    args.Add("-Xms2G");
                }
                else
                {
                    args.Add("-Xmx2G");
                    args.Add("-Xms1G");
                }

                // Additional JVM args for better performance
                args.Add("-XX:+UseG1GC");
                args.Add("-XX:+ParallelRefProcEnabled");
                args.Add("-XX:MaxGCPauseMillis=200");
                args.Add("-XX:+UnlockExperimentalVMOptions");
                args.Add("-XX:+DisableExplicitGC");
                args.Add("-XX:G1NewSizePercent=30");
                args.Add("-XX:G1MaxNewSizePercent=40");
                args.Add("-XX:G1HeapRegionSize=8M");
                args.Add("-XX:G1ReservePercent=20");
                args.Add("-XX:G1HeapWastePercent=5");
                args.Add("-XX:G1MixedGCCountTarget=4");
                args.Add("-XX:InitiatingHeapOccupancyPercent=15");
                args.Add("-XX:G1MixedGCLiveThresholdPercent=90");
                args.Add("-XX:G1RSetUpdatingPauseTimePercent=5");
                args.Add("-XX:SurvivorRatio=32");
                args.Add("-XX:+PerfDisableSharedMem");
                args.Add("-XX:MaxTenuringThreshold=1");

                // Natives library path
                args.Add($"-Djava.library.path=\"{nativesDir}\"");

                // System properties
                args.Add("-Dfml.ignoreInvalidMinecraftCertificates=true");
                args.Add("-Dfml.ignorePatchDiscrepancies=true");

                // Classpath
                args.Add("-cp");
                args.Add($"\"{classpath}\"");

                // Main class
                args.Add(versionInfo.MainClass ?? "net.minecraft.client.main.Main");

                // Game arguments
                var assetsDir = Path.Combine(minecraftDir, "assets");
                var assetIndex = versionInfo.AssetIndex?.Id ?? version;

                args.AddRange(new[]
                {
            "--username", "Player",
            "--version", version,
            "--gameDir", $"\"{minecraftDir}\"",
            "--assetsDir", $"\"{assetsDir}\"",
            "--assetIndex", assetIndex,
            "--uuid", "00000000-0000-0000-0000-000000000000",
            "--accessToken", "0",
            "--userType", "legacy"
        });

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
