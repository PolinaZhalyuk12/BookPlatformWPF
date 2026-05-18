using System.Windows;

namespace BookPlatformWPF
{
    public partial class MainWindow : Window
    {
        public Users CurrentUser { get; private set; }
        
        public MainWindow()
        {
            InitializeComponent();
        }

        public MainWindow(Users user) : this()
        {
            CurrentUser = user;

            bool isFrozen = user?.IsFrozen == true;

            string roleName = user?.Roles?.RoleName ?? "";

            if (isFrozen)
            {
                btnAuthor.Visibility = Visibility.Collapsed;
                btnAdmin.Visibility = Visibility.Collapsed;

                btnCatalog.Visibility = Visibility.Collapsed;
                btnLists.Visibility = Visibility.Collapsed;

                ContentArea.Content = new ProfileView(CurrentUser);

                MessageBox.Show(
                    "Ваш аккаунт заморожен.\nДоступна только страница профиля.",
                    "Аккаунт заморожен",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }


            btnAuthor.Visibility =
                (roleName == "Автор" || roleName == "Администратор")
                ? Visibility.Visible
                : Visibility.Collapsed;

            btnAdmin.Visibility =
                (roleName == "Администратор")
                ? Visibility.Visible
                : Visibility.Collapsed;

            ContentArea.Content = new CatalogView();
        }

        private void BtnCatalog_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentUser?.IsFrozen == true)
                return;

            ContentArea.Content = new CatalogView();
        }
        private void BtnLists_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentUser?.IsFrozen == true)
                return;

            ContentArea.Content = new ReadingListsView(CurrentUser);
        }
        private void BtnAuthor_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentUser?.IsFrozen == true)
                return;

            if (CurrentUser?.Roles?.RoleName == "Автор" ||
                CurrentUser?.Roles?.RoleName == "Администратор")
            {
                ContentArea.Content = new AuthorView(CurrentUser);
            }
            else
            {
                MessageBox.Show(
                    "Эта страница доступна только авторам!",
                    "Доступ запрещён");
            }
        }
        private void BtnAdmin_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentUser?.IsFrozen == true)
                return;

            ContentArea.Content = new AdminView();
        }
        private void BtnProfile_Click(object sender, RoutedEventArgs e) => ContentArea.Content = new ProfileView(CurrentUser);
    }
}