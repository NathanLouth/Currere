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
                Margin = new Thickness(0) // Remove margin from the image
            };

            // Set BitmapScalingMode for high-quality rendering
            RenderOptions.SetBitmapScalingMode(programImage, BitmapScalingMode.HighQuality);

            var programName = new TextBlock
            {
                Text = System.IO.Path.GetFileNameWithoutExtension(programPath),
                Foreground = System.Windows.Media.Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            // Create a Border for rounded corners
            var border = new Border
            {
                CornerRadius = new CornerRadius(10), // Adjust the radius as needed
                Margin = new Thickness(5),
                Padding = new Thickness(5), // Add padding of 5 pixels
                Background = new SolidColorBrush(Colors.Transparent) // Default background
            };

            var stackPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Children = { programImage, programName }
            };

            // Add stackPanel to the border
            border.Child = stackPanel;

            // Event handler for mouse enter
            border.MouseEnter += (s, e) =>
            {
                programImage.Opacity = 0.8; // Slightly dim the icon
                border.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 30, 30, 30)); // Dark background on hover
            };

            // Event handler for mouse leave
            border.MouseLeave += (s, e) =>
            {
                programImage.Opacity = 1.0; // Reset the opacity
                border.Background = new SolidColorBrush(Colors.Transparent); // Reset background
            };

            // Event handler for left mouse click (to launch the program)
            border.MouseDown += (s, e) =>
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    System.Diagnostics.Process.Start(programPath);
                }
                else if (e.RightButton == MouseButtonState.Pressed) // Handle right-click
                {
                    RemoveProgram(border, programPath); // Pass the border instead of stackPanel
                }
            };

            ProgramWrapPanel.Children.Add(border);
        }



        private void RemoveProgram(Border border, string programPath)
        {
            // Create the confirmation window and set its owner
            ConfirmationWindow confirmationWindow = new ConfirmationWindow
            {
                Owner = this // Set the owner to the current MainWindow
            };

            confirmationWindow.ShowDialog(); // Show the dialog and wait for user response

            // Check if the user confirmed the removal
            if (confirmationWindow.IsConfirmed)
            {
                // Remove from UI
                ProgramWrapPanel.Children.Remove(border);

                // Remove from the list of selected programs
                _selectedPrograms.Remove(programPath);

                // Update the registry
                RemoveProgramFromRegistry(programPath);
            }
        }




        private void RemoveProgramFromRegistry(string programPath)
        {
            try
            {
                using (var key = Registry.CurrentUser.CreateSubKey(RegistryKeyPath))
                {
                    if (key != null)
                    {
                        var existingPrograms = key.GetValue("ProgramList") as string;
                        if (!string.IsNullOrEmpty(existingPrograms))
                        {
                            var updatedPrograms = existingPrograms
                                .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                                .Where(p => p != programPath) // Remove the program being deleted
                                .ToList();

                            // Update the registry only if there are remaining programs
                            key.SetValue("ProgramList", string.Join(";", updatedPrograms));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error removing program from registry: {ex.Message}");
            }
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

        private void OpenAsOtherUser_Click(object sender, RoutedEventArgs e)
        {
            // Create a new window for entering credentials
            var credentialWindow = new CredentialWindow
            {
                Owner = this // Set the owner to the current MainWindow
            };

            if (credentialWindow.ShowDialog() == true)
            {
                string username = credentialWindow.Username;
                string password = credentialWindow.Password;

                // Create a process start info for running as another user
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = System.Reflection.Assembly.GetExecutingAssembly().Location, // Path to the executable
                    UserName = username,
                    Password = ConvertToSecureString(password), // Convert password to SecureString
                    Domain = "", // Specify domain if needed
                    UseShellExecute = false,
                    LoadUserProfile = true
                };

                try
                {
                    // Start the new process
                    System.Diagnostics.Process.Start(startInfo);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error starting process: {ex.Message}");
                }

                // Close the original application
                Application.Current.Shutdown();
            }
        }

        // Method to convert password string to SecureString
        private System.Security.SecureString ConvertToSecureString(string password)
        {
            var secureString = new System.Security.SecureString();
            foreach (char c in password)
            {
                secureString.AppendChar(c);
            }
            return secureString;
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
