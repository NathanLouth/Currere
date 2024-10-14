using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Drawing; // For icons
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Threading;
using Microsoft.Win32; // For accessing the Registry

namespace Currere
{
    public partial class MainWindow : Window
    {
        // List to store the selected programs
        private List<string> _selectedPrograms = new List<string>();
        private const string RegistryKeyPath = @"Software\Currere"; // Registry key path

        public MainWindow()
        {
            InitializeComponent();
            string currentUserName = Environment.UserName;
            CurrentUserLabel.Content = $"Running as:  {currentUserName}";

            LoadProgramsFromRegistry(); // Load programs from the registry on startup
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }

            if (e.ChangedButton == MouseButton.Right)
            {
                this.WindowState = WindowState.Minimized; // Minimize on right click
            }
        }

        private void CloseWindow(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }

        private void AboutClicked(object sender, MouseButtonEventArgs e)
        {
            MessageBox.Show("V0.0.1", "About Currere", MessageBoxButton.OK);
        }

        private void AddExecutable_Click(object sender, RoutedEventArgs e)
        {
            // Open file dialog to select an executable
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Filter = "Executable Files (*.exe)|*.exe|All Files (*.*)|*.*";

            if (openFileDialog.ShowDialog() == true)
            {
                string selectedFile = openFileDialog.FileName;
                _selectedPrograms.Add(selectedFile);
                AddProgramToList(selectedFile);
                SaveProgramToRegistry(selectedFile); // Save program path to registry
            }
        }

        private void LoadProgramsFromRegistry()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath))
                {
                    if (key != null)
                    {
                        var programList = key.GetValue("ProgramList") as string;
                        if (!string.IsNullOrEmpty(programList))
                        {
                            // Correctly split the program list from the registry
                            var programs = programList.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                            foreach (var program in programs)
                            {
                                _selectedPrograms.Add(program);
                                AddProgramToList(program); // Add to the UI
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading programs from registry: {ex.Message}");
            }
        }

        private void SaveProgramToRegistry(string programPath)
        {
            try
            {
                using (var key = Registry.CurrentUser.CreateSubKey(RegistryKeyPath))
                {
                    if (key != null)
                    {
                        // Store the program paths as a semicolon-separated string
                        var existingPrograms = key.GetValue("ProgramList") as string;
                        var updatedPrograms = existingPrograms?.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToList() ?? new List<string>();

                        // Only add if it's not already in the list
                        if (!updatedPrograms.Contains(programPath))
                        {
                            updatedPrograms.Add(programPath);
                            key.SetValue("ProgramList", string.Join(";", updatedPrograms));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving program to registry: {ex.Message}");
            }
        }

        private void AddProgramToList(string programPath)
        {
            var programImage = new System.Windows.Controls.Image
            {
                Source = GetIconFromFile(programPath),
                Width = 64,
                Height = 64,
                Margin = new Thickness(5)
            };

            // Set BitmapScalingMode for high-quality rendering
            RenderOptions.SetBitmapScalingMode(programImage, BitmapScalingMode.HighQuality);

            var programName = new TextBlock
            {
                Text = System.IO.Path.GetFileNameWithoutExtension(programPath),
                Foreground = System.Windows.Media.Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var stackPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Children = { programImage, programName },
                Margin = new Thickness(5),
                Background = new SolidColorBrush(Colors.Transparent) // Default background
            };

            // Event handler for mouse enter
            stackPanel.MouseEnter += (s, e) =>
            {
                programImage.Opacity = 0.8; // Slightly dim the icon
                stackPanel.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 30, 30, 30)); // Dark background on hover
            };

            // Event handler for mouse leave
            stackPanel.MouseLeave += (s, e) =>
            {
                programImage.Opacity = 1.0; // Reset the opacity
                stackPanel.Background = new SolidColorBrush(Colors.Transparent); // Reset background
            };

            stackPanel.MouseDown += (s, e) =>
            {
                System.Diagnostics.Process.Start(programPath);
            };

            ProgramWrapPanel.Children.Add(stackPanel);
        }

        private BitmapImage GetIconFromFile(string filePath)
        {
            try
            {
                using (var icon = System.Drawing.Icon.ExtractAssociatedIcon(filePath))
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        icon.ToBitmap().Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
                        memoryStream.Position = 0;

                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.StreamSource = memoryStream;
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        bitmap.Freeze();
                        return bitmap;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error extracting icon: {ex.Message}");
                return null;
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = SearchBox.Text.ToLower();
            var filteredPrograms = _selectedPrograms
                .Where(p => System.IO.Path.GetFileNameWithoutExtension(p).ToLower().Contains(searchText))
                .ToList();

            ProgramWrapPanel.Children.Clear();

            foreach (var program in filteredPrograms)
            {
                AddProgramToList(program);
            }

        }

        private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
        {

        }

        private void SearchBox_LostFocus(object sender, RoutedEventArgs e)
        {

        }

        private void Window_Closed(object sender, EventArgs e)
        {
            // Optional: Clean up resources or save state if needed
        }
    }
}
