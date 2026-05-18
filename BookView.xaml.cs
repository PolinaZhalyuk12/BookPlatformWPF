using Microsoft.VisualBasic;
using Microsoft.VisualBasic.ApplicationServices;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace BookPlatformWPF
{
    public partial class BookView : UserControl
    {
        private readonly BookViewModel _vm;
        private readonly Books _currentBook;
        public Users _currentUser { get; private set; }
       

        public BookView(Books book, Users currentUser)
        {
            InitializeComponent();
            _currentBook = book;
            _currentUser = currentUser;
            _vm = new BookViewModel(book);
            DataContext = _vm;
            UpdateVisibility();
           
        }
        private void FreezeReviewButton_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                if (_currentUser != null &&
                    _currentUser.Roles != null &&
                    _currentUser.Roles.RoleName == "Администратор")
                {
                    btn.Visibility = Visibility.Visible;
                }
                else
                {
                    btn.Visibility = Visibility.Collapsed;
                }
            }
        }
        private Users GetCurrentUser()
        {
            // Основной способ — из MainWindow
            if (Application.Current.MainWindow is MainWindow mainWindow && mainWindow.CurrentUser != null)
            {
                return mainWindow.CurrentUser;
            }

            // Если MainWindow не найден — экстренный вариант (нежелательно)
            MessageBox.Show("Не удалось определить текущего пользователя!", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
            return null;
        }
        private void UpdateVisibility()
        {
            // Проверка, является ли текущий пользователь администратором
            bool isAdmin = false;
            if (_currentUser != null && _currentUser.Roles != null)
            {
                if (_currentUser.Roles.RoleID == 3)
                    isAdmin = true;
            }

            // === Админ панель (заморозка книги) ===
            if (isAdmin == true)
            {
                AdminPanel.Visibility = Visibility.Visible;
            }
            else
            {
                AdminPanel.Visibility = Visibility.Collapsed;
            }

            // === Предупреждение о заморозке книги ===
            if (_currentBook != null && _currentBook.IsFrozen)
            {
                borderFrozen.Visibility = Visibility.Visible;
            }
            else
            {
                borderFrozen.Visibility = Visibility.Collapsed;
            }
          
        }
       

        // ==================== ДОБАВИТЬ В СПИСОК ЧТЕНИЯ ====================
        private void AddToList_Click(object sender, RoutedEventArgs e)
        {
            if (_currentUser == null)
            {
                MessageBox.Show("Необходимо авторизоваться!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Создаём окно выбора статуса
            var selectWindow = new Window
            {
                Title = "Выбор статуса книги",
                Width = 320,
                Height = 220,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize
            };

            var stack = new StackPanel { Margin = new Thickness(20) };

            stack.Children.Add(new TextBlock
            {
                Text = "Выберите статус книги:",
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 10)
            });

            var comboBox = new ComboBox { Margin = new Thickness(0, 0, 0, 15) };
            comboBox.Items.Add("In the plans");
            comboBox.Items.Add("Reading");
            comboBox.Items.Add("Read it");
            comboBox.Items.Add("Abandoned");
            comboBox.SelectedIndex = 0; // По умолчанию "В планах"

            stack.Children.Add(comboBox);

            var btnSave = new Button
            {
                Content = "Сохранить",
                Height = 35,
                Margin = new Thickness(0, 10, 0, 0)
            };

            btnSave.Click += (s, args) =>
            {
                string selectedStatus = comboBox.SelectedItem?.ToString();

                if (string.IsNullOrEmpty(selectedStatus))
                {
                    selectWindow.Close();
                    return;
                }

                // Проверяем существующую запись
                var existing = Core.Context.UserBooks.FirstOrDefault(ub =>
                    ub.UserID == _currentUser.UserID && ub.BookID == _currentBook.BookID);

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
                        BookID = _currentBook.BookID,
                        Status = selectedStatus,
                        AddedAt = DateTime.Now
                    });
                }

                Core.Context.SaveChanges();
                MessageBox.Show($"Книга добавлена в список:\n**{selectedStatus}**",
                                "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                selectWindow.Close();
            };

            stack.Children.Add(btnSave);
            selectWindow.Content = stack;
            selectWindow.ShowDialog();
        }
        // ==================== ОСТАВИТЬ ОТЗЫВ ====================
        private void LeaveReview_Click(object sender, RoutedEventArgs e)
        {
            if (_currentUser == null)
            {
                MessageBox.Show("Необходимо авторизоваться!", "Ошибка");
                return;
            }

            // ===== ОКНО ОТЗЫВА =====
            var reviewWindow = new Window
            {
                Title = "Оставить отзыв",
                Width = 420,
                Height = 380,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize
            };

            var panel = new StackPanel
            {
                Margin = new Thickness(20)
            };

            panel.Children.Add(new TextBlock
            {
                Text = "Текст отзыва:",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 5)
            });

            var txtReview = new TextBox
            {
                Height = 140,
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Margin = new Thickness(0, 0, 0, 15)
            };

            panel.Children.Add(txtReview);

            panel.Children.Add(new TextBlock
            {
                Text = "Оценка:",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 5)
            });

            var cmbRating = new ComboBox
            {
                Height = 35,
                Margin = new Thickness(0, 0, 0, 20)
            };

            for (int i = 1; i <= 10; i++)
            {
                cmbRating.Items.Add(i);
            }

            cmbRating.SelectedIndex = 7;

            panel.Children.Add(cmbRating);

            var btnSave = new Button
            {
                Content = "Сохранить отзыв",
                Height = 40
            };

            btnSave.Click += (s, args) =>
            {
                string text = txtReview.Text?.Trim();

                if (string.IsNullOrWhiteSpace(text))
                {
                    MessageBox.Show("Введите текст отзыва!");
                    return;
                }

                if (cmbRating.SelectedItem == null)
                {
                    MessageBox.Show("Выберите оценку!");
                    return;
                }

                int rating = (int)cmbRating.SelectedItem;

                var existingReview = Core.Context.Reviews.FirstOrDefault(r =>
                    r.BookID == _currentBook.BookID &&
                    r.UserID == _currentUser.UserID);

                if (existingReview != null)
                {
                    existingReview.Rating = rating;
                    existingReview.ReviewText = text;
                    existingReview.CreatedAt = DateTime.Now;
                }
                else
                {
                    Core.Context.Reviews.Add(new Reviews
                    {
                        BookID = _currentBook.BookID,
                        UserID = _currentUser.UserID,
                        Rating = rating,
                        ReviewText = text,
                        CreatedAt = DateTime.Now,
                        IsFrozen = false
                    });
                }

                Core.Context.SaveChanges();

                MessageBox.Show("Отзыв успешно сохранён!", "Успех");

                _vm.RefreshReviews();

                reviewWindow.Close();
            };

            panel.Children.Add(btnSave);

            reviewWindow.Content = panel;
            reviewWindow.ShowDialog();
        }

        // ==================== ПОЖАЛОВАТЬСЯ НА КНИГУ ====================
        private void ComplainBook_Click(object sender, RoutedEventArgs e)
        {
            if (_currentUser == null)
            {
                MessageBox.Show("Необходимо авторизоваться!", "Ошибка");
                return;
            }

            var dialog = new InputDialog("Введите причину жалобы:", "Жалоба");

            if (dialog.ShowDialog() != true)
                return;

            string reason = dialog.ResponseText;
            if (string.IsNullOrWhiteSpace(reason)) return;

            Core.Context.Complaints.Add(new Complaints
            {
                UserID = _currentUser.UserID,
                TargetType = "Book",
                TargetID = _currentBook.BookID,
                Reason = reason,
                CreatedAt = DateTime.Now
            });

            Core.Context.SaveChanges();
            MessageBox.Show("Жалоба успешно отправлена администратору!", "Жалоба отправлена",
                            MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ComplainAuthor_Click(object sender, RoutedEventArgs e)
        {
            if (_currentUser == null) return;
            var dialog = new InputDialog("Введите причину жалобы:", "Жалоба");

            if (dialog.ShowDialog() != true)
                return;

            string reason = dialog.ResponseText;
            if (string.IsNullOrWhiteSpace(reason)) return;

            Core.Context.Complaints.Add(new Complaints
            {
                UserID = _currentUser.UserID,
                TargetType = "Author",
                TargetID = _currentBook.AuthorID,
                Reason = reason,
                CreatedAt = DateTime.Now
            });
            Core.Context.SaveChanges();
            MessageBox.Show("Жалоба на автора отправлена администратору!");
        }

        private void ComplainReview_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Reviews review)
            {
                var dialog = new InputDialog("Введите причину жалобы:", "Жалоба");

                if (dialog.ShowDialog() != true)
                    return;

                string reason = dialog.ResponseText;
                if (string.IsNullOrWhiteSpace(reason)) return;

                Core.Context.Complaints.Add(new Complaints
                {
                    UserID = _currentUser.UserID,
                    TargetType = "Review",
                    TargetID = review.ReviewID,
                    Reason = reason,
                    CreatedAt = DateTime.Now
                });
                Core.Context.SaveChanges();
                MessageBox.Show("Жалоба на отзыв отправлена!");
            }
        }

        private void FreezeBook_Click(object sender, RoutedEventArgs e)
        {
            if (_currentUser?.Roles?.RoleName != "Администратор") return;

            _currentBook.IsFrozen = !_currentBook.IsFrozen;
            Core.Context.SaveChanges();
            UpdateVisibility(); // Обновляем видимость после изменения
            MessageBox.Show(_currentBook.IsFrozen ? "Книга заморожена" : "Книга разморожена");
        }
        private void FreezeReview_Click(object sender, RoutedEventArgs e)
        {
            if (_currentUser?.Roles?.RoleName != "Администратор") return;

            if (sender is Button btn && btn.Tag is Reviews review)
            {
                // Предполагаем, что у Review есть поле IsFrozen. Если нет — можно добавить позже.
                review.IsFrozen = !review.IsFrozen;
                Core.Context.SaveChanges();
                MessageBox.Show("Отзыв заморожен.");
                _vm.RefreshReviews();
                _vm.RefreshData();
            }
        }
    }
}
