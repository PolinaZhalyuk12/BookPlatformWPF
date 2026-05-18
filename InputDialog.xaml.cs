using System.Windows;

namespace BookPlatformWPF
{
    public partial class InputDialog : Window
    {
        public string ResponseText { get; private set; }

        public InputDialog(string message, string title = "Ввод")
        {
            InitializeComponent();

            Title = title;
            txtMessage.Text = message;
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            ResponseText = txtInput.Text;
            DialogResult = true;
            Close();
        }
    }
}