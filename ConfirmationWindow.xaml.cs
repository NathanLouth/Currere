using System.Windows;

namespace Currere
{
    public partial class ConfirmationWindow : Window
    {
        public bool IsConfirmed { get; private set; }

        public ConfirmationWindow()
        {
            InitializeComponent();
            IsConfirmed = false;
        }

        private void YesButton_Click(object sender, RoutedEventArgs e)
        {
            IsConfirmed = true;
            this.Close();
        }

        private void NoButton_Click(object sender, RoutedEventArgs e)
        {
            IsConfirmed = false;
            this.Close();
        }
    }
}
