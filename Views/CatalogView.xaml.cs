using System;
using System.Linq;
using System.Net.NetworkInformation;
using System.Windows;
using System.Windows.Controls;

namespace BookPlatformWPF
{
    public partial class CatalogView : UserControl
    {
        private CatalogViewModel _vm;
        public Users _currentUser { get; private set; }
        public CatalogView()
        {
            InitializeComponent();
            Loaded += CatalogView_Loaded;

        }

        private void CatalogView_Loaded(object sender, RoutedEventArgs e)
        {
            var mainWindow = Window.GetWindow(this) as MainWindow;

            if (mainWindow != null)
            {
                _currentUser = mainWindow.CurrentUser;
            }

            _vm = new CatalogViewModel();
            DataContext = _vm;

            LoadGenres();
        }

        private void LoadGenres()
        {
            var genres = Core.Context.Genres.ToList();
            cmbGenre.Items.Clear();
            cmbGenre.Items.Add(new ComboBoxItem { Content = "Все жанры", IsSelected = true });
            foreach (var genre in genres)
            {
                cmbGenre.Items.Add(new ComboBoxItem { Content = genre.Name });
            }
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void cmbGenre_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void cmbSort_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            if (_vm == null) return;

            string search = txtSearch?.Text ?? "";
            string genre = (cmbGenre.SelectedItem as ComboBoxItem)?.Content?.ToString();
            int sortIndex = cmbSort?.SelectedIndex ?? 0;

            _vm.ApplyFilters(search, genre, sortIndex);
        }

        private void OpenBook_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Books book)
            {
                var mainWindow = Window.GetWindow(this) as MainWindow;
                if (mainWindow != null)
                    mainWindow.ContentArea.Content = new BookView(book, mainWindow.CurrentUser);
            }
        }

        private void AddToList_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Books book)
            {
                MoveBookToStatus(book);
            }
        }
        private void MoveBookToStatus(Books book)
        {
            if (_currentUser == null || book == null)
            {
                MessageBox.Show(
                    "Не удалось определить пользователя или книгу",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            var window = new Window
            {
                Title = "Добавить книгу",
                Width = 340,
                Height = 240,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize
            };

            var panel = new StackPanel
            {
                Margin = new Thickness(20)
            };

            var titleText = new TextBlock
            {
                Text = $"Книга:\n«{book.Title}»",
                FontSize = 14,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 15)
            };

            var combo = new ComboBox
            {
                Margin = new Thickness(0, 0, 0, 20)
            };

            combo.Items.Add("In the plans");
            combo.Items.Add("Reading");
            combo.Items.Add("Read it");
            combo.Items.Add("Abandoned");

            combo.SelectedIndex = 0;

            var btnSave = new Button
            {
                Content = "Сохранить",
                Height = 40,
                FontSize = 14
            };

            btnSave.Click += (s, args) =>
            {
                string selectedStatus = combo.SelectedItem?.ToString();

                if (string.IsNullOrWhiteSpace(selectedStatus))
                    return;

                try
                {
                    var existing = Core.Context.UserBooks
                        .FirstOrDefault(ub =>
                            ub.UserID == _currentUser.UserID &&
                            ub.BookID == book.BookID);

                    if (existing != null)
                    {
                        existing.Status = selectedStatus;
                        existing.AddedAt = DateTime.Now;
                    }
                    else
                    {
                        Core.Context.UserBooks.Add(new UserBooks
                        {
                            UserID = _currentUser.UserID,
                            BookID = book.BookID,
                            Status = selectedStatus,
                            AddedAt = DateTime.Now
                        });
                    }

                    Core.Context.SaveChanges();

                    MessageBox.Show(
                        $"Книга сохранена в список:\n{selectedStatus}",
                        "Успех",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    window.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        ex.Message,
                        "Ошибка",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            };

            panel.Children.Add(titleText);
            panel.Children.Add(combo);
            panel.Children.Add(btnSave);

            window.Content = panel;

            window.ShowDialog();
        }
    }
}
