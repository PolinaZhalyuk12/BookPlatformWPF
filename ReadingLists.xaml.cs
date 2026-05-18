using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace BookPlatformWPF
{
    public partial class ReadingListsView : UserControl
    {
        private readonly ReadingListsViewModel _vm;
        private readonly Users _currentUser;

        public ReadingListsView(Users user)
        {
            InitializeComponent();

            _currentUser = user;

            _vm = new ReadingListsViewModel(user);

            DataContext = _vm;

            LoadGenres();

            cbStatus.SelectedIndex = 0;
        }
        private void LoadGenres()
        {
            cmbGenre.Items.Clear();

            cmbGenre.Items.Add(
                new ComboBoxItem
                {
                    Content = "Все жанры",
                    IsSelected = true
                });

            var genres = Core.Context.Genres.ToList();

            foreach (var genre in genres)
            {
                cmbGenre.Items.Add(
                    new ComboBoxItem
                    {
                        Content = genre.Name
                    });
            }
        }

        private void Filters_Changed(object sender, RoutedEventArgs e)
        {
            ApplyFilters();
        }

        private void OpenBook_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Books book)
            {
                var main = Window.GetWindow(this) as MainWindow;
                main.ContentArea.Content = new BookView(book, _currentUser);
            }
        }

        // ==================== ПЕРЕМЕСТИТЬ КНИГУ (выбор из списка) ====================
        private void MoveBook_Click(object sender, RoutedEventArgs e)
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
                MessageBox.Show("Не удалось определить пользователя или книгу", "Ошибка");
                return;
            }

            var window = new Window
            {
                Title = "Переместить книгу",
                Width = 340,
                Height = 240,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize
            };

            var panel = new StackPanel { Margin = new Thickness(20) };

            var titleText = new TextBlock
            {
                Text = $"Переместить:\n«{book.Title}»",
                FontSize = 14,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 15)
            };

            var combo = new ComboBox { Margin = new Thickness(0, 0, 0, 20) };
            combo.Items.Add("In the plans");
            combo.Items.Add("Reading");
            combo.Items.Add("Read it");
            combo.Items.Add("Abandoned");
            combo.SelectedIndex = 0;

            var btnMove = new Button
            {
                Content = "Переместить",
                Height = 40,
                FontSize = 14
            };

            btnMove.Click += (s, args) =>
            {
                string newStatus = combo.SelectedItem?.ToString();

                try
                {
                    var record = Core.Context.UserBooks.FirstOrDefault(ub =>
                        ub.UserID == _currentUser.UserID && ub.BookID == book.BookID);

                    if (record != null)
                    {
                        record.Status = newStatus;
                        record.AddedAt = DateTime.Now;
                        Core.Context.SaveChanges();

                        MessageBox.Show($"Книга успешно перемещена в:\n**{newStatus}**",
                                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                        // Обновляем текущий список
                        if (cbStatus.SelectedItem is ComboBoxItem currentItem)
                        {
                            _vm.LoadBooksByStatus(currentItem.Content.ToString());
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при перемещении:\n" + ex.Message, "Ошибка",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                }

                window.Close();
            };

            panel.Children.Add(titleText);
            panel.Children.Add(combo);
            panel.Children.Add(btnMove);
            window.Content = panel;
            window.ShowDialog();
        }

        private void ApplyFilters()
        {
            if (_vm == null)
                return;

            string search = txtSearch?.Text ?? "";

            string genre =
                (cmbGenre.SelectedItem as ComboBoxItem)?
                .Content?
                .ToString();

            string status =
                (cbStatus.SelectedItem as ComboBoxItem)?
                .Content?
                .ToString();

            int sortIndex = cmbSort?.SelectedIndex ?? 0;

            _vm.ApplyFilters(
                status,
                search,
                genre,
                sortIndex);
        }
    }
}