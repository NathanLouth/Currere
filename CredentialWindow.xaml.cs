using System.Windows;

namespace Currere
{
    public partial class CredentialWindow : Window
    {
        public string Username => UsernameTextBox.Text;
        public string Password => PasswordBox.Password;

        public CredentialWindow()
        {
            InitializeComponent();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true; // Set DialogResult to true to indicate OK was clicked
            Close(); // Close the window
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false; // Set DialogResult to false to indicate Cancel was clicked
            Close(); // Close the window
        }
    }
}
