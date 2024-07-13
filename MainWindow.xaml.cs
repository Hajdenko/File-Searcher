using Newtonsoft.Json;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace File_Searcher
{
    public class SearchJSON
    {
        public required string SearchTerm { get; set; }
        public required string SearchLocation { get; set; }
        public required Dictionary<string, string> Shortcuts { get; set; }
    }

    public partial class MainWindow : Window
    {
        private List<string> allFoundFiles = new List<string>();
        private CancellationTokenSource? cancellationTokenSource;
        private String ifcantFix = "\n\nIf you don't know how to fix this, reach out to me on discord. hajdenkoo";
        private SearchJSON json;
        private static readonly string JsonFilePath = "search_json.json";
        private bool logErrors;

        public MainWindow()
        {
            InitializeComponent();

            json = load_json();

            SearchInput.Text = json.SearchTerm;
            LocationInput.Text = json.SearchLocation;

            ConsoleOutput.Items.Add("Welcome, everything has loaded successfully.");
            ConsoleOutput.Items.Add("Here you'll be able to see every file my system has found.");
            ConsoleOutput.Items.Add("For more (sometimes) advanced searching enable the administrator mode.");

            ConsoleSearchInput.TextChanged += searchInput_changed;
            LogErrorsCheckBox.Checked += (s, e) => logErrors = true;
            LogErrorsCheckBox.Unchecked += (s, e) => logErrors = false;
            logErrors = LogErrorsCheckBox.IsChecked == true;
        }

        private void close_button(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void minimize_button(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void drag_window_event(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private async void search_button(object sender, RoutedEventArgs e)
        {
            string searchTerm = SearchInput.Text;
            string searchLocation = LocationInput.Text;

            if (json.Shortcuts.ContainsKey(searchLocation))
            {
                searchLocation = json.Shortcuts[searchLocation];
            }

            if (searchLocation.Length == 2 && searchLocation[1] == ':')
            {
                searchLocation += "/";
            }

            if (string.IsNullOrWhiteSpace(searchTerm) || string.IsNullOrWhiteSpace(searchLocation))
            {
                MessageBox.Show("Enter Search Term and Search Location.");
                return;
            }

            if (!Directory.Exists(searchLocation))
            {
                MessageBox.Show("Specified Search Location doesn't exist.");
                return;
            }

            ConsoleOutput.Items.Clear();
            ConsoleOutput.Items.Add("Searching...");
            allFoundFiles.Clear();

            cancellationTokenSource?.Cancel();
            cancellationTokenSource = new CancellationTokenSource();
            var token = cancellationTokenSource.Token;

            try
            {
                await Task.Run(() => search(searchTerm, searchLocation, token), token);

                json.SearchTerm = searchTerm;
                json.SearchLocation = LocationInput.Text;
                save_json(json);

                if (allFoundFiles.Count == 0)
                {
                    Dispatcher.Invoke(() => ConsoleOutput.Items.Add("Found nothing"));
                }
                else
                {
                    Dispatcher.Invoke(() => ConsoleOutput.Items.Add("Search ended"));
                }
            }
            catch (OperationCanceledException)
            {
                MessageBox.Show("Search Canceled.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error has occurred: {ex.Message}" + ifcantFix);
            }
        }

        private void search(string searchTerm, string searchLocation, CancellationToken token)
        {
            Queue<string> directories = new Queue<string>();
            directories.Enqueue(searchLocation);

            while (directories.Count > 0)
            {
                string currentDirectory = directories.Dequeue();
                try
                {
                    foreach (string subdirectory in Directory.GetDirectories(currentDirectory))
                    {
                        try
                        {
                            directories.Enqueue(subdirectory);
                        }
                        catch (UnauthorizedAccessException)
                        {
                            if (logErrors)
                                Dispatcher.Invoke(() => ConsoleOutput.Items.Add($"Unauthorized access to subdirectory {subdirectory}, skipping..."));
                        }
                        catch (Exception ex)
                        {
                            if (logErrors)
                                Dispatcher.Invoke(() => ConsoleOutput.Items.Add($"An error occurred while accessing subdirectory {subdirectory}: {ex.Message}, skipping..."));
                        }
                    }

                    foreach (string file in Directory.GetFiles(currentDirectory))
                    {
                        try
                        {
                            token.ThrowIfCancellationRequested();
                            if (Path.GetFileName(file).Contains(searchTerm))
                            {
                                Dispatcher.Invoke(() => add_consoleoutput(file));
                                allFoundFiles.Add(file);
                            }
                        }
                        catch (UnauthorizedAccessException)
                        {
                            if (logErrors)
                                Dispatcher.Invoke(() => ConsoleOutput.Items.Add($"Unauthorized access to file {file}, skipping..."));
                        }
                        catch (Exception ex)
                        {
                            if (logErrors)
                                Dispatcher.Invoke(() => ConsoleOutput.Items.Add($"An error occurred while accessing file {file}: {ex.Message}, skipping..."));
                        }
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    if (logErrors)
                        Dispatcher.Invoke(() => ConsoleOutput.Items.Add($"Unauthorized access to directory {currentDirectory}, skipping..."));
                }
                catch (Exception ex)
                {
                    if (logErrors)
                        Dispatcher.Invoke(() => ConsoleOutput.Items.Add($"An error occurred while accessing directory {currentDirectory}: {ex.Message}, skipping..."));
                }
            }
        }

        private void add_consoleoutput(string file)
        {
            TextBlock result = new TextBlock
            {
                Text = $"Found! {Path.GetFileName(file)} Click once to open PATH! or right click to copy",
                Foreground = new SolidColorBrush(Colors.LightGray),
                Cursor = Cursors.Hand,
                Tag = file
            };
            result.MouseLeftButtonDown += left_path;
            result.MouseRightButtonDown += right_copy;
            ConsoleOutput.Items.Add(result);
        }

        private void left_path(object sender, MouseButtonEventArgs e)
        {
            TextBlock textBlock = sender as TextBlock;
            if (textBlock != null)
            {
                string filePath = textBlock.Tag as string;
                if (filePath != null)
                {
                    string directory = Path.GetDirectoryName(filePath);
                    string args = $"\"{directory}\"";
                    ProcessStartInfo pfi = new ProcessStartInfo("explorer.exe", args);
                    Process.Start(pfi);
                }
            }
        }

        private void right_copy(object sender, MouseButtonEventArgs e)
        {
            TextBlock? textBlock = sender as TextBlock;
            if (textBlock != null)
            {
                string filePath = textBlock.Tag as string;
                if (filePath != null)
                {
                    Clipboard.SetText(filePath);
                    MessageBox.Show($"File path copied to clipboard: {filePath}");
                }
            }
        }

        private void searchInput_changed(object sender, TextChangedEventArgs e)
        {
            string searchText = ConsoleSearchInput.Text.ToLower();
            var filteredFiles = allFoundFiles.Where(file => Path.GetFileName(file).ToLower().Contains(searchText));
            bool isEmpty = !filteredFiles.Any();

            if (!isEmpty)
            {
                ConsoleOutput.Items.Clear();

                foreach (var file in filteredFiles)
                {
                    add_consoleoutput(file);
                }
            }
        }

        private void enable_admin(object sender, RoutedEventArgs e)
        {
            var exeName = Process.GetCurrentProcess().MainModule.FileName;
            var startInfo = new ProcessStartInfo(exeName)
            {
                Verb = "runas"
            };

            Process.Start(startInfo);
            Application.Current.Shutdown();
        }

        private SearchJSON load_json()
        {
            if (File.Exists(JsonFilePath))
            {
                string json = File.ReadAllText(JsonFilePath);
                return JsonConvert.DeserializeObject<SearchJSON>(json);
            }
            else
            {
                var json = new SearchJSON
                {
                    SearchTerm = "",
                    SearchLocation = "",
                    Shortcuts = new Dictionary<string, string>
                    {
                        { "$Desktop", Environment.GetFolderPath(Environment.SpecialFolder.Desktop) }
                    }
                };
                save_json(json);
                return json;
            }
        }

        private void save_json(SearchJSON json)
        {
            string _json = JsonConvert.SerializeObject(json, Formatting.Indented);
            File.WriteAllText(JsonFilePath, _json);
        }
    }
}
