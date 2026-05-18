using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace BookPlatformWPF
{
    public partial class ProfileView : UserControl
    {
        public ProfileView(Users user)
        {
            InitializeComponent();
            DataContext = new ProfileViewModel(user);  
        }

        public ProfileView()
        {
            InitializeComponent();
            DataContext = new ProfileViewModel();
        }
        private void RequestAuthor_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Заявка на получение роли «Автор» успешно отправлена!\n\nОжидайте рассмотрения администратором.",
                            "Заявка отправлена",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
        }
        private void Unregistration_Click(object sender, RoutedEventArgs e)
        {
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Show();
            Window.GetWindow(this)?.Close();
        }
        private void AppealFreeze_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ProfileViewModel vm &&
                vm.User != null)
            {
                bool exists = Core.Context.UnfreezeRequests.Any(r =>
                    r.UserID == vm.User.UserID);

                if (exists)
                {
                    MessageBox.Show(
                        "Заявка уже отправлена.",
                        "Информация",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    return;
                }

                Core.Context.UnfreezeRequests.Add(new UnfreezeRequests
                {
                    UserID = vm.User.UserID,
                    Reason = "Пользователь оспаривает заморозку аккаунта.",
                    CreatedAt = DateTime.Now
                });

                Core.Context.SaveChanges();

                MessageBox.Show(
                    "Заявка на разморозку отправлена.",
                    "Успех",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }
    }
}