using Microsoft.VisualBasic.ApplicationServices;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace BookPlatformWPF
{
    public abstract class BaseViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
    public class CatalogViewModel : BaseViewModel
    {
        private ObservableCollection<BookExtended> _allBooks;
        public ObservableCollection<BookExtended> FilteredBooks { get; set; }
        private readonly Users _user;
        public ObservableCollection<BookExtended> BooksInList { get; set; }  
        public BookExtended SelectedBook { get; set; }
        public CatalogViewModel()
        {
            LoadBooks();
        }
        public void LoadBooksByStatus(string status)
        {
            if (_user == null) return;

            var books = Core.Context.UserBooks
                .Include("Book.User")
                .Where(ub => ub.UserID == _user.UserID && ub.Status == status)
                .Select(ub => ub.Books)
                .ToList();

            BooksInList = new ObservableCollection<BookExtended>(
                books.Select(b => new BookExtended(b))
            );

            OnPropertyChanged(nameof(BooksInList));
        }

        public void MoveBookToAnotherStatus(Books book)
        {
            if (book == null || _user == null) return;

            var statuses = new[] { "В планах", "Читаю", "Прочитано", "Заброшено" };
            string newStatus = Microsoft.VisualBasic.Interaction.InputBox(
                "Новый статус книги:",
                "Перемещение книги",
                "Прочитано");

            if (string.IsNullOrWhiteSpace(newStatus) || !statuses.Contains(newStatus))
                return;

            var record = Core.Context.UserBooks.FirstOrDefault(ub =>
                ub.UserID == _user.UserID && ub.BookID == book.BookID);

            if (record != null)
            {
                record.Status = newStatus;
                record.AddedAt = System.DateTime.Now;
                Core.Context.SaveChanges();

                MessageBox.Show($"Книга перемещена в список: **{newStatus}**", "Успех");

                // Обновляем текущий список
                LoadBooksByStatus(record.Status); // или текущий выбранный статус
            }
        }
        private void LoadBooks()
        {
            var rawBooks = Core.Context.Books
                .Include("Users")
                .Include("Reviews")
                .Include("Genres") // обязательно
                .ToList();

            _allBooks = new ObservableCollection<BookExtended>(
                rawBooks.Select(b => new BookExtended(b))
            );

            FilteredBooks = new ObservableCollection<BookExtended>(_allBooks);
        }

        /// <summary>
        /// Основной метод фильтрации и сортировки
        /// </summary>
        public void ApplyFilters(string searchText, string genreFilter, int sortIndex)
        {
            var query = _allBooks.AsQueryable();

            // === Поиск по названию книги или имени автора ===
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                searchText = searchText.ToLower();
                query = query.Where(b =>
    (b.Title != null &&
     b.Title.ToLower().Contains(searchText)) ||

    (b.DisplayName != null &&
     b.DisplayName.ToLower().Contains(searchText)));
            }

            // === Фильтрация по жанру ===
          if (!string.IsNullOrWhiteSpace(genreFilter) && genreFilter != "Все жанры")
{
    query = query.Where(b =>
        b.Book.Genres.Any(g => g.Name == genreFilter));
}

            // === Сортировка ===
            switch (sortIndex)
            {
                case 0: // По названию (А-Я)
                    query = query.OrderBy(b => b.Title);
                    break;
                case 1: // По названию (Я-А)
                    query = query.OrderByDescending(b => b.Title);
                    break;
                case 2: // По рейтингу (высокий)
                    query = query.OrderByDescending(b => b.AvgRating);
                    break;
                case 3: // По рейтингу (низкий)
                    query = query.OrderBy(b => b.AvgRating);
                    break;
            }

            FilteredBooks = new ObservableCollection<BookExtended>(query.ToList());
            OnPropertyChanged(nameof(FilteredBooks));
        }
    }

    public class BookExtended
    {
        public Books Book { get; private set; }
        public double AvgRating { get; private set; }

        // Свойства для привязки в XAML
        public string Title => Book?.Title;
        public string DisplayName => Book?.Users?.DisplayName ?? Book?.Users?.DisplayName ?? "Неизвестный автор";

        public DateTime CreatedAt => Book?.CreatedAt ?? DateTime.MinValue;
        public bool IsFrozen => Book?.IsFrozen ?? false;
        public string GenresString => string.Join(", ",
            Book.Genres.Select(bg => bg?.Name) ?? Enumerable.Empty<string>());
        // Для кнопки "Оспорить заморозку"
        public Visibility ShowUnfreezeButton => IsFrozen ? Visibility.Visible : Visibility.Collapsed;

        public BookExtended(Books book)
        {
            Book = book;
            AvgRating = book.Reviews?.Any() == true
                ? book.Reviews.Average(r => r.Rating)
                : 0;
        }
    }
    public class LoginViewModel
    {
        public Users Login(string login, string password)
        {
            return Core.Context.Users
                .Include("Roles")
                .FirstOrDefault(u => u.Login == login && u.Password == password);
        }
    }


      
    
    public class BookViewModel : BaseViewModel
    {
        public Books Book { get; set; }
        public ObservableCollection<Reviews> Reviews { get; set; }
        public string GenresString { get; set; }
        public string AuthorName { get; set; }
        public bool IsAdmin { get; set; }
        public Visibility AdminVisibility
        {
            get
            {
                if (IsAdmin == true)
                {
                    return Visibility.Visible;
                }
                else
                {
                    return Visibility.Collapsed;
                }
            }
        }

        public BookViewModel(Books book)
        {
            Book = book;
            AuthorName = book.Users?.DisplayName ?? book.Users?.DisplayName ?? "Неизвестный автор";

            IsAdmin = (Application.Current.MainWindow as MainWindow)?
    .CurrentUser?
    .Roles?
    .RoleName == "Администратор";

            RefreshReviews();

            GenresString = string.Join(", ",
                book.Genres.Select(bg => bg.Name) ?? Enumerable.Empty<string>());
        }

        public void RefreshReviews()
        {
            Reviews = new ObservableCollection<Reviews>(
                Reviews = new ObservableCollection<Reviews>(
    Core.Context.Reviews
            .Where(r =>
            r.BookID == Book.BookID &&
            !r.IsFrozen)
            .ToList()));
            OnPropertyChanged(nameof(AdminVisibility));
        }

        public void RefreshData()
        {
            RefreshReviews();
            OnPropertyChanged(nameof(Book));
        }
    }


    public class ReadingListsViewModel : BaseViewModel
    {
        private readonly Users _user;
        public ObservableCollection<BookExtended> BooksInList { get; set; }

        public ReadingListsViewModel(Users user)
        {
            _user = user;
            BooksInList = new ObservableCollection<BookExtended>();
        }

        public void LoadBooksByStatus(string status)
        {
            if (_user == null) return;

            var books = Core.Context.UserBooks
                .Include("Book.User")
                .Where(ub => ub.UserID == _user.UserID && ub.Status == status)
                .Select(ub => ub.Books)
                .ToList();

            BooksInList = new ObservableCollection<BookExtended>(
                books.Select(b => new BookExtended(b))
            );

            OnPropertyChanged(nameof(BooksInList));
        }

        public void MoveBookToAnotherStatus(Books book)
        {
            if (book == null || _user == null) return;

            var statuses = new[] { "В планах", "Читаю", "Прочитано", "Заброшено" };
            string newStatus = Microsoft.VisualBasic.Interaction.InputBox(
                "Новый статус книги:",
                "Перемещение книги",
                "Прочитано");

            if (string.IsNullOrWhiteSpace(newStatus) || !statuses.Contains(newStatus))
                return;

            var record = Core.Context.UserBooks.FirstOrDefault(ub =>
                ub.UserID == _user.UserID && ub.BookID == book.BookID);

            if (record != null)
            {
                record.Status = newStatus;
                record.AddedAt = System.DateTime.Now;
                Core.Context.SaveChanges();

                MessageBox.Show($"Книга перемещена в список: **{newStatus}**", "Успех");

                // Обновляем текущий список
                LoadBooksByStatus(record.Status); // или текущий выбранный статус
            }
        }
        public void ApplyFilters(
    string status,
    string searchText,
    string genreFilter,
    int sortIndex)
        {
            if (_user == null)
                return;

            var books = Core.Context.UserBooks
                .Include("Books.Users")
                .Include("Books.Reviews")
                .Include("Books.Genres")
                .Where(ub =>
                    ub.UserID == _user.UserID &&
                    ub.Status == status)
                .Select(ub => ub.Books)
                .ToList();

            var query = books
                .Select(b => new BookExtended(b))
                .AsQueryable();

            // === Поиск ===
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                searchText = searchText.ToLower();

                query = query.Where(b =>
                    (b.Title != null &&
                     b.Title.ToLower().Contains(searchText))

                    ||

                    (b.DisplayName != null &&
                     b.DisplayName.ToLower().Contains(searchText)));
            }

            // === Жанры ===
            if (!string.IsNullOrWhiteSpace(genreFilter) &&
                genreFilter != "Все жанры")
            {
                query = query.Where(b =>
                    b.Book.Genres.Any(g =>
                        g.Name == genreFilter));
            }

            // === Сортировка ===
            switch (sortIndex)
            {
                case 0:
                    query = query.OrderBy(b => b.Title);
                    break;

                case 1:
                    query = query.OrderByDescending(b => b.Title);
                    break;

                case 2:
                    query = query.OrderByDescending(b => b.AvgRating);
                    break;

                case 3:
                    query = query.OrderBy(b => b.AvgRating);
                    break;
            }

            BooksInList =
                new ObservableCollection<BookExtended>(
                    query.ToList());

            OnPropertyChanged(nameof(BooksInList));
        }
    }




    public class ProfileViewModel : BaseViewModel
    {
        public Users User { get; set; }

        public ObservableCollection<Reviews> UserReviews { get; set; }

        public string FreezeReason =>
            User?.IsFrozen == true
                ? "Ваш аккаунт временно заморожен администрацией."
                : "";

        public Visibility FrozenVisibility =>
            User?.IsFrozen == true
                ? Visibility.Visible
                : Visibility.Collapsed;

        public Visibility AuthorRequestVisibility
        {
            get
            {
                if (User?.Roles == null)
                    return Visibility.Visible;

                // Автор или админ — скрываем кнопку
                if (User.Roles.RoleID == 2 ||
                    User.Roles.RoleID == 3)
                {
                    return Visibility.Collapsed;
                }

                return Visibility.Visible;
            }
        }

        public ProfileViewModel()
        {
            UserReviews = new ObservableCollection<Reviews>();
        }

        public ProfileViewModel(Users user)
        {
            User = user;

            UserReviews = new ObservableCollection<Reviews>(
                Core.Context.Reviews
                    .Include("Books")
                    .Where(r => r.UserID == user.UserID)
                    .ToList());
        }
    }


    public class AdminViewModel : BaseViewModel
    {
        public ObservableCollection<Complaints> Complaints { get; set; }
        public ObservableCollection<RoleRequests> RoleRequests { get; set; }
        public ObservableCollection<UnfreezeRequests> UnfreezeRequests { get; set; }
        public ObservableCollection<Users> Users { get; set; }

        // Для вкладки "Замороженные"
        public ObservableCollection<FrozenItem> FrozenItems { get; set; }

        public AdminViewModel()
        {
            RefreshData();
        }

        public void RefreshData()
        {
            // ===== ЖАЛОБЫ =====

            Complaints = new ObservableCollection<Complaints>(
                Core.Context.Complaints
                    .Include("Users")
                    .OrderByDescending(c => c.CreatedAt)
                    .ToList());

            // ===== ЗАЯВКИ НА РОЛЬ =====

            RoleRequests = new ObservableCollection<RoleRequests>(
                Core.Context.RoleRequests
                    .Include("Users")
                    .OrderByDescending(r => r.CreatedAt)
                    .ToList());

            // ===== ЗАЯВКИ НА РАЗМОРОЗКУ =====

            UnfreezeRequests = new ObservableCollection<UnfreezeRequests>(
                Core.Context.UnfreezeRequests
                    .Include("Users")
                    .OrderByDescending(r => r.CreatedAt)
                    .ToList());

            // ===== ПОЛЬЗОВАТЕЛИ =====

            Users = new ObservableCollection<Users>(
                Core.Context.Users
                    .Include("Roles")
                    .OrderBy(u => u.DisplayName)
                    .ToList());

            // ===== ЗАМОРОЖЕННЫЕ КНИГИ =====

            var frozenBooks = Core.Context.Books
                .Where(b => b.IsFrozen)
                .ToList()
                .Select(b => new FrozenItem
                {
                    Type = "Книга",
                    Name = b.Title,
                    FrozenDate = b.CreatedAt
                });

            // ===== ЗАМОРОЖЕННЫЕ ПОЛЬЗОВАТЕЛИ =====

            var frozenUsers = Core.Context.Users
                .Where(u => u.IsFrozen)
                .ToList()
                .Select(u => new FrozenItem
                {
                    Type = "Пользователь",
                    Name = u.DisplayName,
                    FrozenDate = u.CreatedAt
                });

            // ===== ЗАМОРОЖЕННЫЕ ОТЗЫВЫ =====

            var frozenReviews = Core.Context.Reviews
                .Where(r => r.IsFrozen)
                .ToList()
                .Select(r => new FrozenItem
                {
                    Type = "Отзыв",
                    Name = r.ReviewText,
                    FrozenDate = r.CreatedAt
                });

            FrozenItems = new ObservableCollection<FrozenItem>(
                frozenBooks
                .Concat(frozenUsers)
                .Concat(frozenReviews));

            OnPropertyChanged(nameof(Complaints));
            OnPropertyChanged(nameof(RoleRequests));
            OnPropertyChanged(nameof(UnfreezeRequests));
            OnPropertyChanged(nameof(Users));
            OnPropertyChanged(nameof(FrozenItems));
        }}

        // Вспомогательный класс для вкладки "Замороженные"
        public class FrozenItem
    {
        public string Type { get; set; }

        public string Name { get; set; }

        public DateTime FrozenDate { get; set; }

        public int TargetId { get; set; }
    }

    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class AuthorViewModel : BaseViewModel
    {
        private readonly Users _currentUser;
        public ObservableCollection<BookExtended> MyBooks { get; set; }

        public AuthorViewModel(Users user)
        {
            _currentUser = user;
            RefreshBooks();
        }

        public void RefreshBooks()
        {
            ApplyFilters("", false);
        }
        
        public void ApplyFilters(string search, bool onlyFrozen)
        {
            if (_currentUser == null)
                     return;

            var query = Core.Context.Books
                .Where(b => b.AuthorID == _currentUser.UserID);

            // ===== Поиск =====

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();

                query = query.Where(b =>
                    b.Title.ToLower().Contains(search));
            }

            // ===== Только замороженные =====

            if (onlyFrozen)
            {
                query = query.Where(b => b.IsFrozen);
            }

            var books = query
                .OrderByDescending(b => b.CreatedAt)
                .ToList();

            MyBooks = new ObservableCollection<BookExtended>(
                books.Select(b => new BookExtended(b)));

            OnPropertyChanged(nameof(MyBooks));
        }
    }
}
